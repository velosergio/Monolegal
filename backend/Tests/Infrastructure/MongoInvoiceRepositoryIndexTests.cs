using Backend.Tests.Infrastructure.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Shouldly;
using Xunit;

namespace Backend.Tests.Infrastructure;

/// <summary>
/// US5 (spec 007, T013) — Tests de integración de los índices creados por
/// <c>MongoIndexBuilder.EnsureIndexesAsync</c> contra MongoDB real.
/// Cubre FR-005, FR-006, FR-010, SC-006.
/// </summary>
[Trait("Category", "Integration")]
public sealed class MongoInvoiceRepositoryIndexTests : IClassFixture<MongoIntegrationFixture>
{
    private readonly MongoIntegrationFixture _fixture;

    public MongoInvoiceRepositoryIndexTests(MongoIntegrationFixture fixture)
        => _fixture = fixture;

    [Fact]
    public async Task EnsureIndexesAsync_CreatesStatusAndClientIdIndexes()
    {
        await _fixture.CreateCleanRepositoryAsync(); // colección limpia
        var builder = _fixture.CreateIndexBuilder();

        await builder.EnsureIndexesAsync();

        var names = await ListIndexNamesAsync();
        names.ShouldContain("Status_asc");
        names.ShouldContain("ClientId_asc");
    }

    [Fact]
    public async Task EnsureIndexesAsync_IsIdempotent_OnRepeatedRuns()
    {
        await _fixture.CreateCleanRepositoryAsync();
        var builder = _fixture.CreateIndexBuilder();

        await builder.EnsureIndexesAsync();
        // Una segunda ejecución no debe lanzar ni duplicar índices.
        await builder.EnsureIndexesAsync();

        var names = await ListIndexNamesAsync();
        names.Count(n => n == "Status_asc").ShouldBe(1);
        names.Count(n => n == "ClientId_asc").ShouldBe(1);
    }

    private async Task<List<string>> ListIndexNamesAsync()
    {
        using var cursor = await _fixture.InvoicesRaw.Indexes.ListAsync();
        var indexes = await cursor.ToListAsync();
        return indexes.Select(ix => ix["name"].AsString).ToList();
    }
}
