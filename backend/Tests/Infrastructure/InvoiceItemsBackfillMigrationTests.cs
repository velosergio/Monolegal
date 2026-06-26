using System;
using System.Threading.Tasks;
using Backend.Infrastructure.Hosting;
using Backend.Tests.Infrastructure.Support;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;
using Shouldly;
using Xunit;

namespace Backend.Tests.Infrastructure;

/// <summary>
/// spec 018 (T005) — Migración idempotente de items/vencimiento contra MongoDB real: las facturas
/// previas sin <c>Items</c> reciben una línea sintética que preserva el monto, y las que carecen de
/// <c>DueDate</c> reciben <c>CreatedAt + 30 días</c>. Reejecutarla no modifica documentos ya migrados.
/// </summary>
[Trait("Category", "Integration")]
public sealed class InvoiceItemsBackfillMigrationTests : IClassFixture<MongoIntegrationFixture>
{
    private readonly MongoIntegrationFixture _fixture;

    public InvoiceItemsBackfillMigrationTests(MongoIntegrationFixture fixture) => _fixture = fixture;

    private static BsonDocument LegacyInvoice(string id) => new()
    {
        { "_id", id },
        { "ClientId", "c-" + id },
        { "Amount", 250.0m },
        { "Status", 1 },
        { "CreatedAt", BsonDateTime.Create(DateTime.UtcNow.AddDays(-40)) },
        { "UpdatedAt", BsonDateTime.Create(DateTime.UtcNow.AddDays(-40)) },
        { "LastStatusTransitionAt", BsonDateTime.Create(DateTime.UtcNow.AddDays(-40)) },
        { "RemindersCount", 0 },
        // Sin Items ni DueDate (modelo previo a spec 018).
    };

    private async Task<IMongoCollection<BsonDocument>> CleanCollectionAsync()
    {
        await _fixture.Database.DropCollectionAsync("Invoices");
        var collection = _fixture.Database.GetCollection<BsonDocument>("Invoices");
        return collection;
    }

    [Fact]
    public async Task Run_BackfillsItemsAndDueDate_PreservingAmount()
    {
        var collection = await CleanCollectionAsync();
        await collection.InsertOneAsync(LegacyInvoice("inv-1"));
        var migration = new InvoiceItemsBackfillMigration(
            _fixture.Database, NullLogger<InvoiceItemsBackfillMigration>.Instance);

        var result = await migration.RunAsync();

        result.ItemsBackfilled.ShouldBe(1);
        result.DueDatesBackfilled.ShouldBe(1);

        var doc = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", "inv-1")).FirstAsync();
        var items = doc["Items"].AsBsonArray;
        items.Count.ShouldBe(1);
        items[0]["UnitPrice"].ToDecimal().ShouldBe(250.0m);
        items[0]["Quantity"].ToDecimal().ShouldBe(1m);
        doc.Contains("DueDate").ShouldBeTrue();
    }

    [Fact]
    public async Task Run_IsIdempotent_OnRepeatedRuns()
    {
        var collection = await CleanCollectionAsync();
        await collection.InsertOneAsync(LegacyInvoice("inv-2"));
        var migration = new InvoiceItemsBackfillMigration(
            _fixture.Database, NullLogger<InvoiceItemsBackfillMigration>.Instance);

        await migration.RunAsync();
        var second = await migration.RunAsync();

        // La segunda ejecución no debe tocar documentos ya migrados.
        second.ItemsBackfilled.ShouldBe(0);
        second.DueDatesBackfilled.ShouldBe(0);
    }

    [Fact]
    public async Task Run_DoesNotTouchInvoicesThatAlreadyHaveItems()
    {
        var collection = await CleanCollectionAsync();
        var doc = LegacyInvoice("inv-3");
        doc["Items"] = new BsonArray
        {
            new BsonDocument { { "Description", "Existente" }, { "Quantity", 2m }, { "UnitPrice", 125m } },
        };
        doc["DueDate"] = BsonDateTime.Create(DateTime.UtcNow.AddDays(5));
        await collection.InsertOneAsync(doc);
        var migration = new InvoiceItemsBackfillMigration(
            _fixture.Database, NullLogger<InvoiceItemsBackfillMigration>.Instance);

        var result = await migration.RunAsync();

        result.ItemsBackfilled.ShouldBe(0);
        result.DueDatesBackfilled.ShouldBe(0);
    }
}
