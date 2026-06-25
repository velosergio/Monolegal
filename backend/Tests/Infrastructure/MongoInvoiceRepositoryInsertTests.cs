using Backend.Tests.Infrastructure.Support;
using Monolegal.Domain.Enums;
using Shouldly;
using Xunit;

namespace Backend.Tests.Infrastructure;

/// <summary>
/// US4 (spec 007, T011) — Tests de integración de <c>AddAsync</c> (inserción) contra MongoDB real.
/// Cubre FR-004, SC-007.
/// </summary>
[Trait("Category", "Integration")]
public sealed class MongoInvoiceRepositoryInsertTests : IClassFixture<MongoIntegrationFixture>
{
    private readonly MongoIntegrationFixture _fixture;

    public MongoInvoiceRepositoryInsertTests(MongoIntegrationFixture fixture)
        => _fixture = fixture;

    [Fact]
    public async Task AddAsync_PersistsInvoice_RetrievableById()
    {
        var repo = await _fixture.CreateCleanRepositoryAsync();
        var invoice = InvoiceTestFactory.Create("C-555", amount: 320m, status: InvoiceStatus.Pending);

        await repo.AddAsync(invoice);

        var fetched = await repo.GetByIdAsync(invoice.Id);
        fetched.ShouldNotBeNull();
        fetched!.Id.ShouldBe(invoice.Id);
        fetched.ClientId.ShouldBe("C-555");
        fetched.Amount.ShouldBe(320m);
        fetched.Status.ShouldBe(InvoiceStatus.Pending);
    }

    [Fact]
    public async Task AddAsync_PersistedInvoice_AppearsInStatusAndClientQueries()
    {
        var repo = await _fixture.CreateCleanRepositoryAsync();
        var invoice = InvoiceTestFactory.Create("C-777", status: InvoiceStatus.PrimerRecordatorio);

        await repo.AddAsync(invoice);

        var byStatus = (await repo.GetByStatusAsync(InvoiceStatus.PrimerRecordatorio)).ToList();
        var byClient = (await repo.GetByClientIdAsync("C-777")).ToList();

        byStatus.ShouldContain(i => i.Id == invoice.Id);
        byClient.ShouldContain(i => i.Id == invoice.Id);
    }
}
