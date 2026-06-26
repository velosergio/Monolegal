using System.Threading;
using System.Threading.Tasks;
using Monolegal.Domain.Enums;

namespace Backend.Application.Abstractions;

/// <summary>Mensaje de correo ya compuesto (asunto/cuerpo renderizados) listo para enviar.</summary>
public sealed record EmailMessage(string To, string Subject, string Body);

/// <summary>Estado de la credencial / resultado de validación de un proveedor (spec 017).</summary>
public enum EmailCredentialState
{
    /// <summary>El secreto del proveedor no está presente en el entorno.</summary>
    NotConfigured = 0,

    /// <summary>Secreto presente; sin validación exitosa en esta sesión.</summary>
    Configured = 1,

    /// <summary>Última validación contra el proveedor fue exitosa.</summary>
    Validated = 2,

    /// <summary>La validación falló (credencial rechazada o error del proveedor).</summary>
    Invalid = 3
}

/// <summary>Resultado de validar la configuración de un proveedor.</summary>
public sealed record EmailValidationResult(EmailCredentialState State, string? Message);

/// <summary>
/// Abstracción de bajo nivel de un proveedor de envío (SMTP/Resend) — spec 017, D1. El proveedor
/// concreto vive en Infrastructure; lee su configuración no secreta de <c>SystemSettings.Email</c>
/// y sus credenciales del entorno. Permite alternar el proveedor activo en runtime.
/// </summary>
public interface IEmailProvider
{
    /// <summary>Proveedor que implementa esta instancia.</summary>
    EmailProvider Provider { get; }

    /// <summary>Envía un mensaje ya compuesto. Lanza excepción ante fallo de envío.</summary>
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);

    /// <summary>Valida la configuración/credencial sin enviar correo.</summary>
    Task<EmailValidationResult> ValidateAsync(CancellationToken cancellationToken = default);
}
