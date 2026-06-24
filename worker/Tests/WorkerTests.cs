using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Worker.Services;
using Xunit;

namespace Worker.Tests;

/// <summary>
/// Basic smoke tests that validate the Hosted Service setup wires correctly.
/// </summary>
public class WorkerTests
{
    [Fact]
    public void BackgroundWorker_CanBeRegistered_AsHostedService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHostedService<BackgroundWorker>();

        // Act
        var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>();

        // Assert
        Assert.Contains(hostedServices, s => s is BackgroundWorker);
    }

    [Fact]
    public async Task BackgroundWorker_StartAndStop_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole());
        var provider = services.BuildServiceProvider();

        var logger = provider.GetRequiredService<ILogger<BackgroundWorker>>();
        var worker = new BackgroundWorker(logger);

        // Act / Assert: start then immediately stop should not throw
        var cts = new CancellationTokenSource();
        await worker.StartAsync(cts.Token);
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);
    }
}
