using System.Threading;
using System.Threading.Tasks;
using Backend.Application.Abstractions;

namespace Backend.Tests.Infrastructure.Support;

/// <summary>
/// Doble de prueba de <see cref="IClientEmailResolver"/> (spec 013). Devuelve un correo
/// configurable; con <c>null</c> simula un cliente sin correo (notificación omitida).
/// </summary>
public sealed class FakeClientEmailResolver : IClientEmailResolver
{
    private readonly string? _email;

    public FakeClientEmailResolver(string? email = "cliente@correo.com")
    {
        _email = email;
    }

    public Task<string?> ResolveEmailAsync(string clientId, CancellationToken cancellationToken = default)
        => Task.FromResult(_email);
}
