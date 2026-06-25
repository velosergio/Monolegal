using Backend.Tests.Infrastructure.Support;
using Monolegal.Domain.Enums;
using Shouldly;
using Xunit;

namespace Backend.Tests.Infrastructure;

/// <summary>
/// Tests de integración de <c>CountAsync</c> (spec 008, T004) contra MongoDB real.
/// Verifica la precondición de "base vacía" y el conteo tras inserciones (Foundational).
/// </summary>
[Trait("Category", "Integration")]
public sealed class MongoInvoiceRepositoryCountTests : IClassFixture<MongoIntegrationFixture>
{
    private readonly MongoIntegrationFixture _fixture;

    public MongoInvoiceRepositoryCountTests(MongoIntegrationFixture fixture)
        => _fixture = fixture;

    [Fact]
    public async Task CountAsync_OnEmptyCollection_ReturnsZero()
    {
        var repo = await _fixture.CreateCleanRepositoryAsync();

        var count = await repo.CountAsync();

        count.ShouldBe(0);
    }

    [Fact]
    public async Task CountAsync_AfterInserts_ReturnsExactCount()
    {
        var repo = await _fixture.CreateCleanRepositoryAsync();
        await repo.AddAsync(InvoiceTestFactory.Create("c1", status: InvoiceStatus.Pending));
        await repo.AddAsync(InvoiceTestFactory.Create("c2", status: InvoiceStatus.PrimerRecordatorio));
        await repo.AddAsync(InvoiceTestFactory.Create("c3", status: InvoiceStatus.SegundoRecordatorio));

        var count = await repo.CountAsync();

        count.ShouldBe(3);
    }
}
