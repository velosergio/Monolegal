using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Backend.Infrastructure.Hosting;

/// <summary>
/// Migración única e idempotente (spec 015, FR-030/FR-031) que opera a nivel de documento BSON
/// sobre la colección <c>Invoices</c>:
/// <list type="number">
///   <item>Remapea los estados legacy a un estado activo válido (Borrador→Pending, Vencida→Pending,
///   Cancelada→Desactivado) antes de que el sistema dependa de que el enum solo tenga estados activos.</item>
///   <item>Siembra un evento de creación en cada factura sin historial, para que ninguna quede sin él.</item>
/// </list>
/// Trabaja con valores enteros crudos (igual que la serialización de <c>InvoiceStatus</c> en Mongo)
/// para no depender de los miembros del enum, que ya no incluyen los valores legacy. Reejecutarla no
/// duplica eventos ni revierte estados.
/// </summary>
public sealed class StatusHistoryBackfillMigration : IHostedService
{
    // Valores enteros legacy y activos, alineados con la serialización por defecto del enum en Mongo.
    private const int LegacyDraft = 0;
    private const int LegacyOverdue = 3;
    private const int LegacyCancelled = 4;
    private const int ActivePending = 1;
    private const int ActiveDesactivado = 12;
    private const int SourceManual = 1; // StatusChangeSource.Manual

    private readonly IMongoDatabase _database;
    private readonly ILogger<StatusHistoryBackfillMigration> _logger;

    public StatusHistoryBackfillMigration(
        IMongoDatabase database,
        ILogger<StatusHistoryBackfillMigration> logger)
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
                "StatusHistoryBackfillMigration completada. EstadosRemapeados={Remapped} HistorialSembrado={Backfilled}",
                result.RemappedStatuses, result.BackfilledHistories);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fail-soft: un fallo se loguea y no detiene el arranque (mismo criterio que el seeder).
            _logger.LogError(ex, "Falló la migración de historial/estados legacy. Se continúa el arranque.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Ejecuta la migración y devuelve un resumen de cambios. Idempotente.
    /// </summary>
    public async Task<MigrationResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var collection = _database.GetCollection<BsonDocument>("Invoices");
        var filterBuilder = Builders<BsonDocument>.Filter;

        // 1. Remapeo de estados legacy.
        long remapped = 0;
        var toPending = await collection.UpdateManyAsync(
            filterBuilder.In("Status", new[] { LegacyDraft, LegacyOverdue }),
            Builders<BsonDocument>.Update.Set("Status", ActivePending),
            cancellationToken: cancellationToken).ConfigureAwait(false);
        remapped += toPending.ModifiedCount;

        var toDesactivado = await collection.UpdateManyAsync(
            filterBuilder.Eq("Status", LegacyCancelled),
            Builders<BsonDocument>.Update.Set("Status", ActiveDesactivado),
            cancellationToken: cancellationToken).ConfigureAwait(false);
        remapped += toDesactivado.ModifiedCount;

        // 2. Backfill del evento de creación en facturas sin historial.
        var missingHistory = filterBuilder.Or(
            filterBuilder.Exists("StatusHistory", false),
            filterBuilder.Eq("StatusHistory", BsonNull.Value),
            filterBuilder.Size("StatusHistory", 0));

        var pending = await collection
            .Find(missingHistory)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        long backfilled = 0;
        foreach (var doc in pending)
        {
            var status = doc.GetValue("Status", ActivePending);
            var createdAt = doc.GetValue("CreatedAt", BsonDateTime.Create(DateTime.UtcNow));

            // Evento de creación: from == to == estado inicial conocido, en el momento de creación.
            var creationEvent = new BsonDocument
            {
                { "From", status },
                { "To", status },
                { "At", createdAt },
                { "Source", SourceManual },
            };

            await collection.UpdateOneAsync(
                filterBuilder.Eq("_id", doc["_id"]),
                Builders<BsonDocument>.Update.Set("StatusHistory", new BsonArray { creationEvent }),
                cancellationToken: cancellationToken).ConfigureAwait(false);
            backfilled++;
        }

        return new MigrationResult(remapped, backfilled);
    }

    /// <summary>Resumen del resultado de la migración.</summary>
    public sealed record MigrationResult(long RemappedStatuses, long BackfilledHistories);
}
