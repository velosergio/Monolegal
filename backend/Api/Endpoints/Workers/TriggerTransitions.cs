using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Monolegal.Domain.Repositories;
using Monolegal.Domain.Services;

namespace Monolegal.Api.Endpoints.Workers;

/// <summary>
/// Endpoint POST /api/workers/trigger-transitions
///
/// Dispara manualmente un ciclo de transiciones de estado de facturas,
/// replicando la lógica del <see cref="Monolegal.Infrastructure.Workers.InvoiceTransitionsWorker"/>.
/// Útil para pruebas E2E y forzar el procesamiento sin esperar el intervalo horario.
///
/// Devuelve un resumen JSON con:
/// - evaluated: número de facturas candidatas evaluadas
/// - transitioned: número de facturas que efectivamente cambiaron de estado
///
/// Validates: FR-001, FR-002, FR-003 | US1 (spec.md 006-invoice-status-transitions)
/// </summary>
public static class TriggerTransitions
{
    public static void MapTriggerTransitions(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/workers/trigger-transitions", async (
            IInvoiceRepository invoiceRepository,
            ISystemSettingsRepository settingsRepository,
            InvoiceTransitionService transitionService,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(TriggerTransitions));

            logger.LogInformation(
                "Trigger manual de transiciones iniciado. Timestamp={Timestamp:o}",
                DateTimeOffset.UtcNow);

            // 1. Load transition configuration.
            var settings = await settingsRepository.GetSettingsAsync();
            var config = settings.InvoiceTransitions;

            // 2. Fetch all invoices eligible for automatic transition.
            var candidates = await invoiceRepository
                .GetTransitionableAsync(cancellationToken);

            var now = DateTime.UtcNow;
            int evaluated = 0;
            int transitioned = 0;

            // 3. Evaluate and apply transitions.
            foreach (var invoice in candidates)
            {
                evaluated++;

                if (transitionService.TryApplyTransition(invoice, config, now))
                {
                    // 4. Persist the change.
                    await invoiceRepository.UpdateAsync(invoice, cancellationToken);
                    transitioned++;
                }
            }

            logger.LogInformation(
                "Trigger manual de transiciones completado. Evaluadas={Evaluated} Transicionadas={Transitioned}",
                evaluated,
                transitioned);

            return Results.Ok(new
            {
                evaluated,
                transitioned
            });
        })
        .WithName("TriggerTransitions")
        .WithTags("Workers");
    }
}
