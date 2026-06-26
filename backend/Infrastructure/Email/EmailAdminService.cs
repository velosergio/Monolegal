using System;
using System.Threading;
using System.Threading.Tasks;
using Backend.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;
using Monolegal.Domain.Repositories;

namespace Backend.Infrastructure.Email;

/// <summary>
/// Implementación de las herramientas globales de administración de envíos (spec 017, US4).
/// Reutiliza <see cref="IInvoiceTransitionNotifier"/> para el reenvío (misma lógica de plantilla,
/// resolución de correo, envío y registro que el worker) y muta el estado de notificación
/// embebido en <see cref="Invoice"/> para el saneamiento. Fail-soft por factura: un fallo aislado
/// no aborta el lote. No expone secretos.
/// </summary>
public sealed class EmailAdminService : IEmailAdminService
{
    private readonly IInvoiceRepository _invoices;
    private readonly IInvoiceTransitionNotifier _notifier;
    private readonly ILogger<EmailAdminService> _logger;

    private const string SanitizeReason = "saneado: notificación no registrada";

    public EmailAdminService(
        IInvoiceRepository invoices,
        IInvoiceTransitionNotifier notifier,
        ILogger<EmailAdminService> logger)
    {
        _invoices = invoices ?? throw new ArgumentNullException(nameof(invoices));
        _notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ResendFailedResult> ResendFailedAsync(CancellationToken cancellationToken = default)
    {
        var failed = await _invoices
            .GetByNotificationOutcomeAsync(NotificationOutcome.Failed, cancellationToken)
            .ConfigureAwait(false);

        int attempted = 0, resent = 0, failedAgain = 0;

        foreach (var invoice in failed)
        {
            cancellationToken.ThrowIfCancellationRequested();
            attempted++;
            try
            {
                // El estado de la factura no cambia; reusamos la lógica de notificación de la
                // transición "hacia su estado actual" (previousStatus == Status).
                await _notifier
                    .NotifyTransitionAsync(invoice, invoice.Status, cancellationToken)
                    .ConfigureAwait(false);
                await _invoices.UpdateAsync(invoice, cancellationToken).ConfigureAwait(false);

                if (invoice.LastNotificationOutcome == NotificationOutcome.Sent)
                    resent++;
                else
                    failedAgain++;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                failedAgain++;
                _logger.LogError(
                    ex,
                    "Reenvío de notificación fallida. InvoiceId={InvoiceId} Estado={Status}",
                    invoice.Id, invoice.Status);
            }
        }

        _logger.LogInformation(
            "Herramienta reenvío de fallidos. Intentadas={Attempted} Reenviadas={Resent} Fallidas={Failed}",
            attempted, resent, failedAgain);

        return new ResendFailedResult(attempted, resent, failedAgain);
    }

    public async Task<SanitizeResult> SanitizeStuckAsync(CancellationToken cancellationToken = default)
    {
        var none = await _invoices
            .GetByNotificationOutcomeAsync(NotificationOutcome.None, cancellationToken)
            .ConfigureAwait(false);

        var sanitized = 0;

        foreach (var invoice in none)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryMapNotificationType(invoice.Status, out var type))
                continue;

            try
            {
                invoice.RecordNotificationResult(type, NotificationOutcome.Failed, DateTime.UtcNow, SanitizeReason);
                await _invoices.UpdateAsync(invoice, cancellationToken).ConfigureAwait(false);
                sanitized++;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Saneamiento de notificación atascada. InvoiceId={InvoiceId} Estado={Status}",
                    invoice.Id, invoice.Status);
            }
        }

        _logger.LogInformation("Herramienta saneamiento. Saneadas={Sanitized}", sanitized);
        return new SanitizeResult(sanitized);
    }

    /// <summary>Mapea el estado notificable a su tipo de notificación (mismo criterio que el worker).</summary>
    private static bool TryMapNotificationType(InvoiceStatus status, out NotificationType type)
    {
        switch (status)
        {
            case InvoiceStatus.PrimerRecordatorio:
            case InvoiceStatus.SegundoRecordatorio:
                type = NotificationType.Reminder;
                return true;
            case InvoiceStatus.Pagado:
                type = NotificationType.PaymentConfirmation;
                return true;
            case InvoiceStatus.Desactivado:
                type = NotificationType.DeactivationNotice;
                return true;
            default:
                type = default;
                return false;
        }
    }
}
