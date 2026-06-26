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
/// spec 015 (T008) — Migración idempotente contra MongoDB real: remapeo de estados legacy
/// (Borrador→Pending, Vencida→Pending, Cancelada→Desactivado) y backfill del evento de creación
/// en facturas sin historial. Reejecutarla no duplica eventos ni revierte estados.
/// </summary>
[Trait("Category", "Integration")]
public sealed class StatusHistoryBackfillMigrationTests : IClassFixture<MongoIntegrationFixture>
{
    private const int LegacyDraft = 0;
    private const int LegacyOverdue = 3;
    private const int LegacyCancelled = 4;
    private const int Pending = 1;
    private const int Pagado = 2;
    private const int Desactivado = 12;

    private readonly MongoIntegrationFixture _fixture;

    public StatusHistoryBackfillMigrationTests(MongoIntegrationFixture fixture)
        => _fixture = fixture;

    private static BsonDocument RawInvoice(string id, int status, bool withHistory = false)
    {
        var doc = new BsonDocument
        {
            { "_id", id },
            { "ClientId", "c-" + id },
            { "Amount", 100.0m },
            { "Status", status },
            { "CreatedAt", BsonDateTime.Create(DateTime.UtcNow.AddDays(-10)) },
            { "UpdatedAt", BsonDateTime.Create(DateTime.UtcNow.AddDays(-10)) },
            { "LastStatusTransitionAt", BsonDateTime.Create(DateTime.UtcNow.AddDays(-10)) },
            { "RemindersCount", 0 },
        };
        if (withHistory)
        {
            doc["StatusHistory"] = new BsonArray
            {
                new BsonDocument
                {
                    { "From", Pending },
                    { "To", status },
                    { "At", BsonDateTime.Create(DateTime.UtcNow.AddDays(-5)) },
                    { "Source", 1 },
                },
            };
        }
        return doc;
    }

    private async Task<IMongoCollection<BsonDocument>> SeedRawAsync(params BsonDocument[] docs)
    {
        await _fixture.Database.DropCollectionAsync("Invoices");
        var collection = _fixture.Database.GetCollection<BsonDocument>("Invoices");
        await collection.InsertManyAsync(docs);
        return collection;
    }

    private StatusHistoryBackfillMigration CreateMigration()
        => new(_fixture.Database, NullLogger<StatusHistoryBackfillMigration>.Instance);

    [Fact]
    public async Task RemapsLegacyStatuses_ToActiveStatuses()
    {
        var collection = await SeedRawAsync(
            RawInvoice("draft1", LegacyDraft),
            RawInvoice("overdue1", LegacyOverdue),
            RawInvoice("cancelled1", LegacyCancelled));

        await CreateMigration().RunAsync();

        (await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", "draft1")).FirstAsync())["Status"].AsInt32.ShouldBe(Pending);
        (await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", "overdue1")).FirstAsync())["Status"].AsInt32.ShouldBe(Pending);
        (await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", "cancelled1")).FirstAsync())["Status"].AsInt32.ShouldBe(Desactivado);
    }

    [Fact]
    public async Task BackfillsCreationEvent_ForInvoicesWithoutHistory()
    {
        var collection = await SeedRawAsync(RawInvoice("a", Pending));

        var result = await CreateMigration().RunAsync();

        result.BackfilledHistories.ShouldBe(1);
        var doc = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", "a")).FirstAsync();
        doc["StatusHistory"].AsBsonArray.Count.ShouldBe(1);
        doc["StatusHistory"][0]["To"].AsInt32.ShouldBe(Pending);
    }

    [Fact]
    public async Task DoesNotTouch_InvoicesThatAlreadyHaveHistory()
    {
        var collection = await SeedRawAsync(RawInvoice("withhist", Pagado, withHistory: true));

        var result = await CreateMigration().RunAsync();

        result.BackfilledHistories.ShouldBe(0);
        var doc = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", "withhist")).FirstAsync();
        doc["StatusHistory"].AsBsonArray.Count.ShouldBe(1);
    }

    [Fact]
    public async Task IsIdempotent_OnSecondRun()
    {
        await SeedRawAsync(
            RawInvoice("draft1", LegacyDraft),
            RawInvoice("a", Pending));

        await CreateMigration().RunAsync();
        var second = await CreateMigration().RunAsync();

        // Tras la primera corrida no quedan estados legacy ni facturas sin historial.
        second.RemappedStatuses.ShouldBe(0);
        second.BackfilledHistories.ShouldBe(0);
    }
}
