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
/// Endpoint POST /api/clients — crea un cliente (spec 018, RF-014). Valida cuerpo y unicidad de
/// email (RF-015a). Devuelve 201.
/// </summary>
public static class CreateClient
{
    public static void MapCreateClient(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/clients", async (
            [FromBody] CreateClientRequest request,
            IClientRepository clientRepository,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(CreateClient));

            var input = new ClientInput(request?.Name, request?.Email, request?.Phone, request?.Address);
            var validation = await new CreateClientValidator().ValidateAsync(input, cancellationToken);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
            }

            // Unicidad de email (RF-015a): la validación de aplicación es la primera defensa; el índice
            // único de Mongo es la red de seguridad ante carreras (research D5).
            var existing = await clientRepository.GetByEmailAsync(request!.Email!, cancellationToken);
            if (existing is not null)
            {
                logger.LogWarning("Alta de cliente rechazada — email duplicado. Email={Email}", request.Email);
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["email"] = new[] { "Ya existe un cliente con ese email." },
                });
            }

            var client = new Client(request.Name!, request.Email!, request.Phone, request.Address);
            await clientRepository.AddAsync(client, cancellationToken);

            logger.LogInformation("Cliente creado. ClientId={ClientId} Email={Email}", client.Id, client.Email);
            return Results.Created($"/api/clients/{client.Id}", ClientDto.FromEntity(client));
        })
        .WithName("CreateClient")
        .WithTags("Clients")
        .WithSummary("Crear cliente")
        .Produces<ClientDto>(StatusCodes.Status201Created)
        .ProducesValidationProblem();
    }
}
