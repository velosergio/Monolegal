using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Backend.Infrastructure.Configuration;

/// <summary>
/// Health check that reports real MongoDB connectivity by issuing a lightweight
/// { ping: 1 } command against the configured database (FR-008).
/// Maps Healthy/Unhealthy to the /health endpoint (200/503).
/// See specs/004-mongodb-connection/contracts/health-endpoint.md.
/// </summary>
public sealed class MongoHealthCheck(IMongoDatabase database) : IHealthCheck
{
    private readonly IMongoDatabase _database = database;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _database
                .RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return HealthCheckResult.Healthy("MongoDB alcanzable.");
        }
        catch (Exception ex)
        {
            // Classify but keep the public health body free of sensitive detail.
            var failure = MongoErrorClassifier.Classify(ex);
            return HealthCheckResult.Unhealthy(failure.Message, ex);
        }
    }
}
