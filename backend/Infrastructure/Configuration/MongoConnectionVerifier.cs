using Backend.Infrastructure.Persistence;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Backend.Infrastructure.Configuration;

/// <summary>
/// Hosted service that verifies MongoDB connectivity at startup (FR-005, FR-006, research D1).
/// Issues a { ping: 1 } with bounded retries (~10s) and logs the structured result.
/// Fail-soft: a failure is logged as an error and leaves the app running with the
/// MongoDB health check reporting Unhealthy, rather than crash-looping the container.
/// </summary>
public sealed class MongoConnectionVerifier(
    IMongoDatabase database,
    IOptions<MongoDbOptions> options,
    ILogger<MongoConnectionVerifier> logger,
    MongoIndexBuilder indexBuilder) : IHostedService
{
    private static readonly TimeSpan TotalRetryWindow = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

    private readonly IMongoDatabase _database = database;
    private readonly MongoDbOptions _options = options.Value;
    private readonly ILogger<MongoConnectionVerifier> _logger = logger;
    private readonly MongoIndexBuilder _indexBuilder = indexBuilder;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var attempt = 0;
        Exception? lastError = null;

        while (DateTimeOffset.UtcNow - startedAt < TotalRetryWindow && !cancellationToken.IsCancellationRequested)
        {
            attempt++;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                await _database
                    .RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                sw.Stop();

                _logger.LogInformation(
                    "Conexión a MongoDB verificada. Base={DatabaseName} Intento={Attempt} DuracionMs={DurationMs}",
                    _options.DatabaseName,
                    attempt,
                    sw.ElapsedMilliseconds);

                await _indexBuilder.EnsureIndexesAsync(cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                sw.Stop();
                lastError = ex;

                try
                {
                    await Task.Delay(RetryDelay, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        if (lastError is not null)
        {
            var failure = MongoErrorClassifier.Classify(lastError);
            _logger.LogError(
                lastError,
                "No se pudo verificar la conexión a MongoDB tras {Attempt} intento(s). Base={DatabaseName} Tipo={FailureKind} Detalle={FailureMessage}",
                attempt,
                _options.DatabaseName,
                failure.Kind,
                failure.Message);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
