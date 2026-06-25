using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Monolegal.Domain.Repositories;

namespace Monolegal.Api.Endpoints.Invoices;

/// <summary>
/// Endpoint GET /api/invoices/{id} — detalle completo de una factura.
/// Un id inexistente o con formato inválido devuelve 404 de forma uniforme (Q4).
///
/// Validates: FR-008, FR-009 | US2 (spec.md 009-invoice-api-endpoints)
/// </summary>
public static class GetInvoiceById
{
    public static void MapGetInvoiceById(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/invoices/{id}", async (
            string id,
            IInvoiceRepository invoiceRepository,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(GetInvoiceById));

            var invoice = await invoiceRepository.GetByIdAsync(id, cancellationToken);
            if (invoice is null)
            {
                logger.LogWarning("Detalle no encontrado. InvoiceId={InvoiceId}", id);
                return Results.NotFound();
            }

            logger.LogInformation("Detalle de factura entregado. InvoiceId={InvoiceId}", id);
            return Results.Ok(InvoiceDetailDto.FromEntity(invoice));
        })
        .WithName("GetInvoiceById")
        .WithTags("Invoices");
    }
}
