using System;
using System.Threading;
using System.Threading.Tasks;
using Backend.Application.Abstractions;
using Backend.Application.Services;
using Backend.Tests.Infrastructure.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;
using Shouldly;
using Xunit;

namespace Backend.Tests.Monolegal.Application.Tests;

/// <summary>
/// Pruebas de aplicación de <see cref="InvoiceShipmentService.ResendAsync"/> (spec 019, US2):
/// incrementa el contador de reintentos, delega en el notificador y devuelve la factura actualizada.
/// </summary>
public sealed class InvoiceShipmentServiceResendTests
{
    /// <summary>Notificador que registra el resultado configurado en la factura (mutación in-memory).</summary>
    private sealed class StubNotifier : IInvoiceTransitionNotifier
    {
        private readonly NotificationOutcome _outcome;
        public int Calls { get; private set; }

        public StubNotifier(NotificationOutcome outcome) => _outcome = outcome;

        public Task NotifyTransitionAsync(Invoice invoice, InvoiceStatus previousStatus, CancellationToken cancellationToken = default)
        {
            Calls++;
            invoice.RecordNotificationResult(
                NotificationType.Reminder, _outcome, DateTime.UtcNow,
                _outcome == NotificationOutcome.Failed ? "boom" : null);
            return Task.CompletedTask;
        }
    }

    private static Invoice Failed(string clientId)
    {
        var invoice = new Invoice(clientId, 100m);
        invoice.UpdateStatus(InvoiceStatus.PrimerRecordatorio);
        invoice.RecordNotificationResult(NotificationType.Reminder, NotificationOutcome.Failed, DateTime.UtcNow, "previo");
        return invoice;
    }

    [Fact]
    public async Task ResendAsync_IncrementsRetry_AndReturnsSent()
    {
        var repo = new InMemoryInvoiceRepository();
        var invoice = Failed("cli-1");
        await repo.AddAsync(invoice);
        var notifier = new StubNotifier(NotificationOutcome.Sent);
        var service = new InvoiceShipmentService(repo, notifier, NullLogger<InvoiceShipmentService>.Instance);

        var result = await service.ResendAsync(invoice.Id);

        result.ShouldNotBeNull();
        result!.LastNotificationOutcome.ShouldBe(NotificationOutcome.Sent);
        result.NotificationRetryCount.ShouldBe(1);
        notifier.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task ResendAsync_FailedAgain_StillIncrementsRetry()
    {
        var repo = new InMemoryInvoiceRepository();
        var invoice = Failed("cli-1");
        await repo.AddAsync(invoice);
        var service = new InvoiceShipmentService(repo, new StubNotifier(NotificationOutcome.Failed), NullLogger<InvoiceShipmentService>.Instance);

        var result = await service.ResendAsync(invoice.Id);

        result!.LastNotificationOutcome.ShouldBe(NotificationOutcome.Failed);
        result.NotificationRetryCount.ShouldBe(1);
    }

    [Fact]
    public async Task ResendAsync_UnknownId_ReturnsNull()
    {
        var repo = new InMemoryInvoiceRepository();
        var service = new InvoiceShipmentService(repo, new StubNotifier(NotificationOutcome.Sent), NullLogger<InvoiceShipmentService>.Instance);

        (await service.ResendAsync("missing")).ShouldBeNull();
    }
}
