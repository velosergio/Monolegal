using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Tests.Infrastructure.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Shouldly;
using Xunit;

namespace Backend.Tests.Infrastructure;

/// <summary>
/// spec 018 (T062) — Verifica que <c>MongoIndexBuilder.EnsureIndexesAsync</c> crea los índices de la
/// colección <c>Clients</c>: índice único <c>Email_unique</c> e índice <c>Name_asc</c> para el orden
/// del listado. Idempotente. Soporta la paginación forzada y la unicidad de email (Constitución
/// Performance + RF-015a).
/// </summary>
[Trait("Category", "Integration")]
public sealed class ClientIndexTests : IClassFixture<MongoIntegrationFixture>
{
    private readonly MongoIntegrationFixture _fixture;

    public ClientIndexTests(MongoIntegrationFixture fixture) => _fixture = fixture;

    private async Task<List<BsonDocument>> ListClientIndexesAsync()
    {
        var collection = _fixture.Database.GetCollection<BsonDocument>("Clients");
        using var cursor = await collection.Indexes.ListAsync();
        return await cursor.ToListAsync();
    }

    [Fact]
    public async Task EnsureIndexesAsync_CreatesEmailUniqueAndNameIndexes()
    {
        await _fixture.Database.DropCollectionAsync("Clients");

        await _fixture.CreateIndexBuilder().EnsureIndexesAsync();

        var indexes = await ListClientIndexesAsync();
        var names = indexes.Select(ix => ix["name"].AsString).ToList();
        names.ShouldContain("Email_unique");
        names.ShouldContain("Name_asc");

        // El índice de email debe ser único.
        var emailIndex = indexes.First(ix => ix["name"].AsString == "Email_unique");
        emailIndex.GetValue("unique", false).ToBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task EnsureIndexesAsync_IsIdempotent_OnRepeatedRuns()
    {
        await _fixture.Database.DropCollectionAsync("Clients");

        await _fixture.CreateIndexBuilder().EnsureIndexesAsync();
        await _fixture.CreateIndexBuilder().EnsureIndexesAsync();

        var names = (await ListClientIndexesAsync()).Select(ix => ix["name"].AsString).ToList();
        names.Count(n => n == "Email_unique").ShouldBe(1);
        names.Count(n => n == "Name_asc").ShouldBe(1);
    }
}
