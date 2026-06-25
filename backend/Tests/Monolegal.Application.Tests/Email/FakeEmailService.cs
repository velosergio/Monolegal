using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Backend.Application.Abstractions;
using Monolegal.Domain.Entities;

namespace Backend.Tests.Monolegal.Application.Tests.Email;

/// <summary>
/// Doble de prueba que implementa <see cref="IEmailService"/> y registra cada invocación.
/// Demuestra la sustituibilidad del contrato (Liskov/DIP) sin un proveedor de correo real.
/// Ver specs/011-email-service-interface/contracts/IEmailService.md.
/// </summary>
internal sealed class FakeEmailService : IEmailService
{
    public List<(string ClientEmail, Invoice Invoice)> ReminderCalls { get; } = new();
    public List<(string ClientEmail, Invoice Invoice)> PaymentConfirmationCalls { get; } = new();

    public Task SendReminderAsync(string clientEmail, Invoice invoice, CancellationToken cancellationToken = default)
    {
        ReminderCalls.Add((clientEmail, invoice));
        return Task.CompletedTask;
    }

    public Task SendPaymentConfirmationAsync(string clientEmail, Invoice invoice, CancellationToken cancellationToken = default)
    {
        PaymentConfirmationCalls.Add((clientEmail, invoice));
        return Task.CompletedTask;
    }
}
