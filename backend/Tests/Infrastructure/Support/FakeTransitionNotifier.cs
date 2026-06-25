using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Backend.Application.Abstractions;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;

namespace Backend.Tests.Infrastructure.Support;

/// <summary>
/// Doble de prueba no-op de <see cref="IInvoiceTransitionNotifier"/> (spec 013). Registra las
/// invocaciones sin enviar correo ni mutar la factura. Útil para pruebas del worker/endpoints
/// que no ejercitan el envío real.
/// </summary>
public sealed class FakeTransitionNotifier : IInvoiceTransitionNotifier
{
    public List<(string InvoiceId, InvoiceStatus PreviousStatus, InvoiceStatus NewStatus)> Calls { get; } = new();

    public Task NotifyTransitionAsync(Invoice invoice, InvoiceStatus previousStatus, CancellationToken cancellationToken = default)
    {
        Calls.Add((invoice.Id, previousStatus, invoice.Status));
        return Task.CompletedTask;
    }
}
