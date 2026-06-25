using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Monolegal.Infrastructure.Workers;
using Shouldly;
using Xunit;

namespace Backend.Tests.Monolegal.Application.Tests;

/// <summary>
/// Pruebas de la configuración operativa del worker (<see cref="InvoiceTransitionsWorkerOptions"/>):
/// default razonable, enlace desde configuración y normalización de valores inválidos.
///
/// Validates: FR-001, FR-002, SC-005 | US2 (spec.md 012-worker-state-transitions)
/// </summary>
[Trait("Category", "Worker")]
public class InvoiceTransitionsWorkerOptionsTests
{
    // ── T010 [US2] — Default razonable sin configuración ─────────────────────────

    [Fact]
    public void Options_SinConfiguracion_UsaDefaults()
    {
        var options = new InvoiceTransitionsWorkerOptions();

        options.IntervalMinutes.ShouldBe(InvoiceTransitionsWorkerOptions.DefaultIntervalMinutes);
        options.IntervalMinutes.ShouldBe(60);
        options.RunOnStartup.ShouldBeTrue();
        options.HasInvalidInterval.ShouldBeFalse();
        options.GetInterval().ShouldBe(TimeSpan.FromMinutes(60));
    }

    // ── T011 [US2] — Enlace desde configuración (sección y variable de entorno) ───

    [Fact]
    public void Options_EnlazadasDesdeSeccion_TomaElValorConfigurado()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["InvoiceTransitionsWorker:IntervalMinutes"] = "5",
                ["InvoiceTransitionsWorker:RunOnStartup"] = "false"
            })
            .Build();

        var options = new InvoiceTransitionsWorkerOptions();
        configuration.GetSection(InvoiceTransitionsWorkerOptions.SectionName).Bind(options);

        options.IntervalMinutes.ShouldBe(5);
        options.RunOnStartup.ShouldBeFalse();
        options.GetInterval().ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void Options_EnlazadasDesdeVariableDeEntornoEstilo_TomaElValorConfigurado()
    {
        // El proveedor de variables de entorno traduce "InvoiceTransitionsWorker__IntervalMinutes"
        // a la clave jerárquica "InvoiceTransitionsWorker:IntervalMinutes".
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["InvoiceTransitionsWorker:IntervalMinutes"] = "15"
            })
            .Build();

        var options = new InvoiceTransitionsWorkerOptions();
        configuration.GetSection(InvoiceTransitionsWorkerOptions.SectionName).Bind(options);

        options.IntervalMinutes.ShouldBe(15);
    }

    // ── T012 [US2] — Valores inválidos caen al default ───────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Options_IntervaloInvalido_CaeAlDefault(int invalidMinutes)
    {
        var options = new InvoiceTransitionsWorkerOptions { IntervalMinutes = invalidMinutes };

        options.HasInvalidInterval.ShouldBeTrue();
        options.GetInterval().ShouldBe(TimeSpan.FromMinutes(InvoiceTransitionsWorkerOptions.DefaultIntervalMinutes));
    }
}
