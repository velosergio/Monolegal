using System;
using System.Threading;
using System.Threading.Tasks;
using Backend.Application.Abstractions;
using Backend.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Monolegal.Domain.Repositories;

namespace Backend.Infrastructure.Maintenance;

/// <summary>
/// Implementación de las operaciones destructivas de la "zona de peligro" (<c>/configuracion</c>).
/// "Eliminar todos los datos" borra los registros de negocio conservando la base; "Flush DB"
/// vacía toda la base, reconstruye índices y vuelve a ejecutar el sembrador. Registra cada
/// operación con logging estructurado y nunca expone secretos.
/// </summary>
public sealed class MaintenanceService : IMaintenanceService
{
    private readonly IInvoiceRepository _invoices;
    private readonly IMongoDatabase _database;
    private readonly MongoIndexBuilder _indexBuilder;
    private readonly IDevDataSeeder _seeder;
    private readonly ILogger<MaintenanceService> _logger;

    public MaintenanceService(
        IInvoiceRepository invoices,
        IMongoDatabase database,
        MongoIndexBuilder indexBuilder,
        IDevDataSeeder seeder,
        ILogger<MaintenanceService> logger)
    {
        _invoices = invoices ?? throw new ArgumentNullException(nameof(invoices));
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _indexBuilder = indexBuilder ?? throw new ArgumentNullException(nameof(indexBuilder));
        _seeder = seeder ?? throw new ArgumentNullException(nameof(seeder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DeleteAllDataResult> DeleteAllDataAsync(CancellationToken cancellationToken = default)
    {
        var deleted = await _invoices.DeleteAllAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogWarning(
            "Zona de peligro: eliminación de todos los datos. FacturasEliminadas={Deleted}",
            deleted);

        return new DeleteAllDataResult(deleted);
    }

    public async Task<FlushDatabaseResult> FlushDatabaseAsync(CancellationToken cancellationToken = default)
    {
        var existing = await _invoices.CountAsync(cancellationToken).ConfigureAwait(false);

        var databaseName = _database.DatabaseNamespace.DatabaseName;
        await _database.Client
            .DropDatabaseAsync(databaseName, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogWarning(
            "Zona de peligro: base de datos vaciada (flush). Base={Database}", databaseName);

        // Reconstruye los índices que normalmente se crean al arranque (se pierden al dropear).
        await _indexBuilder.EnsureIndexesAsync(cancellationToken).ConfigureAwait(false);

        // Vuelve a sembrar: tras el flush la base está vacía, así que el sembrador siempre actúa.
        var seed = await _seeder.SeedAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogWarning(
            "Zona de peligro: re-sembrado tras flush. Sembrado={Seeded} Clientes={Clients} Facturas={Invoices}",
            seed.Seeded, seed.ClientsCreated, seed.InvoicesCreated);

        return new FlushDatabaseResult(existing, seed.Seeded, seed.ClientsCreated, seed.InvoicesCreated);
    }
}
