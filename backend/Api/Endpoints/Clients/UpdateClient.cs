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

namespace Monolegal.Api.Endpoints.Clients;

/// <summary>
/// Endpoint PUT /api/clients/{id} — edita un cliente (spec 018, RF-016). 404 si no existe; valida
/// cuerpo y unicidad de email excluyendo al propio cliente.
/// </summary>
public static class UpdateClient
{
    public static void MapUpdateClient(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/clients/{id}", async (
            string id,
            [FromBody] UpdateClientRequest request,
            IClientRepository clientRepository,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(UpdateClient));

            var input = new ClientInput(request?.Name, request?.Email, request?.Phone, request?.Address);
            var validation = await new UpdateClientValidator().ValidateAsync(input, cancellationToken);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
            }

            var client = await clientRepository.GetByIdAsync(id, cancellationToken);
            if (client is null)
            {
                logger.LogWarning("Edición de cliente rechazada — no encontrado. ClientId={ClientId}", id);
                return Results.NotFound();
            }

            // Unicidad de email excluyendo al propio cliente (RF-015a).
            var byEmail = await clientRepository.GetByEmailAsync(request!.Email!, cancellationToken);
            if (byEmail is not null && byEmail.Id != id)
            {
                logger.LogWarning("Edición de cliente rechazada — email duplicado. Email={Email}", request.Email);
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["email"] = new[] { "Ya existe otro cliente con ese email." },
                });
            }

            client.Update(request.Name!, request.Email!, request.Phone, request.Address);
            await clientRepository.UpdateAsync(client, cancellationToken);

            logger.LogInformation("Cliente editado. ClientId={ClientId} Email={Email}", client.Id, client.Email);
            return Results.Ok(ClientDto.FromEntity(client));
        })
        .WithName("UpdateClient")
        .WithTags("Clients")
        .WithSummary("Editar cliente")
        .Produces<ClientDto>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status404NotFound);
    }
}
