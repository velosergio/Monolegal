using System;
using System.Threading;
using System.Threading.Tasks;
using Backend.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;
using Monolegal.Domain.Repositories;

namespace Backend.Application.Services;

/// <summary>
/// Implementación de las acciones de envío por factura (spec 019, US2/US4). Reutiliza
/// <see cref="IInvoiceTransitionNotifier"/> para el reenvío (misma lógica de plantilla, resolución
/// de correo, envío y registro que el worker / herramientas masivas) e incrementa el contador de
/// reintentos del dominio. La cancelación marca como omitida (<c>Skipped</c>) las facturas pendientes
/// en estado notificable. No expone secretos.
/// </summary>
public sealed class InvoiceShipmentService : IInvoiceShipmentService
{
    private readonly IInvoiceRepository _invoices;
    private readonly IInvoiceTransitionNotifier _notifier;
    private readonly ILogger<InvoiceShipmentService> _logger;

    private const string CancelReason = "cancelado por el administrador";

    public InvoiceShipmentService(
        IInvoiceRepository invoices,
        IInvoiceTransitionNotifier notifier,
        ILogger<InvoiceShipmentService> logger)
    {
        _invoices = invoices ?? throw new ArgumentNullException(nameof(invoices));
        _notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Invoice?> ResendAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoices.GetByIdAsync(invoiceId, cancellationToken).ConfigureAwait(false);
        if (invoice is null)
            return null;

        // Reintento del aviso vigente: incrementa el contador antes de re-notificar.
        invoice.RecordNotificationRetry();

        // El estado no cambia: re-notificamos "hacia el estado actual" (previousStatus == Status).
        await _notifier
            .NotifyTransitionAsync(invoice, invoice.Status, cancellationToken)
            .ConfigureAwait(false);

        await _invoices.UpdateAsync(invoice, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Reenvío por factura. InvoiceId={InvoiceId} Estado={Status} Resultado={Outcome} Reintentos={RetryCount}",
            invoice.Id, invoice.Status, invoice.LastNotificationOutcome, invoice.NotificationRetryCount);

        return invoice;
    }

    public async Task<CancelNotificationResult> CancelAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoices.GetByIdAsync(invoiceId, cancellationToken).ConfigureAwait(false);
        if (invoice is null)
            return new CancelNotificationResult(CancelNotificationStatus.NotFound, null);

        // Sólo se cancela un envío pendiente (None) en un estado con notificación aplicable.
        if (invoice.LastNotificationOutcome != NotificationOutcome.None
            || !TryMapNotificationType(invoice.Status, out var type))
        {
            return new CancelNotificationResult(CancelNotificationStatus.NotPending, invoice);
        }

        invoice.RecordNotificationResult(type, NotificationOutcome.Skipped, DateTime.UtcNow, CancelReason);
        await _invoices.UpdateAsync(invoice, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Cancelación de envío. InvoiceId={InvoiceId} Estado={Status} Resultado=Skipped",
            invoice.Id, invoice.Status);

        return new CancelNotificationResult(CancelNotificationStatus.Cancelled, invoice);
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
