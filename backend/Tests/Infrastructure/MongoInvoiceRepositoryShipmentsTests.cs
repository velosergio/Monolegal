using System;
using System.Linq;
using Backend.Tests.Infrastructure.Support;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;
using Shouldly;
using Xunit;

namespace Backend.Tests.Infrastructure;

/// <summary>
/// Tests de integración de <c>GetShipmentsPagedAsync</c> contra MongoDB real (spec 019, US1):
/// sólo estados notificables, filtro por sendStatus y por clientIds, orden por último intento,
/// paginación y total.
/// </summary>
[Trait("Category", "Integration")]
public sealed class MongoInvoiceRepositoryShipmentsTests : IClassFixture<MongoIntegrationFixture>
{
    private readonly MongoIntegrationFixture _fixture;

    public MongoInvoiceRepositoryShipmentsTests(MongoIntegrationFixture fixture)
        => _fixture = fixture;

    private static Invoice WithOutcome(string clientId, InvoiceStatus status, NotificationOutcome outcome)
    {
        var invoice = InvoiceTestFactory.Create(clientId, status: status);
        if (outcome != NotificationOutcome.None)
        {
            var type = status switch
            {
                InvoiceStatus.Pagado => NotificationType.PaymentConfirmation,
                InvoiceStatus.Desactivado => NotificationType.DeactivationNotice,
                _ => NotificationType.Reminder,
            };
            invoice.RecordNotificationResult(type, outcome, DateTime.UtcNow, outcome == NotificationOutcome.Failed ? "fallo" : null);
        }
        return invoice;
    }

    [Fact]
    public async Task GetShipmentsPagedAsync_ReturnsOnlyNotifiableStatuses()
    {
        var repo = await _fixture.CreateCleanRepositoryAsync();
        await repo.AddAsync(WithOutcome("c1", InvoiceStatus.PrimerRecordatorio, NotificationOutcome.Failed));
        await repo.AddAsync(WithOutcome("c2", InvoiceStatus.Pending, NotificationOutcome.None)); // excluida

        var (items, total) = await repo.GetShipmentsPagedAsync(null, null, 1, 10);

        total.ShouldBe(1);
        items.ShouldAllBe(i => i.Status != InvoiceStatus.Pending);
    }

    [Fact]
    public async Task GetShipmentsPagedAsync_FiltersBySendStatus()
    {
        var repo = await _fixture.CreateCleanRepositoryAsync();
        await repo.AddAsync(WithOutcome("c1", InvoiceStatus.PrimerRecordatorio, NotificationOutcome.Failed));
        await repo.AddAsync(WithOutcome("c2", InvoiceStatus.Pagado, NotificationOutcome.Sent));

        var (items, total) = await repo.GetShipmentsPagedAsync(NotificationOutcome.Failed, null, 1, 10);

        total.ShouldBe(1);
        items.ShouldAllBe(i => i.LastNotificationOutcome == NotificationOutcome.Failed);
    }

    [Fact]
    public async Task GetShipmentsPagedAsync_FiltersByClientIds()
    {
        var repo = await _fixture.CreateCleanRepositoryAsync();
        await repo.AddAsync(WithOutcome("c1", InvoiceStatus.Pagado, NotificationOutcome.Sent));
        await repo.AddAsync(WithOutcome("c2", InvoiceStatus.Pagado, NotificationOutcome.Sent));

        var (items, total) = await repo.GetShipmentsPagedAsync(null, new[] { "c2" }, 1, 10);

        total.ShouldBe(1);
        items.Single().ClientId.ShouldBe("c2");
    }

    [Fact]
    public async Task GetShipmentsPagedAsync_EmptyClientIds_ReturnsEmpty()
    {
        var repo = await _fixture.CreateCleanRepositoryAsync();
        await repo.AddAsync(WithOutcome("c1", InvoiceStatus.Pagado, NotificationOutcome.Sent));

        var (items, total) = await repo.GetShipmentsPagedAsync(null, Array.Empty<string>(), 1, 10);

        total.ShouldBe(0);
        items.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetShipmentsPagedAsync_PaginatesAndTotalReflectsAllMatches()
    {
        var repo = await _fixture.CreateCleanRepositoryAsync();
        for (var i = 0; i < 12; i++)
            await repo.AddAsync(WithOutcome($"c{i}", InvoiceStatus.Pagado, NotificationOutcome.Sent));

        var (items, total) = await repo.GetShipmentsPagedAsync(null, null, 1, 10);

        items.Count.ShouldBe(10);
        total.ShouldBe(12);
    }
}
