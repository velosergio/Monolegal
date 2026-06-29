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
/// Endpoint GET /api/invoices/shipments — listado paginado de envíos por factura (spec 019, US1).
/// Sólo facturas en estados notificables; filtro por <c>sendStatus</c> y búsqueda por nombre/correo
/// de cliente (resuelta en dos pasos vía <see cref="IClientRepository"/>).
///
/// Validates: FR-001..FR-009 | US1 (spec.md 019-vista-envios)
/// </summary>
public static class ListShipments
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 10;

    /// <summary>Tope de clientes coincidentes con la búsqueda para acotar la consulta (sin queries sin límite).</summary>
    private const int MaxClientMatches = 500;

    public static void MapListShipments(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/invoices/shipments", async (
            string? sendStatus,
            string? search,
            int? page,
            int? pageSize,
            IInvoiceRepository invoiceRepository,
            IClientRepository clientRepository,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(ListShipments));

            var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

            var query = new ShipmentsQuery(
                sendStatus,
                page ?? DefaultPage,
                pageSize ?? DefaultPageSize,
                normalizedSearch);

            var validator = new ShipmentsQueryValidator(ShipmentListItemDto.IsValidSendStatusFilter);
            var validation = await validator.ValidateAsync(query, cancellationToken);
            if (!validation.IsValid)
            {
                logger.LogWarning(
                    "Listado de envíos rechazado — parámetros inválidos. SendStatus={SendStatus} Page={Page} PageSize={PageSize}",
                    sendStatus, query.Page, query.PageSize);
                return Results.ValidationProblem(
                    validation.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
            }

            var outcomeFilter = ShipmentListItemDto.ParseSendStatus(query.SendStatus);

            // Paso 1 (búsqueda): resolver los clientId cuyo nombre o correo coinciden con el término.
            // null = sin filtro por cliente; lista vacía = ningún cliente coincide (resultado vacío).
            IReadOnlyCollection<string>? clientIds = null;
            if (query.Search is not null)
            {
                var (matches, _) = await clientRepository
                    .GetPagedAsync(query.Search, 1, MaxClientMatches, cancellationToken)
                    .ConfigureAwait(false);
                clientIds = matches.Select(c => c.Id).ToList();
            }

            var (items, total) = await invoiceRepository.GetShipmentsPagedAsync(
                outcomeFilter, clientIds, query.Page, query.PageSize, cancellationToken);

            // Resolución de nombre/correo por clientId distinto de la página (anti N+1, como ListInvoices).
            var clientNames = new Dictionary<string, string>();
            var clientEmails = new Dictionary<string, string?>();
            foreach (var clientId in items.Select(i => i.ClientId).Distinct())
            {
                var client = await clientRepository.GetByIdAsync(clientId, cancellationToken);
                clientNames[clientId] = client?.Name ?? clientId;
                clientEmails[clientId] = client?.Email;
            }

            var data = items
                .Select(i => ShipmentListItemDto.FromEntity(
                    i,
                    clientNames.TryGetValue(i.ClientId, out var name) ? name : i.ClientId,
                    clientEmails.TryGetValue(i.ClientId, out var email) ? email : null))
                .ToList();

            logger.LogInformation(
                "Listado de envíos. SendStatus={SendStatus} Search={Search} Page={Page} PageSize={PageSize} Total={Total} Returned={Returned}",
                query.SendStatus ?? "(todos)", query.Search ?? "(sin búsqueda)", query.Page, query.PageSize, total, data.Count);

            return Results.Ok(new PagedResponse<ShipmentListItemDto>(data, total, query.PageSize));
        })
        .WithName("ListShipments")
        .WithTags("Invoices")
        .WithSummary("Listar envíos")
        .WithDescription(
            "Devuelve una lista paginada de envíos por factura (sólo facturas en estados notificables), " +
            "opcionalmente filtrada por estado de envío (query 'sendStatus': pending/sent/failed/skipped) " +
            "y por cliente o correo (query 'search', coincidencia case-insensitive, máximo 100 caracteres). " +
            "Admite 'page' y 'pageSize' (máximo 50).")
        .Produces<PagedResponse<ShipmentListItemDto>>(StatusCodes.Status200OK)
        .ProducesValidationProblem();
    }
}
