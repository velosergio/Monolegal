using System.Threading;
using System.Threading.Tasks;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;

namespace Backend.Application.Abstractions;

/// <summary>
/// Orquesta el envío de la notificación por correo cuando una factura cambia de estado
/// (spec 013, research D4). Lo reutilizan el worker de transiciones y los endpoints de
/// transición manual.
///
/// La implementación selecciona la plantilla según el nuevo estado de la factura, resuelve
/// el correo del destinatario, invoca <see cref="IEmailService"/>, registra el resultado en
/// la entidad (<c>RecordNotificationResult</c>) y actualiza los metadatos de recordatorio en
/// envíos exitosos. NO persiste: el llamador ejecuta una única actualización tras invocarla.
/// </summary>
public interface IInvoiceTransitionNotifier
{
    /// <summary>
    /// Notifica al cliente la transición de <paramref name="previousStatus"/> al estado actual
    /// de <paramref name="invoice"/>. Un fallo de envío no se propaga (salvo cancelación): se
    /// registra como resultado fallido sin revertir la transición.
    /// </summary>
    Task NotifyTransitionAsync(Invoice invoice, InvoiceStatus previousStatus, CancellationToken cancellationToken = default);
}
