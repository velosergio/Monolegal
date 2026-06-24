using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Worker.Services;

/// <summary>
/// Main background worker service that runs as an ASP.NET Core Hosted Service.
/// Processes async jobs such as email reminders and state transitions.
/// </summary>
public class BackgroundWorker : IHostedService
{
    private readonly ILogger<BackgroundWorker> _logger;
    private CancellationTokenSource? _cts;
    private Task? _executingTask;

    public BackgroundWorker(ILogger<BackgroundWorker> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Triggered when the application host is ready to start the service.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("BackgroundWorker starting at: {Time}", DateTimeOffset.UtcNow);

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _executingTask = ExecuteAsync(_cts.Token);

        // If the task completed synchronously, bubble any exception
        if (_executingTask.IsCompleted)
        {
            return _executingTask;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Triggered when the application host is performing a graceful shutdown.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("BackgroundWorker stopping at: {Time}", DateTimeOffset.UtcNow);

        if (_executingTask == null)
        {
            return;
        }

        try
        {
            _cts?.Cancel();
        }
        finally
        {
            // Wait until the task completes or the stop token fires
            await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
        }
    }

    /// <summary>
    /// Core execution loop. Override this to implement actual background processing logic.
    /// </summary>
    private async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BackgroundWorker executing main loop");

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug("BackgroundWorker heartbeat at: {Time}", DateTimeOffset.UtcNow);

            // TODO: Implement actual job processing logic here
            // Example: process pending invoice reminders, status transitions, etc.

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }

        _logger.LogInformation("BackgroundWorker main loop exited");
    }
}
