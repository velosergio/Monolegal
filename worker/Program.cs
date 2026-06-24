using MongoDB.Driver;
using Serilog;
using Worker.Services;

// Bootstrap Serilog early so startup errors are captured
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Worker host");

    var builder = Host.CreateApplicationBuilder(args);

    // -------------------------------------------------------------------------
    // Configuration: merge appsettings files
    // -------------------------------------------------------------------------
    builder.Configuration
        .AddJsonFile("Configuration/appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile(
            $"Configuration/appsettings.{builder.Environment.EnvironmentName}.json",
            optional: true,
            reloadOnChange: true)
        .AddEnvironmentVariables();

    // -------------------------------------------------------------------------
    // Serilog: replace default logging with Serilog
    // -------------------------------------------------------------------------
    builder.Services.AddSerilog((services, loggerConfig) =>
        loggerConfig
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext());

    // -------------------------------------------------------------------------
    // MongoDB: register singleton client
    // -------------------------------------------------------------------------
    var mongoConnectionString = builder.Configuration["MongoDB:ConnectionString"]
        ?? throw new InvalidOperationException("MongoDB:ConnectionString is required");

    builder.Services.AddSingleton<IMongoClient>(_ =>
        new MongoClient(mongoConnectionString));

    builder.Services.AddSingleton<IMongoDatabase>(provider =>
    {
        var client = provider.GetRequiredService<IMongoClient>();
        var dbName = builder.Configuration["MongoDB:DatabaseName"] ?? "monolegal_dev";
        return client.GetDatabase(dbName);
    });

    // -------------------------------------------------------------------------
    // Hosted Services
    // -------------------------------------------------------------------------
    builder.Services.AddHostedService<BackgroundWorker>();

    // -------------------------------------------------------------------------
    // Build & Run
    // -------------------------------------------------------------------------
    var host = builder.Build();
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Worker host terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

return 0;
