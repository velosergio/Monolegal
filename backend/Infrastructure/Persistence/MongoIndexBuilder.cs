using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Monolegal.Domain.Entities;

namespace Backend.Infrastructure.Persistence;

/// <summary>
/// Ensures all required MongoDB indexes exist for the <c>Invoices</c> collection.
/// Called once at application startup (from <see cref="Backend.Infrastructure.Configuration.MongoConnectionVerifier"/>)
/// so that the worker's queries and repository lookups always have covering indexes.
/// </summary>
/// <remarks>
/// Indexes created:
/// <list type="bullet">
///   <item><term>Status_asc</term><description>Ascending index on <c>Status</c> — used by <c>GetTransitionableAsync()</c> worker filter.</description></item>
///   <item><term>ClientId_asc</term><description>Ascending index on <c>ClientId</c> — used by <c>GetByClientIdAsync()</c>.</description></item>
///   <item><term>LastStatusTransitionAt_asc</term><description>Ascending index on <c>LastStatusTransitionAt</c> — enables efficient ordering/filtering by transition time.</description></item>
/// </list>
/// <para>
/// <c>Background = false</c> is intentional: indexes are small and we want them ready
/// before the worker starts. Calling <c>CreateOneAsync</c> on an existing index is
/// idempotent — MongoDB only rebuilds when the index definition changes.
/// </para>
/// </remarks>
public sealed class MongoIndexBuilder(
    IMongoDatabase database,
    ILogger<MongoIndexBuilder> logger)
{
    private readonly IMongoCollection<Invoice> _collection =
        database.GetCollection<Invoice>("Invoices");

    private readonly IMongoCollection<Client> _clients =
        database.GetCollection<Client>("Clients");

    private readonly ILogger<MongoIndexBuilder> _logger = logger;

    /// <summary>
    /// Creates the required indexes if they do not already exist.
    /// Safe to call multiple times — MongoDB driver is idempotent for identical definitions.
    /// </summary>
    public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        var indexOptions = new CreateIndexOptions { Background = false };

        var models = new[]
        {
            new CreateIndexModel<Invoice>(
                Builders<Invoice>.IndexKeys.Ascending(x => x.Status),
                new CreateIndexOptions { Background = false, Name = "Status_asc" }),

            new CreateIndexModel<Invoice>(
                Builders<Invoice>.IndexKeys.Ascending(x => x.ClientId),
                new CreateIndexOptions { Background = false, Name = "ClientId_asc" }),

            new CreateIndexModel<Invoice>(
                Builders<Invoice>.IndexKeys.Ascending(x => x.LastStatusTransitionAt),
                new CreateIndexOptions { Background = false, Name = "LastStatusTransitionAt_asc" }),

            new CreateIndexModel<Invoice>(
                Builders<Invoice>.IndexKeys.Ascending(x => x.LastNotificationOutcome),
                new CreateIndexOptions { Background = false, Name = "LastNotificationOutcome_asc" }),
        };

        foreach (var model in models)
        {
            try
            {
                var indexName = await _collection.Indexes
                    .CreateOneAsync(model, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogInformation(
                    "Índice de MongoDB asegurado. Colección=Invoices Índice={IndexName}",
                    indexName);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(
                    ex,
                    "No se pudo crear el índice '{IndexName}' en la colección Invoices. Se continúa el arranque.",
                    model.Options?.Name ?? "(sin nombre)");
            }
        }

        await EnsureClientIndexesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Crea los índices de la colección <c>Clients</c> (spec 018): índice único sobre <c>Email</c>
    /// (case-insensitive vía collation strength 2, research D5) e índice ascendente sobre <c>Name</c>
    /// para el orden del listado. Idempotente.
    /// </summary>
    private async Task EnsureClientIndexesAsync(CancellationToken cancellationToken)
    {
        var models = new[]
        {
            new CreateIndexModel<Client>(
                Builders<Client>.IndexKeys.Ascending(x => x.Email),
                new CreateIndexOptions
                {
                    Background = false,
                    Unique = true,
                    Name = "Email_unique",
                    Collation = new Collation("en", strength: CollationStrength.Secondary),
                }),

            new CreateIndexModel<Client>(
                Builders<Client>.IndexKeys.Ascending(x => x.Name),
                new CreateIndexOptions { Background = false, Name = "Name_asc" }),
        };

        foreach (var model in models)
        {
            try
            {
                var indexName = await _clients.Indexes
                    .CreateOneAsync(model, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogInformation(
                    "Índice de MongoDB asegurado. Colección=Clients Índice={IndexName}",
                    indexName);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(
                    ex,
                    "No se pudo crear el índice '{IndexName}' en la colección Clients. Se continúa el arranque.",
                    model.Options?.Name ?? "(sin nombre)");
            }
        }
    }
}
