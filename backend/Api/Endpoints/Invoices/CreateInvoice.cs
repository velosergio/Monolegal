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
/// Endpoint POST /api/invoices — crea una factura (spec 018, RF-001/RF-006). Valida el cuerpo
/// (FluentValidation), exige que el cliente exista, deriva el monto de los items y devuelve 201.
/// </summary>
public static class CreateInvoice
{
    public static void MapCreateInvoice(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/invoices", async (
            [FromBody] CreateInvoiceRequest request,
            IInvoiceRepository invoiceRepository,
            IClientRepository clientRepository,
            InvoiceTransitionService transitionService,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(CreateInvoice));

            var input = new CreateInvoiceInput(
                request?.ClientId,
                request?.DueDate,
                request?.Items?.Select(i => new InvoiceItemInputModel(i.Description, i.Quantity, i.UnitPrice)).ToList());

            var validation = await new CreateInvoiceValidator().ValidateAsync(input, cancellationToken);
            if (!validation.IsValid)
            {
                logger.LogWarning("Alta de factura rechazada — cuerpo inválido. ClientId={ClientId}", request?.ClientId);
                return Results.ValidationProblem(validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
            }

            // El cliente debe existir (integridad referencial validada en aplicación — research D4).
            var client = await clientRepository.GetByIdAsync(request!.ClientId!, cancellationToken);
            if (client is null)
            {
                logger.LogWarning("Alta de factura rechazada — cliente inexistente. ClientId={ClientId}", request.ClientId);
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["clientId"] = new[] { "El cliente indicado no existe." },
                });
            }

            var items = request.Items!
                .Select(i => new InvoiceItem(i.Description!, i.Quantity, i.UnitPrice))
                .ToList();
            var invoice = Invoice.Create(request.ClientId!, items, request.DueDate!.Value);

            await invoiceRepository.AddAsync(invoice, cancellationToken);

            logger.LogInformation(
                "Factura creada. InvoiceId={InvoiceId} ClientId={ClientId} Monto={Amount} Items={Items}",
                invoice.Id, invoice.ClientId, invoice.Amount, invoice.Items.Count);

            var allowed = transitionService.GetAllowedTransitions(invoice.Status);
            var dto = InvoiceDetailDto.FromEntity(invoice, allowed);
            return Results.Created($"/api/invoices/{invoice.Id}", dto);
        })
        .WithName("CreateInvoice")
        .WithTags("Invoices")
        .WithSummary("Crear factura")
        .WithDescription("Crea una factura con cliente, items y fecha de vencimiento. El monto se deriva de los items.")
        .Produces<InvoiceDetailDto>(StatusCodes.Status201Created)
        .ProducesValidationProblem();
    }
}
