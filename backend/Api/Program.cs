using System.Text.Json.Serialization;
using Backend.Api.OpenApi;
using Backend.Application.Abstractions;
using Backend.Application.Seeding;
using Backend.Infrastructure.Configuration;
using Backend.Infrastructure.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Monolegal.Api.Endpoints.Invoices;
using Monolegal.Api.Endpoints.Settings;
using Monolegal.Api.Endpoints.Workers;
using Serilog;

// Configure Serilog early
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog as the logging provider
    builder.Host.UseSerilog();

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

    // Development-only data seeder (spec 008). Gate de seguridad: en producción NO se
    // registra ni se ejecuta. Se registra tras AddInfrastructure para correr después de
    // la verificación de conexión a MongoDB.
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddSingleton<IDevDataSeeder, DevDataSeeder>();
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

    // Invoices endpoints
    app.MapPayInvoice();
    app.MapListInvoices();
    app.MapGetInvoiceById();
    app.MapTransitionInvoice();
    app.MapGetInvoiceStats();

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
