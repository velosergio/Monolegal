using System.Text.Json.Serialization;
using Backend.Api.OpenApi;
using Backend.Application.Abstractions;
using Backend.Infrastructure.Configuration;
using Backend.Infrastructure.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Monolegal.Api.Endpoints.Clients;
using Monolegal.Api.Endpoints.Invoices;
using Monolegal.Api.Endpoints.Settings;
using Monolegal.Api.Endpoints.Workers;
using Serilog;
using Serilog.Formatting.Compact;

// Configure Serilog early (bootstrap logger hasta que el host lo reconfigure)
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog as the logging provider. Sink de archivo en JSON estructurado (CompactJsonFormatter)
    // para persistir la observabilidad de notificaciones/transiciones (spec 013, 3.4), conservando
    // la consola. La ruta es configurable vía "Logging:File:Path".
    builder.Host.UseSerilog((context, loggerConfiguration) =>
    {
        var logPath = context.Configuration["Logging:File:Path"] ?? "logs/monolegal-.json";
        loggerConfiguration
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(new CompactJsonFormatter(), logPath, rollingInterval: RollingInterval.Day);
    });

    // Infrastructure services (MongoDB connection, startup verification,
    // "mongodb" health check, logging) — see specs/004-mongodb-connection.
    builder.Services.AddInfrastructure(builder.Configuration);

    // Serialización JSON: los valores de InvoiceStatus se intercambian como cadena en
    // minúscula en el contrato HTTP (research.md D1, spec 009). Aplica a todos los enums.
    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.Converters.Add(
            new JsonStringEnumConverter(new LowerCaseNamingPolicy()));
    });

    // Development-only data seeder (spec 008). El servicio IDevDataSeeder se registra siempre en
    // AddInfrastructure (es idempotente y lo reutiliza la zona de peligro); aquí sólo se habilita
    // el DISPARO automático al arranque, restringido a Development como gate de seguridad.
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddHostedService<DevDataSeederHostedService>();
    }

    // OpenAPI/Swagger — el documento se genera con Microsoft.AspNetCore.OpenApi.
    // Metadatos del documento (título/versión) deterministas para la documentación (spec 010, T006).
    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            document.Info.Title = "Monolegal API";
            document.Info.Version = "v1";
            return Task.CompletedTask;
        });

        // Declara el esquema de seguridad Bearer/JWT para habilitar "Authorize" en Swagger UI
        // (spec 010, US3, FR-011).
        options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    });

    var app = builder.Build();

    // Middleware pipeline. Documentación interactiva restringida a Development (spec 010, D3):
    // el documento OpenAPI se sirve en /openapi/v1.json y la UI Swagger en /swagger.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/openapi/v1.json", "Monolegal API v1");
        });
    }

    app.UseHttpsRedirection();

    // Health check endpoint
    app.MapHealthChecks("/health");

    // Settings endpoints
    app.MapGetInvoiceTransitions();
    app.MapUpdateInvoiceTransitions();

    // Email settings endpoints (spec 017 — US1)
    app.MapGetEmailSettings();
    app.MapUpdateEmailSettings();
    app.MapValidateEmailCredentials();

    // Email templates endpoints (spec 017 — US2)
    app.MapGetEmailTemplates();
    app.MapUpdateEmailTemplate();
    app.MapResetEmailTemplate();
    app.MapPreviewEmailTemplate();

    // Email test send endpoint (spec 017 — US3)
    app.MapSendTestEmail();

    // Email admin tools endpoints (spec 017 — US4)
    app.MapResendFailedNotifications();
    app.MapSanitizeStuckNotifications();

    // Maintenance "danger zone" endpoints (/configuracion)
    app.MapDeleteAllData();
    app.MapFlushDatabase();

    // Invoices endpoints
    app.MapPayInvoice();
    app.MapListInvoices();
    app.MapGetInvoiceById();
    app.MapTransitionInvoice();
    app.MapGetInvoiceStats();

    // Invoices CRUD endpoints (spec 018 — US1)
    app.MapCreateInvoice();
    app.MapUpdateInvoice();
    app.MapDeleteInvoice();

    // Clients CRUD endpoints (spec 018 — US2)
    app.MapListClients();
    app.MapGetClientById();
    app.MapCreateClient();
    app.MapUpdateClient();
    app.MapDeleteClient();

    // Workers endpoints
    app.MapTriggerTransitions();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Declaración pública parcial de Program para permitir que las pruebas de integración
/// (WebApplicationFactory&lt;Program&gt;) referencien el host de la aplicación (spec 010, T007).
/// </summary>
public partial class Program { }
