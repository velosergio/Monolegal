using System;
using System.Linq;
using Backend.Application.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Monolegal.Domain.Repositories;
using Monolegal.Domain.Services;

namespace Monolegal.Api.Endpoints.Invoices;

/// <summary>
/// Endpoint POST /api/invoices/transition/{id} — transición manual del estado de una factura.
/// Reglas: id inexistente/ inválido → 404; newStatus ausente o inválido → 400; transición no
/// permitida por la matriz de dominio → 400 (sin cambios); transición válida → 200 con la factura.
///
/// Validates: FR-010..FR-014, FR-017, FR-018 | US3 (spec.md 009-invoice-api-endpoints)
/// </summary>
public static class TransitionInvoice
{
    public static void MapTransitionInvoice(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/invoices/transition/{id}", async (
            string id,
            [FromBody] TransitionRequest request,
            IInvoiceRepository invoiceRepository,
            InvoiceTransitionService transitionService,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(TransitionInvoice));

            // 1. Validar el cuerpo (newStatus requerido y válido) → 400.
            var validator = new TransitionInvoiceRequestValidator(InvoiceStatusApi.IsValid);
            var input = new TransitionInvoiceInput(request?.NewStatus);
            var validation = await validator.ValidateAsync(input, cancellationToken);
            if (!validation.IsValid)
            {
                logger.LogWarning(
                    "Transición rechazada — cuerpo inválido. InvoiceId={InvoiceId} NewStatus={NewStatus}",
                    id, request?.NewStatus);
                return Results.ValidationProblem(
                    validation.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
            }

            InvoiceStatusApi.TryParse(input.NewStatus, out var newStatus);

            // 2. Buscar la factura → 404 (incluye id con formato inválido, Q4).
            var invoice = await invoiceRepository.GetByIdAsync(id, cancellationToken);
            if (invoice is null)
            {
                logger.LogWarning("Transición rechazada — factura no encontrada. InvoiceId={InvoiceId}", id);
                return Results.NotFound();
            }

            // 3. Aplicar la transición validándola contra la matriz de dominio → 400 si no permitida.
            try
            {
                transitionService.ApplyManualTransition(invoice, newStatus);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(
                    "Transición rechazada — no permitida. InvoiceId={InvoiceId} From={From} To={To} Error={Error}",
                    id, invoice.Status, newStatus, ex.Message);
                return Results.BadRequest(new { error = ex.Message });
            }

            // 4. Persistir y devolver la factura actualizada.
            await invoiceRepository.UpdateAsync(invoice, cancellationToken);

            logger.LogInformation(
                "Transición aplicada. InvoiceId={InvoiceId} NuevoEstado={NewStatus}",
                invoice.Id, invoice.Status);

            return Results.Ok(InvoiceDetailDto.FromEntity(invoice));
        })
        .WithName("TransitionInvoice")
        .WithTags("Invoices")
        .WithSummary("Transicionar el estado de una factura")
        .WithDescription(
            "Aplica una transición manual de estado a la factura indicada. El cuerpo debe incluir " +
            "'newStatus'. Devuelve 400 si el cuerpo es inválido o la transición no está permitida, " +
            "y 404 si la factura no existe.")
        .Produces<InvoiceDetailDto>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);
    }
}
