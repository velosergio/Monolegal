using System;
using System.Threading;
using System.Threading.Tasks;
using Backend.Application.Abstractions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Repositories;
using DomainEmailProvider = Monolegal.Domain.Enums.EmailProvider;

namespace Backend.Infrastructure.Email;

/// <summary>
/// Proveedor SMTP (MailKit) — spec 017. Lee la configuración NO secreta (host/puerto/usuario/
/// remitente) de <see cref="SystemSettings.Email"/> con respaldo en <see cref="EmailOptions"/>
/// (entorno) y la contraseña SOLO del entorno. Se evalúa en cada envío para soportar cambios en
/// runtime sin reinicio (FR-002b).
/// </summary>
/// <remarks>
/// SOLID: DIP/OCP — implementa <see cref="IEmailProvider"/>; se suma a la factory sin modificar consumidores.
/// LSP — intercambiable con <see cref="ResendEmailProvider"/>. SRP — única responsabilidad: enviar vía SMTP.
/// </remarks>
public sealed class SmtpEmailProvider : IEmailProvider
{
    private readonly ISystemSettingsRepository _settings;
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailProvider> _logger;

    public SmtpEmailProvider(
        ISystemSettingsRepository settings,
        IOptions<EmailOptions> options,
        ILogger<SmtpEmailProvider> logger)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public DomainEmailProvider Provider => DomainEmailProvider.Smtp;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var cfg = await ResolveConfigAsync().ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(cfg.Host))
            throw new InvalidOperationException("El host SMTP no está configurado (Email__Host).");

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(cfg.FromName, cfg.From));
        mime.To.Add(MailboxAddress.Parse(message.To));
        mime.Subject = message.Subject;
        mime.Body = new TextPart("plain") { Text = message.Body };

        using var client = new SmtpClient();
        var secure = cfg.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;

        await client.ConnectAsync(cfg.Host, cfg.Port, secure, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(cfg.Username))
            await client.AuthenticateAsync(cfg.Username, cfg.Password ?? string.Empty, cancellationToken).ConfigureAwait(false);
        await client.SendAsync(mime, cancellationToken).ConfigureAwait(false);
        await client.DisconnectAsync(quit: true, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Correo SMTP enviado. Destinatario={To}", message.To);
    }

    public async Task<EmailValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
    {
        var cfg = await ResolveConfigAsync().ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(cfg.Host))
            return new EmailValidationResult(EmailCredentialState.NotConfigured, "El host SMTP no está configurado.");

        try
        {
            using var client = new SmtpClient();
            var secure = cfg.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
            await client.ConnectAsync(cfg.Host, cfg.Port, secure, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(cfg.Username))
                await client.AuthenticateAsync(cfg.Username, cfg.Password ?? string.Empty, cancellationToken).ConfigureAwait(false);
            await client.DisconnectAsync(quit: true, cancellationToken).ConfigureAwait(false);

            return new EmailValidationResult(EmailCredentialState.Validated, "Conexión SMTP verificada.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fallo al validar la configuración SMTP.");
            return new EmailValidationResult(EmailCredentialState.Invalid, $"No se pudo validar SMTP: {ex.Message}");
        }
    }

    private async Task<SmtpConfig> ResolveConfigAsync()
    {
        var settings = await _settings.GetSettingsAsync().ConfigureAwait(false);
        var smtp = settings.Email.Smtp;
        return new SmtpConfig(
            Host: string.IsNullOrWhiteSpace(smtp.Host) ? _options.Host : smtp.Host,
            Port: smtp.Port > 0 ? smtp.Port : _options.Port,
            Username: string.IsNullOrWhiteSpace(smtp.Username) ? _options.Username : smtp.Username,
            Password: _options.Password,
            UseStartTls: smtp.UseStartTls,
            From: string.IsNullOrWhiteSpace(settings.Email.FromAddress) ? _options.From : settings.Email.FromAddress,
            FromName: string.IsNullOrWhiteSpace(settings.Email.FromName) ? _options.FromName : settings.Email.FromName);
    }

    private sealed record SmtpConfig(
        string? Host, int Port, string? Username, string? Password, bool UseStartTls, string From, string FromName);
}
