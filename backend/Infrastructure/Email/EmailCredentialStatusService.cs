using System;
using System.Collections.Concurrent;
using Backend.Application.Abstractions;
using Microsoft.Extensions.Options;
using DomainEmailProvider = Monolegal.Domain.Enums.EmailProvider;

namespace Backend.Infrastructure.Email;

/// <summary>
/// Reporta el estado de la credencial de cada proveedor (spec 017, FR-008) SIN exponer su valor.
/// La presencia del secreto se determina por el entorno (<see cref="EmailOptions"/>); el resultado
/// "Validated" es efímero en memoria y se actualiza al validar exitosamente desde la UI.
/// </summary>
public sealed class EmailCredentialStatusService : IEmailCredentialStatus
{
    private readonly EmailOptions _options;
    private readonly ConcurrentDictionary<DomainEmailProvider, bool> _validated = new();

    public EmailCredentialStatusService(IOptions<EmailOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public EmailCredentialState GetStatus(DomainEmailProvider provider)
    {
        if (!IsSecretPresent(provider))
            return EmailCredentialState.NotConfigured;

        if (_validated.TryGetValue(provider, out var ok))
            return ok ? EmailCredentialState.Validated : EmailCredentialState.Invalid;

        return EmailCredentialState.Configured;
    }

    public void MarkValidation(DomainEmailProvider provider, bool succeeded)
        => _validated[provider] = succeeded;

    private bool IsSecretPresent(DomainEmailProvider provider) => provider switch
    {
        // SMTP: se considera configurado cuando hay host (la contraseña puede ser opcional).
        DomainEmailProvider.Smtp => !string.IsNullOrWhiteSpace(_options.Host),
        DomainEmailProvider.Resend => !string.IsNullOrWhiteSpace(_options.Resend.ApiKey),
        _ => false,
    };
}
