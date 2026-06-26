using System;
using System.Collections.Generic;
using System.Linq;
using Backend.Application.Abstractions;
using DomainEmailProvider = Monolegal.Domain.Enums.EmailProvider;

namespace Backend.Infrastructure.Email;

/// <summary>
/// Resuelve el <see cref="IEmailProvider"/> concreto a partir del proveedor activo (spec 017, D1).
/// Recibe todas las implementaciones registradas y selecciona por <see cref="IEmailProvider.Provider"/>.
/// </summary>
public sealed class EmailProviderFactory : IEmailProviderFactory
{
    private readonly IReadOnlyDictionary<DomainEmailProvider, IEmailProvider> _providers;

    public EmailProviderFactory(IEnumerable<IEmailProvider> providers)
    {
        if (providers is null) throw new ArgumentNullException(nameof(providers));
        _providers = providers.ToDictionary(p => p.Provider);
    }

    public IEmailProvider Resolve(DomainEmailProvider provider)
    {
        if (_providers.TryGetValue(provider, out var impl))
            return impl;

        throw new InvalidOperationException($"No hay un proveedor de email registrado para '{provider}'.");
    }
}
