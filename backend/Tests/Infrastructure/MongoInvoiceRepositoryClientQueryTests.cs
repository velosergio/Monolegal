using Backend.Tests.Infrastructure.Support;
using Monolegal.Domain.Enums;
using Shouldly;
using Xunit;

namespace Backend.Tests.Infrastructure;

/// <summary>
/// US2 (spec 007, T007) — Tests de integración de <c>GetByClientIdAsync</c> contra MongoDB real.
/// Cubre FR-002, FR-008, SC-002.
/// </summary>
[Trait("Category", "Integration")]
public sealed class MongoInvoiceRepositoryClientQueryTests : IClassFixture<MongoIntegrationFixture>
{
    private readonly MongoIntegrationFixture _fixture;

    public MongoInvoiceRepositoryClientQueryTests(MongoIntegrationFixture fixture)
        => _fixture = fixture;

    [Fact]
    public async Task GetByClientIdAsync_ReturnsOnlyInvoicesOfThatClient()
    {
        var repo = await _fixture.CreateCleanRepositoryAsync();
        await repo.AddAsync(InvoiceTestFactory.Create("C-123", status: InvoiceStatus.Pending));
        await repo.AddAsync(InvoiceTestFactory.Create("C-123", status: InvoiceStatus.PrimerRecordatorio));
        await repo.AddAsync(InvoiceTestFactory.Create("C-999", status: InvoiceStatus.Pending));

        var result = (await repo.GetByClientIdAsync("C-123")).ToList();

        result.Count.ShouldBe(2);
        result.ShouldAllBe(i => i.ClientId == "C-123");
    }

    [Fact]
    public async Task GetByClientIdAsync_ReturnsEmpty_WhenClientHasNoInvoices()
    {
        var repo = await _fixture.CreateCleanRepositoryAsync();
        await repo.AddAsync(InvoiceTestFactory.Create("C-123", status: InvoiceStatus.Pending));

        var result = await repo.GetByClientIdAsync("C-sin-facturas");

        result.ShouldBeEmpty();
    }
}
