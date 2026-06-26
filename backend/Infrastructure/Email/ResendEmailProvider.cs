using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Backend.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monolegal.Domain.Repositories;
using DomainEmailProvider = Monolegal.Domain.Enums.EmailProvider;

namespace Backend.Infrastructure.Email;

/// <summary>
/// Proveedor de envío vía API de Resend (spec 017, D8). La API key es SECRETA y se lee solo del
/// entorno (<c>Email__Resend__ApiKey</c>); el dominio remitente y el From son configuración no
/// secreta tomada de <see cref="SystemSettings.Email"/>. Se evalúa por envío (cambios en runtime).
/// </summary>
public sealed class ResendEmailProvider : IEmailProvider
{
    private const string SendEndpoint = "https://api.resend.com/emails";
    private const string DomainsEndpoint = "https://api.resend.com/domains";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISystemSettingsRepository _settings;
    private readonly EmailOptions _options;
    private readonly ILogger<ResendEmailProvider> _logger;

    public ResendEmailProvider(
        IHttpClientFactory httpClientFactory,
        ISystemSettingsRepository settings,
        IOptions<EmailOptions> options,
        ILogger<ResendEmailProvider> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public DomainEmailProvider Provider => DomainEmailProvider.Resend;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var apiKey = _options.Resend.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("La API key de Resend no está configurada (Email__Resend__ApiKey).");

        var settings = await _settings.GetSettingsAsync().ConfigureAwait(false);
        var from = ResolveFrom(settings.Email.FromAddress, settings.Email.FromName);

        using var request = new HttpRequestMessage(HttpMethod.Post, SendEndpoint)
        {
            Content = JsonContent.Create(new
            {
                from,
                to = new[] { message.To },
                subject = message.Subject,
                text = message.Body,
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var client = _httpClientFactory.CreateClient("resend");
        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException(
                $"Resend rechazó el envío (HTTP {(int)response.StatusCode}). {Truncate(detail)}");
        }

        _logger.LogInformation("Correo Resend enviado. Destinatario={To}", message.To);
    }

    public async Task<EmailValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
    {
        var apiKey = _options.Resend.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
            return new EmailValidationResult(EmailCredentialState.NotConfigured, "La API key de Resend no está configurada.");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, DomainsEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            using var client = _httpClientFactory.CreateClient("resend");
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
                return new EmailValidationResult(EmailCredentialState.Validated, "API key de Resend verificada.");

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return new EmailValidationResult(EmailCredentialState.Invalid, "La API key de Resend fue rechazada.");

            return new EmailValidationResult(
                EmailCredentialState.Invalid, $"Resend respondió HTTP {(int)response.StatusCode}.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fallo al validar la API key de Resend.");
            return new EmailValidationResult(EmailCredentialState.Invalid, $"No se pudo validar Resend: {ex.Message}");
        }
    }

    private string ResolveFrom(string fromAddress, string fromName)
    {
        var address = string.IsNullOrWhiteSpace(fromAddress) ? _options.From : fromAddress;
        var name = string.IsNullOrWhiteSpace(fromName) ? _options.FromName : fromName;
        return string.IsNullOrWhiteSpace(name) ? address : $"{name} <{address}>";
    }

    private static string Truncate(string? value)
        => string.IsNullOrEmpty(value) ? string.Empty : (value.Length <= 300 ? value : value[..300]);
}
