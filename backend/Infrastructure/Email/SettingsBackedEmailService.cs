using System;
using System.Threading;
using System.Threading.Tasks;
using Backend.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;
using Monolegal.Domain.Repositories;

namespace Backend.Infrastructure.Email;

/// <summary>
/// Implementación de <see cref="IEmailService"/> respaldada por la configuración persistida
/// (spec 017). En cada envío resuelve el proveedor activo y las plantillas desde
/// <see cref="SystemSettings"/> (cambios efectivos en runtime, FR-002b), compone el mensaje y
/// delega en el <see cref="IEmailProvider"/> correspondiente. Los fallos se propagan como
/// excepción para que el orquestador los registre como Failed.
/// </summary>
public sealed class SettingsBackedEmailService : IEmailService
{
    private readonly ISystemSettingsRepository _settings;
    private readonly EmailTemplateProvider _templates;
    private readonly IEmailProviderFactory _factory;
    private readonly ILogger<SettingsBackedEmailService> _logger;

    public SettingsBackedEmailService(
        ISystemSettingsRepository settings,
        EmailTemplateProvider templates,
        IEmailProviderFactory factory,
        ILogger<SettingsBackedEmailService> logger)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _templates = templates ?? throw new ArgumentNullException(nameof(templates));
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task SendReminderAsync(string clientEmail, Invoice invoice, CancellationToken cancellationToken = default)
        => SendAsync(NotificationType.Reminder, clientEmail, invoice, cancellationToken);

    public Task SendPaymentConfirmationAsync(string clientEmail, Invoice invoice, CancellationToken cancellationToken = default)
        => SendAsync(NotificationType.PaymentConfirmation, clientEmail, invoice, cancellationToken);

    public Task SendDeactivationNoticeAsync(string clientEmail, Invoice invoice, CancellationToken cancellationToken = default)
        => SendAsync(NotificationType.DeactivationNotice, clientEmail, invoice, cancellationToken);

    private async Task SendAsync(
        NotificationType type,
        string clientEmail,
        Invoice invoice,
        CancellationToken cancellationToken)
    {
        var settings = await _settings.GetSettingsAsync().ConfigureAwait(false);
        var (subject, body) = _templates.Render(type, invoice, clientEmail, settings.EmailTemplates);

        var provider = _factory.Resolve(settings.Email.ActiveProvider);
        await provider.SendAsync(new EmailMessage(clientEmail, subject, body), cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Correo enviado. InvoiceId={InvoiceId} Tipo={NotificationType} Proveedor={Provider}",
            invoice.Id, type, settings.Email.ActiveProvider);
    }
}
