using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Serilog;

namespace Backend.Infrastructure.Configuration;

/// <summary>
/// Extension methods for registering infrastructure services in the DI container.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // MongoDB setup
        var mongoConnectionString = configuration.GetConnectionString("MongoDB")
            ?? configuration["MONGODB_URI"]
            ?? "mongodb://localhost:27017";

        var mongoClient = new MongoClient(mongoConnectionString);
        services.AddSingleton<IMongoClient>(mongoClient);
        services.AddSingleton<IMongoDatabase>(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            var dbName = configuration["MongoDB:DatabaseName"] ?? "monolegal_dev";
            return client.GetDatabase(dbName);
        });

        // Serilog logging
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddSerilog(dispose: true);
        });

        return services;
    }
}
