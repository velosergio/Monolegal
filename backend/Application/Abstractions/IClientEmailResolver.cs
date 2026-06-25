using System.Threading;
using System.Threading.Tasks;

namespace Backend.Application.Abstractions;

/// <summary>
/// Resuelve la dirección de correo de un cliente a partir de su identificador (spec 013, research D2).
///
/// Desacopla la notificación por correo de la (inexistente por ahora) gestión de clientes:
/// la entidad <c>Invoice</c> sólo conoce el <c>ClientId</c>. Cuando no hay correo disponible
/// devuelve <c>null</c>/vacío, lo que el orquestador traduce en una notificación omitida (Skipped),
/// sin lanzar excepción.
/// </summary>
public interface IClientEmailResolver
{
    /// <summary>
    /// Devuelve el correo del cliente indicado, o <c>null</c>/vacío si no hay correo disponible.
    /// </summary>
    Task<string?> ResolveEmailAsync(string clientId, CancellationToken cancellationToken = default);
}
