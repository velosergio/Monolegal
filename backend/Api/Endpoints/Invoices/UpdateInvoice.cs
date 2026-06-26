using System;
using System.Collections.Generic;
using System.Linq;
using Backend.Application.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Repositories;
using Monolegal.Domain.Services;

namespace Monolegal.Api.Endpoints.Invoices;

/// <summary>
/// Endpoint PUT /api/invoices/{id} — edita una factura (spec 018, RF-003). 404 si no existe; 409 si
/// está en estado terminal (RF-004a); valida cuerpo y existencia del cliente; recalcula el monto.
/// </summary>
public static class UpdateInvoice
{
    public static void MapUpdateInvoice(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/invoices/{id}", async (
            string id,
            [FromBody] UpdateInvoiceRequest request,
            IInvoiceRepository invoiceRepository,
            IClientRepository clientRepository,
            InvoiceTransitionService transitionService,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(UpdateInvoice));

            var input = new UpdateInvoiceInput(
                request?.ClientId,
                request?.DueDate,
                request?.Items?.Select(i => new InvoiceItemInputModel(i.Description, i.Quantity, i.UnitPrice)).ToList());

            var validation = await new UpdateInvoiceValidator().ValidateAsync(input, cancellationToken);
            if (!validation.IsValid)
            {
                logger.LogWarning("Edición de factura rechazada — cuerpo inválido. InvoiceId={InvoiceId}", id);
                return Results.ValidationProblem(validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
            }

            var invoice = await invoiceRepository.GetByIdAsync(id, cancellationToken);
            if (invoice is null)
            {
                logger.LogWarning("Edición de factura rechazada — no encontrada. InvoiceId={InvoiceId}", id);
                return Results.NotFound();
            }

            if (invoice.IsTerminal)
            {
                logger.LogWarning(
                    "Edición de factura rechazada — estado terminal. InvoiceId={InvoiceId} Estado={Status}",
                    id, invoice.Status);
                return Results.Conflict(new { error = "No se puede editar una factura en estado terminal (pagado/desactivado)." });
            }

            var client = await clientRepository.GetByIdAsync(request!.ClientId!, cancellationToken);
            if (client is null)
            {
                logger.LogWarning("Edición de factura rechazada — cliente inexistente. ClientId={ClientId}", request.ClientId);
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["clientId"] = new[] { "El cliente indicado no existe." },
                });
            }

            var items = request.Items!
                .Select(i => new InvoiceItem(i.Description!, i.Quantity, i.UnitPrice))
                .ToList();

            try
            {
                invoice.UpdateDetails(request.ClientId!, items, request.DueDate!.Value);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }

            await invoiceRepository.UpdateAsync(invoice, cancellationToken);

            logger.LogInformation(
                "Factura editada. InvoiceId={InvoiceId} ClientId={ClientId} Monto={Amount}",
                invoice.Id, invoice.ClientId, invoice.Amount);

            var allowed = transitionService.GetAllowedTransitions(invoice.Status);
            return Results.Ok(InvoiceDetailDto.FromEntity(invoice, allowed));
        })
        .WithName("UpdateInvoice")
        .WithTags("Invoices")
        .WithSummary("Editar factura")
        .WithDescription("Edita cliente, items y vencimiento de una factura no terminal. Recalcula el monto.")
        .Produces<InvoiceDetailDto>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);
    }
}
