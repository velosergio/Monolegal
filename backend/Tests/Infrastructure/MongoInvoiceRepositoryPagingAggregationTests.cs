using System.Linq;
using Backend.Tests.Infrastructure.Support;
using Monolegal.Domain.Enums;
using Shouldly;
using Xunit;

namespace Backend.Tests.Infrastructure;

/// <summary>
/// Tests de integración de los métodos añadidos en la spec 009 contra MongoDB real:
/// <c>GetPagedAsync</c> (filtro, total, orden CreatedAt desc, paginación) y las agregaciones
/// <c>CountByStatusAsync</c> / <c>CountByClientAsync</c>.
/// </summary>
[Trait("Category", "Integration")]
public sealed class MongoInvoiceRepositoryPagingAggregationTests : IClassFixture<MongoIntegrationFixture>
{
    private readonly MongoIntegrationFixture _fixture;

    public MongoInvoiceRepositoryPagingAggregationTests(MongoIntegrationFixture fixture)
        => _fixture = fixture;

    [Fact]
    public async Task GetPagedAsync_WithStatusFilter_ReturnsOnlyMatchingWithTotal()
    {
        var repo = await _fixture.CreateCleanRepositoryAsync();
        await repo.AddAsync(InvoiceTestFactory.Create("c1", status: InvoiceStatus.Pending));
        await repo.AddAsync(InvoiceTestFactory.Create("c2", status: InvoiceStatus.PrimerRecordatorio));
        await repo.AddAsync(InvoiceTestFactory.Create("c3", status: InvoiceStatus.PrimerRecordatorio));

        var (items, total) = await repo.GetPagedAsync(InvoiceStatus.PrimerRecordatorio, 1, 10);

        total.ShouldBe(2);
        items.ShouldAllBe(i => i.Status == InvoiceStatus.PrimerRecordatorio);
    }

    [Fact]
    public async Task GetPagedAsync_PaginatesAndTotalReflectsAllMatches()
    {
        var repo = await _fixture.CreateCleanRepositoryAsync();
        for (var i = 0; i < 15; i++)
            await repo.AddAsync(InvoiceTestFactory.Create($"c{i}", status: InvoiceStatus.Pending));

        var (items, total) = await repo.GetPagedAsync(null, page: 1, pageSize: 10);

        items.Count.ShouldBe(10);
        total.ShouldBe(15);
    }

    [Fact]
    public async Task GetPagedAsync_OrdersByCreatedAtDescending()
    {
        var repo = await _fixture.CreateCleanRepositoryAsync();
        // Insertadas en orden; CreatedAt = UtcNow en el constructor (incremental).
        var first = InvoiceTestFactory.Create("c1", status: InvoiceStatus.Pending);
        await repo.AddAsync(first);
        await Task.Delay(10);
        var last = InvoiceTestFactory.Create("c2", status: InvoiceStatus.Pending);
        await repo.AddAsync(last);

        var (items, _) = await repo.GetPagedAsync(null, 1, 10);

        items.First().Id.ShouldBe(last.Id);
    }

    [Fact]
    public async Task CountByStatusAsync_GroupsCorrectly()
    {
        var repo = await _fixture.CreateCleanRepositoryAsync();
        await repo.AddAsync(InvoiceTestFactory.Create("c1", status: InvoiceStatus.Pending));
        await repo.AddAsync(InvoiceTestFactory.Create("c2", status: InvoiceStatus.Pagado));
        await repo.AddAsync(InvoiceTestFactory.Create("c3", status: InvoiceStatus.Pagado));

        var byStatus = await repo.CountByStatusAsync();

        byStatus[InvoiceStatus.Pagado].ShouldBe(2);
        byStatus[InvoiceStatus.Pending].ShouldBe(1);
        byStatus.Values.Sum().ShouldBe(3);
    }

    [Fact]
    public async Task CountByClientAsync_GroupsCorrectly()
    {
        var repo = await _fixture.CreateCleanRepositoryAsync();
        await repo.AddAsync(InvoiceTestFactory.Create("A", status: InvoiceStatus.Pending));
        await repo.AddAsync(InvoiceTestFactory.Create("A", status: InvoiceStatus.Pagado));
        await repo.AddAsync(InvoiceTestFactory.Create("B", status: InvoiceStatus.Pending));

        var byClient = await repo.CountByClientAsync();

        byClient["A"].ShouldBe(2);
        byClient["B"].ShouldBe(1);
    }
}
