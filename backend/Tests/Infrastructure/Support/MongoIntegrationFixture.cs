using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;
using Backend.Infrastructure.Persistence;
using Monolegal.Infrastructure.Repositories;
using Xunit;

namespace Backend.Tests.Infrastructure.Support;

/// <summary>
/// Fixture compartido para los tests de integración del repositorio de facturas (spec 007, T003).
///
/// Provisiona una base de datos MongoDB con nombre único (sufijo GUID) por instancia de fixture
/// para garantizar aislamiento entre clases de test, y la elimina (drop) al finalizar. La cadena
/// de conexión proviene de la variable de entorno <c>MONGODB_URI</c> (mismo origen que la app),
/// con un default de desarrollo. Requiere un MongoDB en ejecución (docker-compose up -d mongo).
/// </summary>
/// <remarks>
/// Patrón alineado con <c>MongoConnectionTests</c>: <c>[Trait("Category", "Integration")]</c> y
/// <c>MONGODB_URI</c>. Ver research.md D5/D6.
/// </remarks>
public sealed class MongoIntegrationFixture : IAsyncLifetime
{
    private const string DefaultConnectionString =
        "mongodb://root:example_dev_password@localhost:27017/monolegal_dev?authSource=admin";

    private readonly string _databaseName = $"monolegal_it_{Guid.NewGuid():N}";
    private IMongoClient _client = null!;

    /// <summary>Base de datos efímera y aislada para esta clase de test.</summary>
    public IMongoDatabase Database { get; private set; } = null!;

    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("MONGODB_URI") ?? DefaultConnectionString;

    public async Task InitializeAsync()
    {
        var settings = MongoClientSettings.FromConnectionString(ConnectionString);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
        _client = new MongoClient(settings);
        Database = _client.GetDatabase(_databaseName);

        // Verificación de conectividad temprana con mensaje claro si Mongo no está disponible.
        try
        {
            await Database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "No se pudo conectar a MongoDB para los tests de integración. " +
                "Arranque el servicio (docker compose up -d mongo) y defina MONGODB_URI. " +
                $"Cadena usada: {ConnectionString}", ex);
        }
    }

    /// <summary>
    /// Devuelve un repositorio sobre una colección <c>Invoices</c> recién vaciada,
    /// dando a cada test un punto de partida limpio y determinista.
    /// </summary>
    public async Task<MongoInvoiceRepository> CreateCleanRepositoryAsync()
    {
        await Database.DropCollectionAsync("Invoices");
        return new MongoInvoiceRepository(Database);
    }

    /// <summary>Crea el builder de índices sobre la base efímera (US5).</summary>
    public MongoIndexBuilder CreateIndexBuilder()
        => new(Database, NullLogger<MongoIndexBuilder>.Instance);

    /// <summary>Acceso directo a la colección de facturas para aserciones de bajo nivel (índices).</summary>
    public IMongoCollection<MongoDB.Bson.BsonDocument> InvoicesRaw
        => Database.GetCollection<MongoDB.Bson.BsonDocument>("Invoices");

    public async Task DisposeAsync()
    {
        if (_client is not null)
        {
            await _client.DropDatabaseAsync(_databaseName);
        }
    }
}
