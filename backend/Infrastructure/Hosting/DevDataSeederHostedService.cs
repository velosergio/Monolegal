using System;
using System.Threading;
using System.Threading.Tasks;
using Backend.Application.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Backend.Infrastructure.Hosting;

/// <summary>
/// Hosted service que dispara el sembrador de datos de desarrollo al arranque
/// (spec 008, US1). Se registra en DI <b>únicamente en entorno Development</b>
/// (gate de seguridad, research D5): en producción ni se registra ni se ejecuta.
/// Fail-soft: un fallo se loguea y no detiene el arranque de la aplicación.
/// </summary>
public sealed class DevDataSeederHostedService(
    IDevDataSeeder seeder,
    ILogger<DevDataSeederHostedService> logger) : IHostedService
{
    private readonly IDevDataSeeder _seeder = seeder;
    private readonly ILogger<DevDataSeederHostedService> _logger = logger;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _seeder.SeedAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Falló la ejecución del seed de desarrollo. Se continúa el arranque.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
