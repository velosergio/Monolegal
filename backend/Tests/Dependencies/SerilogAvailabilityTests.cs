using Serilog;
using Serilog.Core;
using Shouldly;
using Xunit;

namespace Backend.Tests.Dependencies;

/// <summary>
/// US3 - Smoke test de disponibilidad de la librería de logging estructurado.
/// Verifica que los tipos de Serilog resuelven y que es posible construir un logger.
/// No configura sinks (Console/JSON corresponde a fases posteriores); solo comprueba disponibilidad.
/// </summary>
public class SerilogAvailabilityTests
{
    [Fact]
    public void LoggerConfiguration_PuedeConstruirUnLogger()
    {
        Logger logger = new LoggerConfiguration().CreateLogger();

        logger.ShouldNotBeNull();
        logger.Dispose();
    }
}
