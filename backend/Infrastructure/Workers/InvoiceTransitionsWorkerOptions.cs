using System;

namespace Monolegal.Infrastructure.Workers;

/// <summary>
/// Configuración operativa del <see cref="InvoiceTransitionsWorker"/>. Se enlaza desde
/// <c>IConfiguration</c> (sección <see cref="SectionName"/>, sobreescribible por variable de
/// entorno <c>InvoiceTransitionsWorker__IntervalMinutes</c>). Es distinta de la configuración de
/// negocio (umbrales de días por transición), que vive en <c>SystemSettings</c>.
///
/// Spec: 012-worker-state-transitions (FR-001, FR-002, SC-005).
/// </summary>
public sealed class InvoiceTransitionsWorkerOptions
{
    /// <summary>Nombre de la sección de configuración.</summary>
    public const string SectionName = "InvoiceTransitionsWorker";

    /// <summary>Intervalo por defecto entre ciclos cuando no se configura uno válido (60 min).</summary>
    public const int DefaultIntervalMinutes = 60;

    /// <summary>Frecuencia entre ciclos, en minutos. Debe ser &gt; 0; valores inválidos caen al default.</summary>
    public int IntervalMinutes { get; set; } = DefaultIntervalMinutes;

    /// <summary>Si el primer ciclo se ejecuta inmediatamente al arrancar (true) o tras el primer intervalo (false).</summary>
    public bool RunOnStartup { get; set; } = true;

    /// <summary>
    /// Resuelve el intervalo efectivo como <see cref="TimeSpan"/>. Si <see cref="IntervalMinutes"/> no
    /// es positivo, se normaliza al default (<see cref="DefaultIntervalMinutes"/>).
    /// </summary>
    public TimeSpan GetInterval() =>
        IntervalMinutes > 0
            ? TimeSpan.FromMinutes(IntervalMinutes)
            : TimeSpan.FromMinutes(DefaultIntervalMinutes);

    /// <summary>Indica si el valor configurado de <see cref="IntervalMinutes"/> es inválido (no positivo).</summary>
    public bool HasInvalidInterval => IntervalMinutes <= 0;
}
