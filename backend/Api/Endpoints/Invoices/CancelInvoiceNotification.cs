using Backend.Application.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Monolegal.Domain.Repositories;

namespace Monolegal.Api.Endpoints.Invoices;

/// <summary>
/// Endpoint POST /api/invoices/{id}/cancel-notification — "cancelar envío" (spec 019, US4): marca
/// como omitida (None → Skipped) una factura pendiente en estado notificable para que el worker no
/// la procese. 404 si no existe; 409 si no está pendiente. Conserva el registro.
/// </summary>
public static class CancelInvoiceNotification
{
    public static void MapCancelInvoiceNotification(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/invoices/{id}/cancel-notification", async (
            string id,
            IInvoiceShipmentService shipmentService,
            IClientRepository clientRepository,
            CancellationToken cancellationToken) =>
        {
            var result = await shipmentService.CancelAsync(id, cancellationToken);

            switch (result.Status)
            {
                case CancelNotificationStatus.NotFound:
                    return Results.NotFound();

                case CancelNotificationStatus.NotPending:
                    return Results.Conflict(new { error = "La factura no tiene un envío pendiente que cancelar." });

                default:
                    var invoice = result.Invoice!;
                    var client = await clientRepository.GetByIdAsync(invoice.ClientId, cancellationToken);
                    var dto = ShipmentListItemDto.FromEntity(invoice, client?.Name ?? invoice.ClientId, client?.Email);
                    return Results.Ok(dto);
            }
        })
        .WithName("CancelInvoiceNotification")
        .WithTags("Invoices")
        .WithSummary("Cancelar envío de una factura")
        .WithDescription(
            "Marca como omitida la notificación de una factura pendiente en estado notificable, para " +
            "que el worker no la procese. Conserva el registro. Devuelve 409 si la factura no está " +
            "pendiente y 404 si no existe.")
        .Produces<ShipmentListItemDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);
    }
}
