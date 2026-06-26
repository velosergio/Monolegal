using System;
using System.Threading.Tasks;
using Monolegal.Domain.Entities;
using Backend.Tests.Infrastructure.Support;
using Shouldly;
using Xunit;

namespace Backend.Tests.Infrastructure;

/// <summary>
/// Tests de contrato de los métodos nuevos de <c>MongoInvoiceRepository</c> (spec 018, T004):
/// <c>DeleteAsync</c> (hard delete) y <c>CountByClientIdAsync</c> (guard de borrado de cliente),
/// contra MongoDB real.
/// </summary>
[Trait("Category", "Integration")]
public sealed class InvoiceRepositoryDeleteTests : IClassFixture<MongoIntegrationFixture>
{
    private readonly MongoIntegrationFixture _fixture;

    public InvoiceRepositoryDeleteTests(MongoIntegrationFixture fixture) => _fixture = fixture;

    private static Invoice NewInvoice(string clientId) =>
        Invoice.Create(clientId, new[] { new InvoiceItem("X", 1m, 100m) }, DateTime.UtcNow.AddDays(30));

    [Fact]
    public async Task DeleteAsync_RemovesInvoice_AndReturnsTrue()
    {
        var repo = await _fixture.CreateCleanRepositoryAsync();
        var invoice = NewInvoice("c-1");
        await repo.AddAsync(invoice);

        (await repo.DeleteAsync(invoice.Id)).ShouldBeTrue();
        (await repo.GetByIdAsync(invoice.Id)).ShouldBeNull();
    }

    [Fact]
    public async Task DeleteAsync_Nonexistent_ReturnsFalse()
    {
        var repo = await _fixture.CreateCleanRepositoryAsync();
        (await repo.DeleteAsync("no-existe")).ShouldBeFalse();
    }

    [Fact]
    public async Task CountByClientIdAsync_CountsOnlyMatchingClient()
    {
        var repo = await _fixture.CreateCleanRepositoryAsync();
        await repo.AddAsync(NewInvoice("c-1"));
        await repo.AddAsync(NewInvoice("c-1"));
        await repo.AddAsync(NewInvoice("c-2"));

        (await repo.CountByClientIdAsync("c-1")).ShouldBe(2);
        (await repo.CountByClientIdAsync("c-2")).ShouldBe(1);
        (await repo.CountByClientIdAsync("c-3")).ShouldBe(0);
    }
}
