using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Monolegal.Domain.Repositories;

namespace Monolegal.Api.Endpoints.Invoices;

/// <summary>
/// Endpoint DELETE /api/invoices/{id} — elimina permanentemente una factura (spec 018, RF-005/RF-010).
/// Permitido en cualquier estado. 204 si se eliminó, 404 si no existía. La confirmación es del frontend.
/// </summary>
public static class DeleteInvoice
{
    public static void MapDeleteInvoice(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/invoices/{id}", async (
            string id,
            IInvoiceRepository invoiceRepository,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(DeleteInvoice));

            var deleted = await invoiceRepository.DeleteAsync(id, cancellationToken);
            if (!deleted)
            {
                logger.LogWarning("Eliminación de factura — no encontrada. InvoiceId={InvoiceId}", id);
                return Results.NotFound();
            }

            logger.LogInformation("Factura eliminada. InvoiceId={InvoiceId}", id);
            return Results.NoContent();
        })
        .WithName("DeleteInvoice")
        .WithTags("Invoices")
        .WithSummary("Eliminar factura")
        .WithDescription("Elimina permanentemente una factura (hard delete), en cualquier estado.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);
    }
}
