using Backend.Application.Abstractions;
using Backend.Application.Notifications;
using Backend.Application.Seeding;
using Backend.Application.Services;
using Backend.Infrastructure.Clients;
using Backend.Infrastructure.Email;
using Backend.Infrastructure.Maintenance;
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
        // Mapeos BSON personalizados (spec 017): claves enum en diccionarios persistidos.
        // Debe registrarse antes de cualquier (de)serialización de SystemSettings.
        MongoSerializationConfig.Register();

        var mongoOptions = BuildMongoOptions(configuration);
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(mongoOptions));

        // Domain services
        services.AddSingleton<InvoiceTransitionService>();

        // Repositories
        services.AddSingleton<ISystemSettingsRepository, MongoSystemSettingsRepository>();
        services.AddSingleton<IInvoiceRepository, MongoInvoiceRepository>();
        services.AddSingleton<IClientRepository, MongoClientRepository>();

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

        // Migración única e idempotente del historial de estados y limpieza de estados legacy
        // (spec 015, FR-030/FR-031). Se ejecuta al arranque y es segura de reejecutar.
        services.AddHostedService<Backend.Infrastructure.Hosting.StatusHistoryBackfillMigration>();

        // Migración única e idempotente de items/vencimiento para facturas previas (spec 018, D6).
        services.AddHostedService<Backend.Infrastructure.Hosting.InvoiceItemsBackfillMigration>();

        // Background worker options: interval is operational config, bound from configuration
        // (section "InvoiceTransitionsWorker", overridable via env var InvoiceTransitionsWorker__IntervalMinutes).
        // Default interval applies when unset/invalid (spec 012, FR-001/FR-002, SC-005).
        var workerOptions = new InvoiceTransitionsWorkerOptions();
        configuration.GetSection(InvoiceTransitionsWorkerOptions.SectionName).Bind(workerOptions);
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(workerOptions));

        // Email notifications on transition (spec 013) + configuración multi-proveedor (spec 017).
        // EmailOptions desde la sección "Email": SOLO secretos y defaults de arranque (entorno).
        var emailOptions = new EmailOptions();
        configuration.GetSection(EmailOptions.SectionName).Bind(emailOptions);
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(emailOptions));
        services.AddSingleton<EmailTemplateProvider>();

        // HttpClient para el proveedor Resend (API REST).
        services.AddHttpClient("resend");

        // Proveedores concretos, factory y estado de credenciales (spec 017, D1/FR-008).
        services.AddSingleton<IEmailProvider, SmtpEmailProvider>();
        services.AddSingleton<IEmailProvider, ResendEmailProvider>();
        services.AddSingleton<IEmailProviderFactory, EmailProviderFactory>();
        services.AddSingleton<IEmailCredentialStatus, EmailCredentialStatusService>();

        // Emisor de alto nivel: si hay algún proveedor configurado (host SMTP o API key Resend),
        // se usa el servicio respaldado por settings (proveedor activo en runtime). En Dev/CI sin
        // proveedor configurado se mantiene NoOp para no requerir un servidor real.
        var anyProviderConfigured = !string.IsNullOrWhiteSpace(emailOptions.Host)
            || !string.IsNullOrWhiteSpace(emailOptions.Resend.ApiKey);
        if (anyProviderConfigured)
            services.AddSingleton<IEmailService, SettingsBackedEmailService>();
        else
            services.AddSingleton<IEmailService, NoOpEmailService>();

        // Resolución del correo del cliente (spec 018, D8): la implementación principal lee de la
        // colección Clients y recurre al resolver basado en configuración cuando no hay dato.
        services.AddSingleton<ConfiguredClientEmailResolver>();
        services.AddSingleton<IClientEmailResolver>(sp => new ClientRepositoryEmailResolver(
            sp.GetRequiredService<IClientRepository>(),
            sp.GetRequiredService<ConfiguredClientEmailResolver>()));
        services.AddSingleton<IInvoiceTransitionNotifier, InvoiceTransitionNotifier>();

        // Herramientas globales de administración de envíos (spec 017, US4).
        services.AddSingleton<IEmailAdminService, EmailAdminService>();

        // Acciones de envío por factura (spec 019, US2/US4): reenvío y cancelación.
        services.AddSingleton<IInvoiceShipmentService, InvoiceShipmentService>();

        // Sembrador de datos de desarrollo. Se registra SIEMPRE (es idempotente: sólo siembra con
        // la base vacía) para que la zona de peligro pueda re-sembrar tras un flush. El disparo
        // automático al arranque sigue restringido a Development (ver Program.cs).
        services.AddSingleton<IDevDataSeeder, DevDataSeeder>();

        // Operaciones destructivas de mantenimiento de la "zona de peligro" (/configuracion).
        services.AddSingleton<IMaintenanceService, MaintenanceService>();

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
