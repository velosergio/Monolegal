using Backend.Infrastructure.Configuration;
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

    // Infrastructure services (MongoDB, logging)
    builder.Services.AddInfrastructure(builder.Configuration);

    // Health checks
    builder.Services.AddHealthChecks();

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
