using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Monolegal.Domain.Repositories;

namespace Monolegal.Api.Endpoints.Invoices;

/// <summary>
/// Endpoint GET /api/invoices/stats — estadísticas agregadas para el dashboard.
/// Devuelve el total, el conteo por estado y el conteo por cliente. Invariante: Σ(byStatus)==total.
///
/// Validates: FR-015, FR-016 | US4 (spec.md 009-invoice-api-endpoints)
/// </summary>
public static class GetInvoiceStats
{
    public static void MapGetInvoiceStats(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/invoices/stats", async (
            IInvoiceRepository invoiceRepository,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(GetInvoiceStats));

            var total = await invoiceRepository.CountAsync(cancellationToken);
            var byStatusRaw = await invoiceRepository.CountByStatusAsync(cancellationToken);
            var byClient = await invoiceRepository.CountByClientAsync(cancellationToken);

            // Claves de byStatus como cadena de API en minúscula (research.md D1).
            var byStatus = new Dictionary<string, long>();
            foreach (var (status, count) in byStatusRaw)
                byStatus[InvoiceStatusApi.ToApiString(status)] = count;

            logger.LogInformation(
                "Estadísticas de facturas. Total={Total} Estados={EstadosCount} Clientes={ClientesCount}",
                total, byStatus.Count, byClient.Count);

            return Results.Ok(new InvoiceStatsDto(total, byStatus, byClient));
        })
        .WithName("GetInvoiceStats")
        .WithTags("Invoices")
        .WithSummary("Obtener estadísticas de facturas")
        .WithDescription(
            "Devuelve métricas agregadas para el dashboard: total de facturas, conteo por estado " +
            "('byStatus') y conteo por cliente ('byClient').")
        .Produces<InvoiceStatsDto>(StatusCodes.Status200OK);
    }
}
