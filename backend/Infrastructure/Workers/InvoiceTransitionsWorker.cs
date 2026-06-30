using System;
using System.Threading;
using System.Threading.Tasks;
using Backend.Application.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monolegal.Domain.Repositories;
using Monolegal.Domain.Services;

namespace Monolegal.Infrastructure.Workers;

/// <summary>
/// Background worker that periodically evaluates and applies automatic status
/// transitions to active invoices (Pending → PrimerRecordatorio → SegundoRecordatorio → Desactivado).
///
/// Runs on a configurable interval (see <see cref="InvoiceTransitionsWorkerOptions"/>).
/// On each cycle:
///   1. Loads the transition configuration from <see cref="ISystemSettingsRepository"/>.
///   2. Fetches all invoices in transitionable states via <see cref="IInvoiceRepository"/>.
///   3. Calls <see cref="InvoiceTransitionService.TryApplyTransition"/> on each invoice,
///      isolating per-invoice failures so one error does not abort the batch.
///   4. Persists changed invoices back through <see cref="IInvoiceRepository.UpdateAsync"/>.
///   5. Logs a structured summary with Serilog (timestamp, evaluated, transitioned, errors, duration).
///
/// Validates: FR-001..FR-012 | US1/US2/US3 (spec.md 012-worker-state-transitions)
/// </summary>
/// <remarks>
/// SOLID: DIP — depende de <see cref="ISystemSettingsRepository"/> e <see cref="IInvoiceRepository"/>
/// (abstracciones inyectadas por constructor), no de MongoDB. SRP — única responsabilidad: orquestar
/// el ciclo periódico de evaluación y aplicación de transiciones.
/// </remarks>
public sealed class InvoiceTransitionsWorker : BackgroundService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ISystemSettingsRepository _settingsRepository;
    private readonly InvoiceTransitionService _transitionService;
    private readonly IInvoiceTransitionNotifier _notifier;
    private readonly ILogger<InvoiceTransitionsWorker> _logger;
    private readonly InvoiceTransitionsWorkerOptions _options;

    public InvoiceTransitionsWorker(
        IInvoiceRepository invoiceRepository,
        ISystemSettingsRepository settingsRepository,
        InvoiceTransitionService transitionService,
        IInvoiceTransitionNotifier notifier,
        IOptions<InvoiceTransitionsWorkerOptions> options,
        ILogger<InvoiceTransitionsWorker> logger)
    {
        _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
        _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
        _transitionService = transitionService ?? throw new ArgumentNullException(nameof(transitionService));
        _notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = _options.GetInterval();

        if (_options.HasInvalidInterval)
        {
            _logger.LogWarning(
                "InvoiceTransitionsWorker — IntervalMinutes inválido ({Configured}); se usa el default de {DefaultMinutes} min.",
                _options.IntervalMinutes,
                InvoiceTransitionsWorkerOptions.DefaultIntervalMinutes);
        }

        _logger.LogInformation(
            "InvoiceTransitionsWorker iniciado. Intervalo={IntervalMinutes} min RunOnStartup={RunOnStartup}.",
            interval.TotalMinutes,
            _options.RunOnStartup);

        // Optionally wait one interval before the first cycle.
        if (!_options.RunOnStartup)
        {
            try
            {
                await Task.Delay(interval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("InvoiceTransitionsWorker detenido antes del primer ciclo.");
                return;
            }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunCycleAsync(stoppingToken).ConfigureAwait(false);

            try
            {
                await Task.Delay(interval, stoppingToken).ConfigureAwait(false);
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
    /// Executes a single evaluation cycle: loads config, fetches transitionable invoices,
    /// applies transitions (isolating per-invoice failures), persists changes and returns a
    /// summary of the cycle.
    /// </summary>
    internal async Task<CycleResult> RunCycleAsync(CancellationToken cancellationToken = default)
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
            int errors = 0;

            // 3. Evaluate and apply transitions, isolating per-invoice failures.
            foreach (var invoice in candidates)
            {
                evaluated++;
                var previousStatus = invoice.Status;

                try
                {
                    if (_transitionService.TryApplyTransition(invoice, config, now))
                    {
                        // 4. Notify the client of the transition (sends email, records result on
                        //    the invoice). It mutates the entity in-memory; persistence below is a
                        //    single write covering both status and notification result (spec 013).
                        await _notifier
                            .NotifyTransitionAsync(invoice, previousStatus, cancellationToken)
                            .ConfigureAwait(false);

                        // 5. Persist the change (status + notification result).
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
                catch (OperationCanceledException)
                {
                    // Shutdown signal — stop the whole cycle.
                    throw;
                }
                catch (Exception ex)
                {
                    errors++;
                    _logger.LogError(
                        ex,
                        "InvoiceTransitionsWorker — error al procesar la factura. InvoiceId={InvoiceId}",
                        invoice.Id);
                    // Fail-soft per invoice: continue with the rest of the batch.
                }
            }

            var elapsed = DateTimeOffset.UtcNow - cycleStart;

            _logger.LogInformation(
                "InvoiceTransitionsWorker — fin de ciclo. Evaluadas={Evaluated} Transicionadas={Transitioned} Errores={Errors} DuracionMs={DurationMs}",
                evaluated,
                transitioned,
                errors,
                elapsed.TotalMilliseconds);

            return new CycleResult(cycleStart, evaluated, transitioned, errors, elapsed);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("InvoiceTransitionsWorker — ciclo cancelado por señal de apagado.");
            throw;
        }
        catch (Exception ex)
        {
            var elapsed = DateTimeOffset.UtcNow - cycleStart;
            _logger.LogError(
                ex,
                "InvoiceTransitionsWorker — error no controlado en el ciclo. Timestamp={Timestamp:o}",
                cycleStart);
            // Fail-soft: log the error and let the next scheduled cycle try again.
            return new CycleResult(cycleStart, 0, 0, 0, elapsed);
        }
    }

    /// <summary>
    /// Resumen estructurado del resultado de un ciclo del worker (spec 012, FR-008).
    /// </summary>
    internal sealed record CycleResult(
        DateTimeOffset Timestamp,
        int Evaluated,
        int Transitioned,
        int Errors,
        TimeSpan Duration);
}
