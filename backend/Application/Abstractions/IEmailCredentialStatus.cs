using Monolegal.Domain.Enums;

namespace Backend.Application.Abstractions;

/// <summary>
/// Reporta el estado de la credencial de un proveedor (spec 017, FR-008) SIN exponer su valor.
/// Las credenciales se leen del entorno; este servicio nunca devuelve el secreto.
/// </summary>
public interface IEmailCredentialStatus
{
    /// <summary>
    /// Estado de la credencial del proveedor indicado: <c>NotConfigured</c> si el secreto no está
    /// en el entorno; <c>Configured</c> si está presente; <c>Validated</c> si la última validación
    /// en esta sesión fue exitosa para ese proveedor.
    /// </summary>
    EmailCredentialState GetStatus(EmailProvider provider);

    /// <summary>Marca el resultado de la última validación de un proveedor (estado efímero).</summary>
    void MarkValidation(EmailProvider provider, bool succeeded);
}
