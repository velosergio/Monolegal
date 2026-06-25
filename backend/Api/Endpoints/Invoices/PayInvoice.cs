using Backend.Application.Abstractions;
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
            IInvoiceTransitionNotifier notifier,
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
            var previousStatus = invoice.Status;
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

            // 4. Notify the client (records the result on the invoice; a send failure does not
            //    revert the transition nor fail the response — spec 013).
            await notifier.NotifyTransitionAsync(invoice, previousStatus, cancellationToken);

            // 5. Persist the change (status + notification result).
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
        .WithTags("Invoices")
        .WithSummary("Marcar una factura como pagada")
        .WithDescription(
            "Aplica la transición de pago a la factura indicada desde cualquier estado activo válido. " +
            "Devuelve 404 si la factura no existe y 409 si el estado actual no permite el pago.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);
    }
}
