using System.Threading;
using System.Threading.Tasks;
using Backend.Application.Abstractions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;

namespace Backend.Infrastructure.Email;

/// <summary>
/// Implementación SMTP de <see cref="IEmailService"/> usando MailKit (spec 013, research D1).
/// Compone el correo con <see cref="EmailTemplateProvider"/> y lo envía por SMTP con las
/// credenciales de <see cref="EmailOptions"/> (provenientes de variables de entorno).
/// Los fallos de envío se propagan como excepción (el orquestador los registra como Failed).
/// </summary>
public sealed class SmtpEmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly EmailTemplateProvider _templates;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IOptions<EmailOptions> options,
        EmailTemplateProvider templates,
        ILogger<SmtpEmailService> logger)
    {
        _options = options?.Value ?? throw new System.ArgumentNullException(nameof(options));
        _templates = templates ?? throw new System.ArgumentNullException(nameof(templates));
        _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
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
        var (subject, body) = _templates.Render(type, invoice);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.From));
        message.To.Add(MailboxAddress.Parse(clientEmail));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();

        var secureOption = _options.UseStartTls
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.Auto;

        await client.ConnectAsync(_options.Host, _options.Port, secureOption, cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(_options.Username))
            await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken).ConfigureAwait(false);

        await client.SendAsync(message, cancellationToken).ConfigureAwait(false);
        await client.DisconnectAsync(quit: true, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Correo SMTP enviado. InvoiceId={InvoiceId} Tipo={NotificationType}",
            invoice.Id, type);
    }
}
