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

    /// <summary>
    /// Opciones del proveedor Resend (spec 017). La API key es un secreto: solo por
    /// variables de entorno (<c>Email__Resend__ApiKey</c>), nunca persistida en BD.
    /// </summary>
    public ResendOptions Resend { get; set; } = new();
}

/// <summary>
/// Opciones del proveedor Resend (spec 017, D8). <see cref="ApiKey"/> es secreta y se lee
/// solo del entorno; <see cref="FromDomain"/> es un parámetro no secreto que también puede
/// editarse desde la vista de configuración (la BD tiene prioridad cuando existe).
/// </summary>
public sealed class ResendOptions
{
    /// <summary>API key de Resend (SECRETA; solo entorno: <c>Email__Resend__ApiKey</c>).</summary>
    public string? ApiKey { get; set; }

    /// <summary>Dominio remitente verificado en Resend (no secreto).</summary>
    public string? FromDomain { get; set; }
}
