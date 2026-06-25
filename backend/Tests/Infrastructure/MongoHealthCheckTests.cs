using Backend.Infrastructure.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;
using Shouldly;
using Xunit;

namespace Backend.Tests.Infrastructure;

/// <summary>
/// Integration tests for the MongoDB health check (FR-008, T012).
/// Contract: specs/004-mongodb-connection/contracts/health-endpoint.md.
/// </summary>
[Trait("Category", "Integration")]
public class MongoHealthCheckTests
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("MONGODB_URI")
        ?? "mongodb://root:example_dev_password@localhost:27017/monolegal_dev?authSource=admin";

    private static IMongoDatabase Database(string connectionString, string dbName)
    {
        var settings = MongoClientSettings.FromConnectionString(connectionString);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(3);
        return new MongoClient(settings).GetDatabase(dbName);
    }

    [Fact]
    public async Task CheckHealth_ReturnsHealthy_WhenMongoAvailable()
    {
        var check = new MongoHealthCheck(Database(ConnectionString, "monolegal_dev"));

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealth_ReturnsUnhealthy_WhenServerUnreachable()
    {
        // Non-routable host with a short server-selection timeout forces failure quickly.
        var check = new MongoHealthCheck(
            Database("mongodb://10.255.255.1:27017/monolegal_dev", "monolegal_dev"));

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNullOrEmpty();
    }
}
