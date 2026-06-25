using MongoDB.Bson;
using MongoDB.Driver;
using Shouldly;
using Xunit;

namespace Backend.Tests.Infrastructure;

/// <summary>
/// Integration tests verifying real connectivity to MongoDB (FR-005, T011).
/// Requires a running MongoDB (docker-compose up mongo). The connection string is
/// sourced from the MONGODB_URI environment variable (same source as the app).
/// </summary>
[Trait("Category", "Integration")]
public class MongoConnectionTests
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("MONGODB_URI")
        ?? "mongodb://root:example_dev_password@localhost:27017/monolegal_dev?authSource=admin";

    private static IMongoDatabase GetDatabase()
    {
        var settings = MongoClientSettings.FromConnectionString(ConnectionString);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
        var client = new MongoClient(settings);
        var dbName = MongoUrl.Create(ConnectionString).DatabaseName ?? "monolegal_dev";
        return client.GetDatabase(dbName);
    }

    [Fact]
    public async Task Ping_Succeeds_AgainstMonolegalDev()
    {
        var database = GetDatabase();

        var result = await database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));

        result["ok"].ToDouble().ShouldBe(1.0);
    }

    [Fact]
    public async Task Database_IsNamed_MonolegalDev()
    {
        var database = GetDatabase();

        database.DatabaseNamespace.DatabaseName.ShouldBe("monolegal_dev");

        // The database is reachable for read operations (collection listing).
        var collections = await (await database.ListCollectionNamesAsync()).ToListAsync();
        collections.ShouldNotBeNull();
    }
}
