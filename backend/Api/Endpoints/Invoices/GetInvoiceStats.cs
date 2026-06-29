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
            IClientRepository clientRepository,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(GetInvoiceStats));

            var total = await invoiceRepository.CountAsync(cancellationToken);
            var byStatusRaw = await invoiceRepository.CountByStatusAsync(cancellationToken);
            var byClientRaw = await invoiceRepository.CountByClientAsync(cancellationToken);

            // Claves de byStatus como cadena de API en minúscula (research.md D1).
            var byStatus = new Dictionary<string, long>();
            foreach (var (status, count) in byStatusRaw)
                byStatus[InvoiceStatusApi.ToApiString(status)] = count;

            // Reemplaza la clave clientId por el nombre legible del cliente para el gráfico
            // "Facturas por cliente". Ante un cliente inexistente se conserva el clientId; si dos
            // clientes comparten nombre, sus conteos se agregan bajo la misma clave.
            var byClient = new Dictionary<string, long>();
            foreach (var (clientId, count) in byClientRaw)
            {
                var client = await clientRepository.GetByIdAsync(clientId, cancellationToken);
                var key = client?.Name ?? clientId;
                byClient[key] = byClient.TryGetValue(key, out var existing) ? existing + count : count;
            }

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
