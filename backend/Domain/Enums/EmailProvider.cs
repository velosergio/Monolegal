namespace Monolegal.Domain.Enums;

/// <summary>
/// Proveedor de envío de correo activo del sistema (spec 017). Conmutable en runtime desde
/// la vista de configuración; el proveedor concreto vive en la capa Infrastructure.
/// </summary>
public enum EmailProvider
{
    /// <summary>Envío por SMTP (MailKit).</summary>
    Smtp = 0,

    /// <summary>Envío por la API de Resend.</summary>
    Resend = 1
}
