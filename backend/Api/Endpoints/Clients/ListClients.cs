using System.Linq;
using Backend.Application.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Monolegal.Api.Endpoints.Invoices;
using Monolegal.Domain.Repositories;

namespace Monolegal.Api.Endpoints.Clients;

/// <summary>
/// Endpoint GET /api/clients — listado paginado de clientes con búsqueda opcional por nombre/email
/// (spec 018, RF-012/RF-013).
/// </summary>
public static class ListClients
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 10;

    public static void MapListClients(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/clients", async (
            string? search,
            int? page,
            int? pageSize,
            IClientRepository clientRepository,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(ListClients));

            var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
            var query = new ListClientsQuery(page ?? DefaultPage, pageSize ?? DefaultPageSize, normalizedSearch);

            var validation = await new ListClientsQueryValidator().ValidateAsync(query, cancellationToken);
            if (!validation.IsValid)
            {
                logger.LogWarning("Listado de clientes rechazado — parámetros inválidos. Page={Page} PageSize={PageSize}",
                    query.Page, query.PageSize);
                return Results.ValidationProblem(validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
            }

            var (items, total) = await clientRepository.GetPagedAsync(
                query.Search, query.Page, query.PageSize, cancellationToken);

            var data = items.Select(ClientDto.FromEntity).ToList();

            logger.LogInformation(
                "Listado de clientes. Search={Search} Page={Page} PageSize={PageSize} Total={Total} Returned={Returned}",
                query.Search ?? "(sin búsqueda)", query.Page, query.PageSize, total, data.Count);

            return Results.Ok(new PagedResponse<ClientDto>(data, total, query.PageSize));
        })
        .WithName("ListClients")
        .WithTags("Clients")
        .WithSummary("Listar clientes")
        .WithDescription("Lista paginada de clientes, con búsqueda opcional (query 'search') por nombre o email.")
        .Produces<PagedResponse<ClientDto>>(StatusCodes.Status200OK)
        .ProducesValidationProblem();
    }
}
