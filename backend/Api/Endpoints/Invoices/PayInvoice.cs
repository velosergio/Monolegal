using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Monolegal.Domain.Repositories;
using Monolegal.Domain.Services;

namespace Monolegal.Api.Endpoints.Invoices;

/// <summary>
/// Endpoint POST /api/invoices/{id}/pay
///
/// Marca manualmente una factura como pagada desde cualquier estado activo válido.
/// Devuelve el id, el nuevo estado y la fecha de última transición.
///
/// Validates: FR-004 | US2 (spec.md 006-invoice-status-transitions)
/// </summary>
public static class PayInvoice
{
    public static void MapPayInvoice(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/invoices/{id}/pay", async (
            string id,
            IInvoiceRepository invoiceRepository,
            InvoiceTransitionService transitionService,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(PayInvoice));

            // 1. Fetch invoice by id.
            var invoice = await invoiceRepository.GetByIdAsync(id, cancellationToken);

            // 2. Not found → 404.
            if (invoice is null)
            {
                logger.LogWarning(
                    "Pago rechazado — factura no encontrada. InvoiceId={InvoiceId}",
                    id);
                return Results.NotFound();
            }

            // 3. Apply payment transition.
            try
            {
                transitionService.ApplyPayment(invoice);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(
                    "Pago rechazado — conflicto de estado. InvoiceId={InvoiceId} Error={Error}",
                    id,
                    ex.Message);
                return Results.Conflict(new { error = ex.Message });
            }

            // 4. Persist the change.
            await invoiceRepository.UpdateAsync(invoice, cancellationToken);

            logger.LogInformation(
                "Pago aplicado exitosamente. InvoiceId={InvoiceId} NuevoEstado={NewStatus}",
                invoice.Id,
                invoice.Status);

            // 5. Return updated state.
            return Results.Ok(new
            {
                id = invoice.Id,
                status = invoice.Status,
                lastStatusTransitionAt = invoice.LastStatusTransitionAt
            });
        })
        .WithName("PayInvoice")
        .WithTags("Invoices");
    }
}
