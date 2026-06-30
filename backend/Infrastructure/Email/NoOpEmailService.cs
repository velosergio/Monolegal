using System.Threading;
using System.Threading.Tasks;
using Backend.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;

namespace Backend.Infrastructure.Email;

/// <summary>
/// Implementación de <see cref="IEmailService"/> para Desarrollo/CI (spec 013, research D1).
/// No envía correo real: registra un log estructurado y completa con éxito. Permite ejecutar
/// los flujos de transición y notificación sin un servidor SMTP.
/// </summary>
/// <remarks>
/// SOLID: LSP — sustituye a cualquier <see cref="IEmailService"/> en Dev/CI sin romper a los consumidores.
/// DIP — se inyecta vía la abstracción. SRP — única responsabilidad: simular el envío sin servidor real.
/// </remarks>
public sealed class NoOpEmailService : IEmailService
{
    private readonly ILogger<NoOpEmailService> _logger;

    public NoOpEmailService(ILogger<NoOpEmailService> logger)
    {
        _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
    }

    public Task SendReminderAsync(string clientEmail, Invoice invoice, CancellationToken cancellationToken = default)
        => LogAndComplete(NotificationType.Reminder, clientEmail, invoice);

    public Task SendPaymentConfirmationAsync(string clientEmail, Invoice invoice, CancellationToken cancellationToken = default)
        => LogAndComplete(NotificationType.PaymentConfirmation, clientEmail, invoice);

    public Task SendDeactivationNoticeAsync(string clientEmail, Invoice invoice, CancellationToken cancellationToken = default)
        => LogAndComplete(NotificationType.DeactivationNotice, clientEmail, invoice);

    private Task LogAndComplete(NotificationType type, string clientEmail, Invoice invoice)
    {
        _logger.LogInformation(
            "NoOpEmailService — correo simulado (no enviado). InvoiceId={InvoiceId} Tipo={NotificationType} Destinatario={ClientEmail}",
            invoice.Id, type, clientEmail);
        return Task.CompletedTask;
    }
}
