using Backend.Infrastructure.Configuration;
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

    // OpenAPI/Swagger
    builder.Services.AddOpenApi();

    var app = builder.Build();

    // Middleware pipeline
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseHttpsRedirection();

    // Health check endpoint
    app.MapHealthChecks("/health");

    // Settings endpoints
    app.MapGetInvoiceTransitions();
    app.MapUpdateInvoiceTransitions();

    // Invoices endpoints
    app.MapPayInvoice();

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
