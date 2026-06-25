using System.Collections.Generic;
using System.Linq;
using Backend.Application.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Monolegal.Domain.Repositories;

namespace Monolegal.Api.Endpoints.Invoices;

/// <summary>
/// Endpoint GET /api/invoices — listado paginado de facturas con filtro opcional por estado.
///
/// Validates: FR-001..FR-007, FR-017, FR-018 | US1 (spec.md 009-invoice-api-endpoints)
/// </summary>
public static class ListInvoices
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 10;

    public static void MapListInvoices(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/invoices", async (
            string? status,
            int? page,
            int? pageSize,
            IInvoiceRepository invoiceRepository,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(ListInvoices));

            // Defaults aplicados solo ante ausencia del parámetro (FR-006).
            var query = new ListInvoicesQuery(
                status,
                page ?? DefaultPage,
                pageSize ?? DefaultPageSize);

            // Validación de parámetros (page/pageSize/status) → 400 ante valores inválidos.
            var validator = new ListInvoicesQueryValidator(InvoiceStatusApi.IsValid);
            var validation = await validator.ValidateAsync(query, cancellationToken);
            if (!validation.IsValid)
            {
                logger.LogWarning(
                    "Listado rechazado — parámetros inválidos. Status={Status} Page={Page} PageSize={PageSize}",
                    status, query.Page, query.PageSize);
                return Results.ValidationProblem(
                    validation.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
            }

            // El status ya validado se traduce al enum de dominio (null = sin filtro).
            Monolegal.Domain.Enums.InvoiceStatus? statusFilter = null;
            if (query.Status is not null && InvoiceStatusApi.TryParse(query.Status, out var parsed))
                statusFilter = parsed;

            var (items, total) = await invoiceRepository.GetPagedAsync(
                statusFilter, query.Page, query.PageSize, cancellationToken);

            var data = items.Select(InvoiceListItemDto.FromEntity).ToList();

            logger.LogInformation(
                "Listado de facturas. Status={Status} Page={Page} PageSize={PageSize} Total={Total} Returned={Returned}",
                query.Status ?? "(todos)", query.Page, query.PageSize, total, data.Count);

            return Results.Ok(new PagedResponse<InvoiceListItemDto>(data, total, query.PageSize));
        })
        .WithName("ListInvoices")
        .WithTags("Invoices")
        .WithSummary("Listar facturas")
        .WithDescription(
            "Devuelve una lista paginada de facturas, opcionalmente filtrada por estado " +
            "(query param 'status'). Admite paginación con 'page' y 'pageSize' (máximo 50).")
        .Produces<PagedResponse<InvoiceListItemDto>>(StatusCodes.Status200OK)
        .ProducesValidationProblem();
    }
}
