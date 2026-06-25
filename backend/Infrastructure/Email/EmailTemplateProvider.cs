using System.Globalization;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;

namespace Backend.Infrastructure.Email;

/// <summary>
/// Compone el asunto y el cuerpo del correo según el tipo de notificación (spec 013, research D3).
/// Render simple por sustitución de placeholders a partir de los datos de la factura; sin motor
/// de plantillas pesado. Sustituible a futuro por plantillas editables sin cambiar a los consumidores.
/// </summary>
public sealed class EmailTemplateProvider
{
    public (string Subject, string Body) Render(NotificationType type, Invoice invoice)
    {
        var amount = invoice.Amount.ToString("C", CultureInfo.GetCultureInfo("es-CO"));

        return type switch
        {
            NotificationType.Reminder => (
                $"Recordatorio de pago — Factura {invoice.Id}",
                $"Estimado cliente,\n\nLe recordamos que su factura {invoice.Id} por {amount} se encuentra pendiente de pago. "
                + "Por favor realice el pago a la mayor brevedad.\n\nGracias,\nMonolegal"),

            NotificationType.PaymentConfirmation => (
                $"Confirmación de pago — Factura {invoice.Id}",
                $"Estimado cliente,\n\nConfirmamos la recepción del pago de su factura {invoice.Id} por {amount}. "
                + "Gracias por su pago.\n\nMonolegal"),

            NotificationType.DeactivationNotice => (
                $"Aviso de desactivación — Factura {invoice.Id}",
                $"Estimado cliente,\n\nLe informamos que su factura {invoice.Id} por {amount} ha sido desactivada tras los "
                + "recordatorios enviados sin recibir pago. Para regularizar su situación, contáctenos.\n\nMonolegal"),

            _ => (
                $"Notificación — Factura {invoice.Id}",
                $"Notificación relacionada con su factura {invoice.Id}.")
        };
    }
}
