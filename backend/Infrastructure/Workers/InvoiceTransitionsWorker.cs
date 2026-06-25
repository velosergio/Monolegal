using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Monolegal.Domain.Repositories;
using Monolegal.Domain.Services;

namespace Monolegal.Infrastructure.Workers;

/// <summary>
/// Background worker that periodically evaluates and applies automatic status
/// transitions to active invoices (Pending → PrimerRecordatorio → SegundoRecordatorio → Desactivado).
///
/// Runs every hour (configurable via <see cref="RunInterval"/>).
/// On each cycle:
///   1. Loads the transition configuration from <see cref="ISystemSettingsRepository"/>.
///   2. Fetches all invoices in transitionable states via <see cref="IInvoiceRepository"/>.
///   3. Calls <see cref="InvoiceTransitionService.TryApplyTransition"/> on each invoice.
///   4. Persists changed invoices back through <see cref="IInvoiceRepository.UpdateAsync"/>.
///   5. Logs a structured summary with Serilog.
///
/// Validates: FR-001, FR-002, FR-003 | US1 (spec.md 006-invoice-status-transitions)
/// </summary>
public sealed class InvoiceTransitionsWorker : BackgroundService
{
    /// <summary>How often the worker evaluates pending transitions. Default: 1 hour.</summary>
    public static readonly TimeSpan RunInterval = TimeSpan.FromHours(1);

    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ISystemSettingsRepository _settingsRepository;
    private readonly InvoiceTransitionService _transitionService;
    private readonly ILogger<InvoiceTransitionsWorker> _logger;

    public InvoiceTransitionsWorker(
        IInvoiceRepository invoiceRepository,
        ISystemSettingsRepository settingsRepository,
        InvoiceTransitionService transitionService,
        ILogger<InvoiceTransitionsWorker> logger)
    {
        _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
        _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
        _transitionService = transitionService ?? throw new ArgumentNullException(nameof(transitionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "InvoiceTransitionsWorker iniciado. Intervalo={IntervalMinutes} min.",
            RunInterval.TotalMinutes);

        // Run immediately on startup, then on the configured interval.
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunCycleAsync(stoppingToken).ConfigureAwait(false);

            try
            {
                await Task.Delay(RunInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Application is shutting down — exit gracefully.
                break;
            }
        }

        _logger.LogInformation("InvoiceTransitionsWorker detenido.");
    }

    /// <summary>
    /// Executes a single evaluation cycle: loads config, fetches transitionable
    /// invoices, applies transitions, and persists changes.
    /// </summary>
    internal async Task RunCycleAsync(CancellationToken cancellationToken = default)
    {
        var cycleStart = DateTimeOffset.UtcNow;

        _logger.LogInformation(
            "InvoiceTransitionsWorker — inicio de ciclo. Timestamp={Timestamp:o}",
            cycleStart);

        try
        {
            // 1. Load transition configuration.
            var settings = await _settingsRepository.GetSettingsAsync().ConfigureAwait(false);
            var config = settings.InvoiceTransitions;

            // 2. Fetch all invoices eligible for automatic transition.
            var candidates = await _invoiceRepository
                .GetTransitionableAsync(cancellationToken)
                .ConfigureAwait(false);

            var now = DateTime.UtcNow;
            int evaluated = 0;
            int transitioned = 0;

            // 3. Evaluate and apply transitions.
            foreach (var invoice in candidates)
            {
                evaluated++;
                var previousStatus = invoice.Status;

                if (_transitionService.TryApplyTransition(invoice, config, now))
                {
                    // 4. Persist the change.
                    await _invoiceRepository
                        .UpdateAsync(invoice, cancellationToken)
                        .ConfigureAwait(false);

                    transitioned++;

                    _logger.LogInformation(
                        "Transición aplicada. InvoiceId={InvoiceId} De={PreviousStatus} A={NewStatus}",
                        invoice.Id,
                        previousStatus,
                        invoice.Status);
                }
            }

            var elapsed = DateTimeOffset.UtcNow - cycleStart;

            _logger.LogInformation(
                "InvoiceTransitionsWorker — fin de ciclo. Evaluadas={Evaluated} Transicionadas={Transitioned} DuracionMs={DurationMs}",
                evaluated,
                transitioned,
                elapsed.TotalMilliseconds);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("InvoiceTransitionsWorker — ciclo cancelado por señal de apagado.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "InvoiceTransitionsWorker — error no controlado en el ciclo. Timestamp={Timestamp:o}",
                cycleStart);
            // Fail-soft: log the error and let the next scheduled cycle try again.
        }
    }
}
