namespace Backend.Infrastructure.Email;

/// <summary>
/// Opciones de configuración del emisor de correo SMTP (spec 013, research D1).
/// Se enlazan desde la sección "Email" de la configuración / variables de entorno
/// (<c>Email__Host</c>, <c>Email__Port</c>, ...). Las credenciales NO se hardcodean.
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>Host del servidor SMTP.</summary>
    public string? Host { get; set; }

    /// <summary>Puerto SMTP (587 STARTTLS por defecto).</summary>
    public int Port { get; set; } = 587;

    /// <summary>Usuario de autenticación SMTP (opcional).</summary>
    public string? Username { get; set; }

    /// <summary>Contraseña de autenticación SMTP (opcional; sólo por entorno/secretos).</summary>
    public string? Password { get; set; }

    /// <summary>Dirección remitente (From).</summary>
    public string From { get; set; } = "no-reply@monolegal.local";

    /// <summary>Nombre visible del remitente.</summary>
    public string FromName { get; set; } = "Monolegal";

    /// <summary>Usar STARTTLS al conectar.</summary>
    public bool UseStartTls { get; set; } = true;
}
