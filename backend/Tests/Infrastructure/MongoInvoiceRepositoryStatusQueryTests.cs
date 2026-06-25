using Backend.Tests.Infrastructure.Support;
using Monolegal.Domain.Enums;
using Shouldly;
using Xunit;

namespace Backend.Tests.Infrastructure;

/// <summary>
/// US1 (spec 007, T005) — Tests de integración de <c>GetByStatusAsync</c> contra MongoDB real.
/// Cubre FR-001, FR-008, SC-001.
/// </summary>
[Trait("Category", "Integration")]
public sealed class MongoInvoiceRepositoryStatusQueryTests : IClassFixture<MongoIntegrationFixture>
{
    private readonly MongoIntegrationFixture _fixture;

    public MongoInvoiceRepositoryStatusQueryTests(MongoIntegrationFixture fixture)
        => _fixture = fixture;

    [Fact]
    public async Task GetByStatusAsync_ReturnsOnlyMatchingStatus()
    {
        var repo = await _fixture.CreateCleanRepositoryAsync();
        await repo.AddAsync(InvoiceTestFactory.Create("c1", status: InvoiceStatus.Pending));
        await repo.AddAsync(InvoiceTestFactory.Create("c2", status: InvoiceStatus.PrimerRecordatorio));
        await repo.AddAsync(InvoiceTestFactory.Create("c3", status: InvoiceStatus.Pagado));

        var result = (await repo.GetByStatusAsync(InvoiceStatus.Pending)).ToList();

        result.Count.ShouldBe(1);
        result[0].ClientId.ShouldBe("c1");
        result.ShouldAllBe(i => i.Status == InvoiceStatus.Pending);
    }

    [Fact]
    public async Task GetByStatusAsync_ReturnsEmpty_WhenNoMatch()
    {
        var repo = await _fixture.CreateCleanRepositoryAsync();
        await repo.AddAsync(InvoiceTestFactory.Create("c1", status: InvoiceStatus.Pending));

        var result = await repo.GetByStatusAsync(InvoiceStatus.Pagado);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetByStatusAsync_ReturnsMultiple_WhenSeveralMatch()
    {
        var repo = await _fixture.CreateCleanRepositoryAsync();
        await repo.AddAsync(InvoiceTestFactory.Create("c1", status: InvoiceStatus.PrimerRecordatorio));
        await repo.AddAsync(InvoiceTestFactory.Create("c2", status: InvoiceStatus.PrimerRecordatorio));
        await repo.AddAsync(InvoiceTestFactory.Create("c3", status: InvoiceStatus.Pending));

        var result = (await repo.GetByStatusAsync(InvoiceStatus.PrimerRecordatorio)).ToList();

        result.Count.ShouldBe(2);
        result.ShouldAllBe(i => i.Status == InvoiceStatus.PrimerRecordatorio);
    }
}
