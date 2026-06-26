using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Backend.Infrastructure.Hosting;

/// <summary>
/// Migración única e idempotente (spec 018, research D6) que opera a nivel de documento BSON sobre
/// la colección <c>Invoices</c> para llevar las facturas previas al nuevo modelo:
/// <list type="number">
///   <item>Las facturas sin <c>Items</c> reciben una línea de detalle sintética
///   <c>{ Description: "Concepto", Quantity: 1, UnitPrice: Amount }</c>, preservando el monto.</item>
///   <item>Las facturas sin <c>DueDate</c> reciben <c>CreatedAt + 30 días</c>.</item>
/// </list>
/// Reejecutarla no modifica documentos ya migrados (solo toca los que carecen de los campos).
/// </summary>
public sealed class InvoiceItemsBackfillMigration : IHostedService
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<InvoiceItemsBackfillMigration> _logger;

    public InvoiceItemsBackfillMigration(
        IMongoDatabase database,
        ILogger<InvoiceItemsBackfillMigration> logger)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await RunAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation(
                "InvoiceItemsBackfillMigration completada. ItemsSembrados={Items} VencimientosSembrados={DueDates}",
                result.ItemsBackfilled, result.DueDatesBackfilled);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Falló la migración de items/vencimiento de facturas. Se continúa el arranque.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Ejecuta la migración y devuelve un resumen de cambios. Idempotente.</summary>
    public async Task<MigrationResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var collection = _database.GetCollection<BsonDocument>("Invoices");
        var filterBuilder = Builders<BsonDocument>.Filter;

        var missingItems = filterBuilder.Or(
            filterBuilder.Exists("Items", false),
            filterBuilder.Eq("Items", BsonNull.Value),
            filterBuilder.Size("Items", 0));

        var missingDueDate = filterBuilder.Or(
            filterBuilder.Exists("DueDate", false),
            filterBuilder.Eq("DueDate", BsonNull.Value));

        var pending = await collection
            .Find(filterBuilder.Or(missingItems, missingDueDate))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        long itemsBackfilled = 0;
        long dueDatesBackfilled = 0;

        foreach (var doc in pending)
        {
            var updates = new System.Collections.Generic.List<UpdateDefinition<BsonDocument>>();

            var hasItems = doc.TryGetValue("Items", out var itemsValue)
                && itemsValue is BsonArray { Count: > 0 };
            if (!hasItems)
            {
                var amount = doc.GetValue("Amount", BsonDecimal128.Create(0m));
                var item = new BsonDocument
                {
                    { "Description", "Concepto" },
                    { "Quantity", BsonDecimal128.Create(1m) },
                    { "UnitPrice", amount },
                };
                updates.Add(Builders<BsonDocument>.Update.Set("Items", new BsonArray { item }));
                itemsBackfilled++;
            }

            var hasDueDate = doc.TryGetValue("DueDate", out var dueValue)
                && dueValue.BsonType != BsonType.Null;
            if (!hasDueDate)
            {
                var createdAt = doc.GetValue("CreatedAt", BsonDateTime.Create(DateTime.UtcNow)).ToUniversalTime();
                updates.Add(Builders<BsonDocument>.Update.Set("DueDate", BsonDateTime.Create(createdAt.AddDays(30))));
                dueDatesBackfilled++;
            }

            if (updates.Count > 0)
            {
                await collection.UpdateOneAsync(
                    filterBuilder.Eq("_id", doc["_id"]),
                    Builders<BsonDocument>.Update.Combine(updates),
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        return new MigrationResult(itemsBackfilled, dueDatesBackfilled);
    }

    /// <summary>Resumen del resultado de la migración.</summary>
    public sealed record MigrationResult(long ItemsBackfilled, long DueDatesBackfilled);
}
