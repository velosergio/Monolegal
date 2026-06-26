using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Monolegal.Domain.Repositories;

namespace Monolegal.Api.Endpoints.Clients;

/// <summary>Endpoint GET /api/clients/{id} — detalle de un cliente (spec 018).</summary>
public static class GetClientById
{
    public static void MapGetClientById(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/clients/{id}", async (
            string id,
            IClientRepository clientRepository,
            CancellationToken cancellationToken) =>
        {
            var client = await clientRepository.GetByIdAsync(id, cancellationToken);
            return client is null ? Results.NotFound() : Results.Ok(ClientDto.FromEntity(client));
        })
        .WithName("GetClientById")
        .WithTags("Clients")
        .WithSummary("Detalle de cliente")
        .Produces<ClientDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);
    }
}
