using System.Threading;
using System.Threading.Tasks;
using Monolegal.Domain.Entities;

namespace Backend.Application.Abstractions;

/// <summary>
/// Contrato de notificaciones por correo del sistema. Desacopla a los consumidores
/// (worker de transiciones, futuros endpoints) del proveedor de correo concreto
/// (SMTP/API), cuya implementación vivirá en la capa Infrastructure
/// (ver specs/011-email-service-interface + Spec 3.3 del roadmap).
///
/// Los fallos de envío se señalan mediante excepciones; este contrato no define
/// un tipo de resultado de error. La validación del formato del correo y el manejo
/// de fallos del proveedor son responsabilidad de la implementación y del invocador.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Envía un correo de recordatorio de cobro para una factura activa.
    /// </summary>
    /// <param name="clientEmail">Dirección de correo del destinatario (no nula/no vacía).</param>
    /// <param name="invoice">Factura asociada usada para componer el contenido del correo.</param>
    /// <param name="cancellationToken">Cancelación cooperativa (p. ej. apagado del worker).</param>
    Task SendReminderAsync(string clientEmail, Invoice invoice, CancellationToken cancellationToken = default);

    /// <summary>
    /// Envía un correo de confirmación cuando una factura pasa a estado pagado.
    /// </summary>
    /// <param name="clientEmail">Dirección de correo del destinatario (no nula/no vacía).</param>
    /// <param name="invoice">Factura pagada usada para componer el contenido del correo.</param>
    /// <param name="cancellationToken">Cancelación cooperativa (p. ej. apagado del worker).</param>
    Task SendPaymentConfirmationAsync(string clientEmail, Invoice invoice, CancellationToken cancellationToken = default);

    /// <summary>
    /// Envía un correo de aviso de desactivación / última notificación cuando una factura
    /// pasa a estado desactivado (spec 013, research D5).
    /// </summary>
    /// <param name="clientEmail">Dirección de correo del destinatario (no nula/no vacía).</param>
    /// <param name="invoice">Factura desactivada usada para componer el contenido del correo.</param>
    /// <param name="cancellationToken">Cancelación cooperativa (p. ej. apagado del worker).</param>
    Task SendDeactivationNoticeAsync(string clientEmail, Invoice invoice, CancellationToken cancellationToken = default);
}
