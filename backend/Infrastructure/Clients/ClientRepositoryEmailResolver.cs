using System.Threading;
using System.Threading.Tasks;
using Backend.Application.Abstractions;
using Monolegal.Domain.Repositories;

namespace Backend.Infrastructure.Clients;

/// <summary>
/// Resolver del correo del cliente respaldado por la colección <c>Clients</c> (spec 018, research D8).
/// Una vez que el cliente es una entidad de primera clase con email persistido, el correo se resuelve
/// desde el repositorio. Para no romper entornos sin clientes migrados, recurre a un
/// <paramref name="fallback"/> (típicamente <see cref="ConfiguredClientEmailResolver"/>, basado en
/// la sección de configuración <c>ClientEmails</c>) cuando el cliente no existe o no tiene email.
/// </summary>
/// <remarks>
/// SOLID: DIP — implementa <see cref="IClientEmailResolver"/> y delega en un fallback
/// <see cref="IClientEmailResolver"/> inyectado. OCP — la estrategia de resolución se compone
/// (repositorio + fallback) sin modificar a los consumidores. SRP — resolver el correo del cliente.
/// </remarks>
public sealed class ClientRepositoryEmailResolver : IClientEmailResolver
{
    private readonly IClientRepository _clients;
    private readonly IClientEmailResolver _fallback;

    public ClientRepositoryEmailResolver(IClientRepository clients, IClientEmailResolver fallback)
    {
        _clients = clients;
        _fallback = fallback;
    }

    public async Task<string?> ResolveEmailAsync(string clientId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            return null;

        var client = await _clients.GetByIdAsync(clientId, cancellationToken).ConfigureAwait(false);
        if (client is not null && !string.IsNullOrWhiteSpace(client.Email))
            return client.Email;

        return await _fallback.ResolveEmailAsync(clientId, cancellationToken).ConfigureAwait(false);
    }
}
