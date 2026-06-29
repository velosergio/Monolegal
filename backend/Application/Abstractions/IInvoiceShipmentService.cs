using System.Threading;
using System.Threading.Tasks;
using Monolegal.Domain.Entities;

namespace Backend.Application.Abstractions;

/// <summary>Resultado de la cancelación de un envío (spec 019, US4).</summary>
public enum CancelNotificationStatus
{
    /// <summary>Se marcó como omitida (None → Skipped).</summary>
    Cancelled,

    /// <summary>No existe una factura con ese id.</summary>
    NotFound,

    /// <summary>La factura no está pendiente o su estado no es notificable: nada que cancelar.</summary>
    NotPending,
}

/// <summary>Resultado tipado de <see cref="IInvoiceShipmentService.CancelAsync"/>.</summary>
public sealed record CancelNotificationResult(CancelNotificationStatus Status, Invoice? Invoice);

/// <summary>
/// Acciones de envío por factura (spec 019): reenvío manual de la notificación y cancelación
/// (marcar como omitida). Reutiliza <see cref="IInvoiceTransitionNotifier"/> para el reenvío y muta
/// el contador de reintentos del dominio. Confina el proveedor de correo a Infrastructure.
/// </summary>
public interface IInvoiceShipmentService
{
    /// <summary>
    /// Reenvía la notificación correspondiente al estado actual de la factura, incrementando el
    /// contador de reintentos. Fail-soft: un fallo de envío se registra como <c>Failed</c> en la
    /// factura, no se relanza. Devuelve la factura actualizada, o <c>null</c> si no existe.
    /// </summary>
    Task<Invoice?> ResendAsync(string invoiceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// "Cancelar envío": marca como omitida (<c>None → Skipped</c>) una factura pendiente en estado
    /// notificable, para que el worker no la procese. Conserva el registro y no toca el contador.
    /// </summary>
    Task<CancelNotificationResult> CancelAsync(string invoiceId, CancellationToken cancellationToken = default);
}
