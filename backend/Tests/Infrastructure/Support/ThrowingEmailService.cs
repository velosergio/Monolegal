using System;
using System.Threading;
using System.Threading.Tasks;
using Backend.Application.Abstractions;
using Monolegal.Domain.Entities;

namespace Backend.Tests.Infrastructure.Support;

/// <summary>
/// Doble de prueba de <see cref="IEmailService"/> que siempre falla (spec 013). Simula un
/// proveedor de correo caído para verificar el manejo de fallos del orquestador.
/// </summary>
public sealed class ThrowingEmailService : IEmailService
{
    private readonly string _message;

    public ThrowingEmailService(string message = "Proveedor SMTP no disponible")
    {
        _message = message;
    }

    public Task SendReminderAsync(string clientEmail, Invoice invoice, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(_message);

    public Task SendPaymentConfirmationAsync(string clientEmail, Invoice invoice, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(_message);

    public Task SendDeactivationNoticeAsync(string clientEmail, Invoice invoice, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(_message);
}
