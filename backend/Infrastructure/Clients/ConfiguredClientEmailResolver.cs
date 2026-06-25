using System.Threading;
using System.Threading.Tasks;
using Backend.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Backend.Infrastructure.Clients;

/// <summary>
/// Resolver inicial del correo del cliente (spec 013, research D2). Mientras no exista una
/// entidad/colección de clientes, resuelve el correo desde la sección de configuración
/// "ClientEmails" (mapa <c>ClientId → correo</c>). Si no hay correo configurado para el cliente
/// devuelve <c>null</c>, lo que el orquestador traduce en una notificación omitida (Skipped).
///
/// La gestión completa de clientes (con su correo) es una feature futura del roadmap.
/// </summary>
public sealed class ConfiguredClientEmailResolver : IClientEmailResolver
{
    private readonly IConfiguration _configuration;

    public ConfiguredClientEmailResolver(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new System.ArgumentNullException(nameof(configuration));
    }

    public Task<string?> ResolveEmailAsync(string clientId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            return Task.FromResult<string?>(null);

        // Sección "ClientEmails": { "<clientId>": "correo@dominio" }
        var email = _configuration.GetSection("ClientEmails")[clientId];
        return Task.FromResult(string.IsNullOrWhiteSpace(email) ? null : email);
    }
}
