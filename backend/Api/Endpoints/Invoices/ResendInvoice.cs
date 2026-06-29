using Backend.Application.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Monolegal.Domain.Repositories;

namespace Monolegal.Api.Endpoints.Invoices;

/// <summary>
/// Endpoint POST /api/invoices/{id}/resend — reenvía la notificación de una factura (spec 019, US2).
/// Incrementa el contador de reintentos. Fail-soft: un fallo de envío devuelve 200 con
/// <c>sendStatus: failed</c>, no 500. 404 si la factura no existe.
/// </summary>
public static class ResendInvoice
{
    public static void MapResendInvoice(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/invoices/{id}/resend", async (
            string id,
            IInvoiceShipmentService shipmentService,
            IClientRepository clientRepository,
            CancellationToken cancellationToken) =>
        {
            var invoice = await shipmentService.ResendAsync(id, cancellationToken);
            if (invoice is null)
                return Results.NotFound();

            var client = await clientRepository.GetByIdAsync(invoice.ClientId, cancellationToken);
            var dto = ShipmentListItemDto.FromEntity(invoice, client?.Name ?? invoice.ClientId, client?.Email);
            return Results.Ok(dto);
        })
        .WithName("ResendInvoice")
        .WithTags("Invoices")
        .WithSummary("Reenviar notificación de una factura")
        .WithDescription(
            "Reenvía la notificación correspondiente al estado actual de la factura e incrementa el " +
            "contador de reintentos. Fail-soft: un fallo de envío se refleja como 'failed' (HTTP 200). " +
            "Devuelve el ítem de envío actualizado.")
        .Produces<ShipmentListItemDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);
    }
}
