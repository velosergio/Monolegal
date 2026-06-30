using System.Collections.Generic;
using System.Globalization;
using Monolegal.Domain.Email;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;

namespace Backend.Infrastructure.Email;

/// <summary>
/// Compone el asunto y el cuerpo del correo (spec 013/017). Selecciona la plantilla efectiva
/// (personalizada en <see cref="SystemSettings.EmailTemplates"/> o el default por tipo) y sustituye
/// las variables del catálogo con <see cref="EmailTemplateRenderer"/>. Las plantillas por defecto
/// usan los marcadores canónicos para que el comportamiento sea idéntico con o sin personalización.
/// </summary>
/// <remarks>
/// SOLID: SRP — única responsabilidad: componer asunto y cuerpo del correo a partir de plantillas.
/// OCP — admite plantillas personalizadas sobre las default sin modificar la lógica de composición.
/// </remarks>
public sealed class EmailTemplateProvider
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("es-CO");

    /// <summary>Plantillas por defecto (con marcadores canónicos) por tipo de notificación.</summary>
    public static readonly IReadOnlyDictionary<NotificationType, EmailTemplate> Defaults =
        new Dictionary<NotificationType, EmailTemplate>
        {
            [NotificationType.Reminder] = new EmailTemplate
            {
                Subject = "Recordatorio de pago — Factura {{factura.id}}",
                Body = "Estimado cliente,\n\nLe recordamos que su factura {{factura.id}} por {{factura.monto}} "
                    + "se encuentra pendiente de pago. Por favor realice el pago a la mayor brevedad."
                    + "\n\nGracias,\nMonolegal",
            },
            [NotificationType.PaymentConfirmation] = new EmailTemplate
            {
                Subject = "Confirmación de pago — Factura {{factura.id}}",
                Body = "Estimado cliente,\n\nConfirmamos la recepción del pago de su factura {{factura.id}} "
                    + "por {{factura.monto}}. Gracias por su pago.\n\nMonolegal",
            },
            [NotificationType.DeactivationNotice] = new EmailTemplate
            {
                Subject = "Aviso de desactivación — Factura {{factura.id}}",
                Body = "Estimado cliente,\n\nLe informamos que su factura {{factura.id}} por {{factura.monto}} "
                    + "ha sido desactivada tras los recordatorios enviados sin recibir pago. "
                    + "Para regularizar su situación, contáctenos.\n\nMonolegal",
            },
        };

    /// <summary>Devuelve la plantilla efectiva (personalizada si existe y no está vacía; si no, el default).</summary>
    public EmailTemplate GetEffective(
        NotificationType type,
        IReadOnlyDictionary<NotificationType, EmailTemplate>? custom)
    {
        if (custom is not null
            && custom.TryGetValue(type, out var t)
            && !string.IsNullOrWhiteSpace(t.Subject)
            && !string.IsNullOrWhiteSpace(t.Body))
        {
            return t;
        }

        return Defaults.TryGetValue(type, out var def)
            ? def
            : new EmailTemplate { Subject = $"Notificación — Factura {{{{factura.id}}}}", Body = "{{factura.id}}" };
    }

    /// <summary>Renderiza el asunto/cuerpo de un tipo para una factura y destinatario concretos.</summary>
    public (string Subject, string Body) Render(
        NotificationType type,
        Invoice invoice,
        string? clientEmail,
        IReadOnlyDictionary<NotificationType, EmailTemplate>? custom)
    {
        var template = GetEffective(type, custom);
        var variables = BuildVariables(invoice, clientEmail);
        return (
            EmailTemplateRenderer.Render(template.Subject, variables),
            EmailTemplateRenderer.Render(template.Body, variables));
    }

    /// <summary>Renderiza una plantilla arbitraria con un conjunto de variables (vista previa, US2).</summary>
    public (string Subject, string Body) RenderRaw(
        string subject,
        string body,
        IReadOnlyDictionary<string, string> variables)
        => (EmailTemplateRenderer.Render(subject, variables), EmailTemplateRenderer.Render(body, variables));

    /// <summary>Construye el mapa de variables del catálogo a partir de la factura y el correo.</summary>
    public static IReadOnlyDictionary<string, string> BuildVariables(Invoice invoice, string? clientEmail)
        => new Dictionary<string, string>
        {
            [EmailTemplateVariables.FacturaId] = invoice.Id,
            [EmailTemplateVariables.FacturaMonto] = invoice.Amount.ToString("C", Culture),
            [EmailTemplateVariables.FacturaVencimiento] = string.Empty,
            [EmailTemplateVariables.FacturaEstado] = SpanishStatusLabel(invoice.Status),
            [EmailTemplateVariables.FacturaFechaEmision] = invoice.CreatedAt.ToString("yyyy-MM-dd", Culture),
            [EmailTemplateVariables.ClienteNombre] = string.Empty,
            [EmailTemplateVariables.ClienteEmail] = clientEmail ?? string.Empty,
            [EmailTemplateVariables.ClienteEmpresa] = string.Empty,
            [EmailTemplateVariables.EnlacePago] = string.Empty,
        };

    /// <summary>Conjunto de variables de ejemplo para la vista previa (sin datos reales).</summary>
    public static IReadOnlyDictionary<string, string> SampleVariables()
        => new Dictionary<string, string>
        {
            [EmailTemplateVariables.FacturaId] = "F-2026-000123",
            [EmailTemplateVariables.FacturaMonto] = 250_000m.ToString("C", Culture),
            [EmailTemplateVariables.FacturaVencimiento] = "2026-07-15",
            [EmailTemplateVariables.FacturaEstado] = "Primer recordatorio",
            [EmailTemplateVariables.FacturaFechaEmision] = "2026-06-15",
            [EmailTemplateVariables.ClienteNombre] = "Cliente de ejemplo",
            [EmailTemplateVariables.ClienteEmail] = "cliente@ejemplo.com",
            [EmailTemplateVariables.ClienteEmpresa] = "Empresa de ejemplo S.A.S.",
            [EmailTemplateVariables.EnlacePago] = "https://pagos.monolegal.local/F-2026-000123",
        };

    private static string SpanishStatusLabel(InvoiceStatus status) => status switch
    {
        InvoiceStatus.Pending => "Pendiente",
        InvoiceStatus.PrimerRecordatorio => "Primer recordatorio",
        InvoiceStatus.SegundoRecordatorio => "Segundo recordatorio",
        InvoiceStatus.Desactivado => "Desactivado",
        InvoiceStatus.Pagado => "Pagado",
        _ => status.ToString(),
    };
}
