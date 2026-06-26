using FluentValidation;
using Monolegal.Domain.Enums;

namespace Backend.Application.Validation;

/// <summary>Parámetros SMTP no secretos de la petición (sin contraseña).</summary>
public sealed record SmtpSettingsRequest(string? Host = null, int Port = 587, string? Username = null, bool UseStartTls = true);

/// <summary>Parámetros Resend no secretos de la petición (sin API key).</summary>
public sealed record ResendSettingsRequest(string? FromDomain = null);

/// <summary>
/// Cuerpo de PUT /api/settings/email (spec 017, contrato email-settings-api.md). SOLO contiene
/// configuración NO secreta; las credenciales (contraseña SMTP, API key Resend) nunca forman parte
/// del contrato y se gestionan exclusivamente por variables de entorno.
/// </summary>
public sealed record EmailSettingsRequest(
    EmailProvider ActiveProvider,
    string? FromAddress,
    string? FromName,
    SmtpSettingsRequest? Smtp,
    ResendSettingsRequest? Resend);

/// <summary>
/// Validador del cuerpo de actualización de configuración de email (spec 017, FR-005/FR-006).
/// Requiere remitente válido y los parámetros propios del proveedor activo.
/// </summary>
public sealed class UpdateEmailSettingsValidator : AbstractValidator<EmailSettingsRequest>
{
    public UpdateEmailSettingsValidator()
    {
        RuleFor(x => x.FromAddress)
            .NotEmpty().WithMessage("El campo 'fromAddress' es obligatorio.")
            .EmailAddress().WithMessage("El campo 'fromAddress' debe ser un correo válido.");

        RuleFor(x => x.FromName)
            .NotEmpty().WithMessage("El campo 'fromName' es obligatorio.");

        When(x => x.ActiveProvider == EmailProvider.Smtp, () =>
        {
            RuleFor(x => x.Smtp == null ? null : x.Smtp.Host)
                .NotEmpty().WithName("smtp.host")
                .WithMessage("El campo 'smtp.host' es obligatorio para el proveedor SMTP.");

            RuleFor(x => x.Smtp == null ? 587 : x.Smtp.Port)
                .InclusiveBetween(1, 65535).WithName("smtp.port")
                .WithMessage("El campo 'smtp.port' debe estar entre 1 y 65535.");
        });

        When(x => x.ActiveProvider == EmailProvider.Resend, () =>
        {
            RuleFor(x => x.Resend == null ? null : x.Resend.FromDomain)
                .NotEmpty().WithName("resend.fromDomain")
                .WithMessage("El campo 'resend.fromDomain' es obligatorio para el proveedor Resend.");
        });
    }
}
