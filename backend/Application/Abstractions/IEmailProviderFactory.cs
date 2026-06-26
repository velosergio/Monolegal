using Monolegal.Domain.Enums;

namespace Backend.Application.Abstractions;

/// <summary>
/// Resuelve la instancia de <see cref="IEmailProvider"/> correspondiente a un proveedor
/// (spec 017, D1). Permite seleccionar el proveedor activo en runtime sin acoplar a los
/// consumidores del envío con las implementaciones concretas.
/// </summary>
public interface IEmailProviderFactory
{
    /// <summary>Devuelve el proveedor que implementa <paramref name="provider"/>.</summary>
    IEmailProvider Resolve(EmailProvider provider);
}
