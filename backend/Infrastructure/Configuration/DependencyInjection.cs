using Backend.Application.Abstractions;
using Backend.Application.Notifications;
using Backend.Infrastructure.Clients;
using Backend.Infrastructure.Email;
using Backend.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Monolegal.Domain.Repositories;
using Monolegal.Domain.Services;
using Monolegal.Infrastructure.Repositories;
using Monolegal.Infrastructure.Workers;
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

        // Domain services
        services.AddSingleton<InvoiceTransitionService>();

        // Repositories
        services.AddSingleton<ISystemSettingsRepository, MongoSystemSettingsRepository>();
        services.AddSingleton<IInvoiceRepository, MongoInvoiceRepository>();

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
        // MongoIndexBuilder is registered first so MongoConnectionVerifier can inject it.
        services.AddSingleton<MongoIndexBuilder>();
        services.AddHostedService<MongoConnectionVerifier>();

        // Background worker options: interval is operational config, bound from configuration
        // (section "InvoiceTransitionsWorker", overridable via env var InvoiceTransitionsWorker__IntervalMinutes).
        // Default interval applies when unset/invalid (spec 012, FR-001/FR-002, SC-005).
        var workerOptions = new InvoiceTransitionsWorkerOptions();
        configuration.GetSection(InvoiceTransitionsWorkerOptions.SectionName).Bind(workerOptions);
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(workerOptions));

        // Email notifications on transition (spec 013).
        // EmailOptions desde la sección "Email" (credenciales SMTP por variables de entorno).
        var emailOptions = new EmailOptions();
        configuration.GetSection(EmailOptions.SectionName).Bind(emailOptions);
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(emailOptions));
        services.AddSingleton<EmailTemplateProvider>();

        // Selección del emisor: SMTP si hay host configurado; en caso contrario NoOp (Dev/CI).
        if (!string.IsNullOrWhiteSpace(emailOptions.Host))
            services.AddSingleton<IEmailService, SmtpEmailService>();
        else
            services.AddSingleton<IEmailService, NoOpEmailService>();

        // Resolución del correo del cliente y orquestador de notificación en transición.
        services.AddSingleton<IClientEmailResolver, ConfiguredClientEmailResolver>();
        services.AddSingleton<IInvoiceTransitionNotifier, InvoiceTransitionNotifier>();

        // Background worker: evaluates and applies invoice status transitions periodically.
        services.AddHostedService<InvoiceTransitionsWorker>();

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
