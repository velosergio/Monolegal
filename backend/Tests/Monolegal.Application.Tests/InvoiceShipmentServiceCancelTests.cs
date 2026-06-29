using System;
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
/// Pruebas de aplicación de <see cref="InvoiceShipmentService.CancelAsync"/> (spec 019, US4):
/// pendiente notificable → omitido; rechazo si no pendiente o estado no notificable; no existe.
/// </summary>
public sealed class InvoiceShipmentServiceCancelTests
{
    private static InvoiceShipmentService NewService(InMemoryInvoiceRepository repo)
        => new(repo, new FakeTransitionNotifier(), NullLogger<InvoiceShipmentService>.Instance);

    [Fact]
    public async Task CancelAsync_PendingNotifiable_MarksSkipped()
    {
        var repo = new InMemoryInvoiceRepository();
        var invoice = new Invoice("cli-1", 100m);
        invoice.UpdateStatus(InvoiceStatus.PrimerRecordatorio); // None + notificable
        await repo.AddAsync(invoice);

        var result = await NewService(repo).CancelAsync(invoice.Id);

        result.Status.ShouldBe(CancelNotificationStatus.Cancelled);
        result.Invoice!.LastNotificationOutcome.ShouldBe(NotificationOutcome.Skipped);
    }

    [Fact]
    public async Task CancelAsync_AlreadySent_ReturnsNotPending()
    {
        var repo = new InMemoryInvoiceRepository();
        var invoice = new Invoice("cli-1", 100m);
        invoice.UpdateStatus(InvoiceStatus.Pagado);
        invoice.RecordNotificationResult(NotificationType.PaymentConfirmation, NotificationOutcome.Sent, DateTime.UtcNow);
        await repo.AddAsync(invoice);

        var result = await NewService(repo).CancelAsync(invoice.Id);

        result.Status.ShouldBe(CancelNotificationStatus.NotPending);
    }

    [Fact]
    public async Task CancelAsync_NonNotifiableStatus_ReturnsNotPending()
    {
        var repo = new InMemoryInvoiceRepository();
        var invoice = new Invoice("cli-1", 100m); // Pending: no notificable, None
        await repo.AddAsync(invoice);

        var result = await NewService(repo).CancelAsync(invoice.Id);

        result.Status.ShouldBe(CancelNotificationStatus.NotPending);
    }

    [Fact]
    public async Task CancelAsync_UnknownId_ReturnsNotFound()
    {
        var repo = new InMemoryInvoiceRepository();

        var result = await NewService(repo).CancelAsync("missing");

        result.Status.ShouldBe(CancelNotificationStatus.NotFound);
    }
}
