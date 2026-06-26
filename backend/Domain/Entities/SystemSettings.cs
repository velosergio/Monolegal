using System;
using System.Collections.Generic;
using Monolegal.Domain.Enums;

namespace Monolegal.Domain.Entities;

public class SystemSettings
{
    public string Id { get; set; } = string.Empty;
    public InvoiceTransitionsConfig InvoiceTransitions { get; set; } = new();

    /// <summary>Configuración NO secreta del proveedor de email (spec 017).</summary>
    public EmailSettings Email { get; set; } = new();

    /// <summary>Plantillas de correo personalizadas por tipo (spec 017). Vacío ⇒ se usan defaults.</summary>
    public Dictionary<NotificationType, EmailTemplate> EmailTemplates { get; set; } = new();

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public void UpdateTransitions(InvoiceTransitionsConfig config)
    {
        InvoiceTransitions = config;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Reemplaza la configuración NO secreta del email (spec 017, FR-005).</summary>
    public void UpdateEmailSettings(EmailSettings settings)
    {
        Email = settings ?? throw new ArgumentNullException(nameof(settings));
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Crea o actualiza la plantilla de un tipo de notificación (spec 017, FR-013).</summary>
    public void UpdateTemplate(NotificationType type, string subject, string body)
    {
        EmailTemplates[type] = new EmailTemplate { Subject = subject, Body = body };
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Elimina la personalización de un tipo, volviendo al contenido por defecto (FR-014).</summary>
    public void ResetTemplate(NotificationType type)
    {
        if (EmailTemplates.Remove(type))
            UpdatedAt = DateTime.UtcNow;
    }
}

public class InvoiceTransitionsConfig
{
    public int PendingToFirstReminderDays { get; set; } = 3;
    public int FirstToSecondReminderDays { get; set; } = 3;
    public int SecondToDeactivatedDays { get; set; } = 3;
}

/// <summary>
/// Configuración NO secreta del proveedor de email (spec 017, data-model §1.1). NUNCA contiene
/// credenciales secretas (contraseña SMTP, API key de Resend), que se gestionan solo por entorno.
/// </summary>
public class EmailSettings
{
    /// <summary>Proveedor activo de envío. Default SMTP.</summary>
    public EmailProvider ActiveProvider { get; set; } = EmailProvider.Smtp;

    /// <summary>Dirección remitente (From).</summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>Nombre visible del remitente.</summary>
    public string FromName { get; set; } = string.Empty;

    /// <summary>Parámetros no secretos de SMTP.</summary>
    public SmtpSettings Smtp { get; set; } = new();

    /// <summary>Parámetros no secretos de Resend.</summary>
    public ResendSettings Resend { get; set; } = new();
}

public class SmtpSettings
{
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public bool UseStartTls { get; set; } = true;
}

public class ResendSettings
{
    public string? FromDomain { get; set; }
}

/// <summary>Plantilla editable de correo (spec 017). Asunto y cuerpo con variables del catálogo.</summary>
public class EmailTemplate
{
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}
