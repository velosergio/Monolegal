using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Serilog;

namespace Backend.Infrastructure.Configuration;

/// <summary>
/// Extension methods for registering infrastructure services in the DI container.
/// Centralizes MongoDB configuration (typed options + pooling), startup connection
/// verification and the MongoDB health check. See specs/004-mongodb-connection/.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var mongoOptions = BuildMongoOptions(configuration);
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(mongoOptions));

        // Build the client from explicit settings so pooling and server-selection
        // timeout are configured (FR-010, research D4).
        var clientSettings = MongoClientSettings.FromConnectionString(mongoOptions.ConnectionString);
        clientSettings.MaxConnectionPoolSize = mongoOptions.MaxConnectionPoolSize;
        clientSettings.ServerSelectionTimeout = mongoOptions.ServerSelectionTimeout;

        var mongoClient = new MongoClient(clientSettings);
        services.AddSingleton<IMongoClient>(mongoClient);
        services.AddSingleton<IMongoDatabase>(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(mongoOptions.DatabaseName);
        });

        // Startup connection verification with observable structured logging (FR-005/006/007).
        services.AddHostedService<MongoConnectionVerifier>();

        // Real MongoDB connectivity health check exposed at /health (FR-008).
        services.AddHealthChecks()
            .AddCheck<MongoHealthCheck>("mongodb");

        // Serilog logging
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddSerilog(dispose: true);
        });

        return services;
    }

    /// <summary>
    /// Resolves <see cref="MongoDbOptions"/> from configuration. The connection string is
    /// sourced from MONGODB_URI (docker-compose) or the ConnectionStrings:MongoDB key, unified
    /// across API and Worker. The database name comes from configuration, the connection
    /// string, or the default. See contracts/connection-config.md.
    /// </summary>
    public static MongoDbOptions BuildMongoOptions(IConfiguration configuration)
    {
        var options = new MongoDbOptions();
        // Bind non-sensitive values (pool size, timeout, optional overrides) if present.
        configuration.GetSection("MongoDb").Bind(options);

        var connectionString = configuration["MONGODB_URI"]
            ?? configuration.GetConnectionString("MongoDB")
            ?? options.ConnectionString;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "La cadena de conexión a MongoDB es obligatoria. Defina la variable de entorno MONGODB_URI.");
        }

        options.ConnectionString = connectionString;
        options.DatabaseName = ResolveDatabaseName(connectionString, options.DatabaseName);
        return options;
    }

    private static string ResolveDatabaseName(string connectionString, string configuredName)
    {
        // Prefer an explicitly configured/overridden non-default name; otherwise derive
        // from the connection string; otherwise fall back to the default.
        if (!string.IsNullOrWhiteSpace(configuredName)
            && configuredName != MongoDbOptions.DefaultDatabaseName)
        {
            return configuredName;
        }

        try
        {
            var url = MongoUrl.Create(connectionString);
            if (!string.IsNullOrWhiteSpace(url.DatabaseName))
            {
                return url.DatabaseName;
            }
        }
        catch (Exception)
        {
            // Malformed URI: defer to default; connection attempt will surface the error.
        }

        return string.IsNullOrWhiteSpace(configuredName)
            ? MongoDbOptions.DefaultDatabaseName
            : configuredName;
    }
}
