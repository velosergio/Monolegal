using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Monolegal.Domain.Repositories;

namespace Monolegal.Api.Endpoints.Clients;

/// <summary>
/// Endpoint DELETE /api/clients/{id} — elimina un cliente (spec 018, RF-017). Impide la eliminación
/// si el cliente tiene facturas asociadas (RF-018 → 409). 204 si se eliminó, 404 si no existía.
/// </summary>
public static class DeleteClient
{
    public static void MapDeleteClient(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/clients/{id}", async (
            string id,
            IClientRepository clientRepository,
            IInvoiceRepository invoiceRepository,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(DeleteClient));

            var client = await clientRepository.GetByIdAsync(id, cancellationToken);
            if (client is null)
            {
                logger.LogWarning("Eliminación de cliente — no encontrado. ClientId={ClientId}", id);
                return Results.NotFound();
            }

            // Guard de integridad referencial (RF-018): no se elimina si tiene facturas asociadas.
            var associated = await invoiceRepository.CountByClientIdAsync(id, cancellationToken);
            if (associated > 0)
            {
                logger.LogWarning(
                    "Eliminación de cliente rechazada — facturas asociadas. ClientId={ClientId} Facturas={Count}",
                    id, associated);
                return Results.Conflict(new { error = "No se puede eliminar el cliente: tiene facturas asociadas." });
            }

            await clientRepository.DeleteAsync(id, cancellationToken);
            logger.LogInformation("Cliente eliminado. ClientId={ClientId}", id);
            return Results.NoContent();
        })
        .WithName("DeleteClient")
        .WithTags("Clients")
        .WithSummary("Eliminar cliente")
        .WithDescription("Elimina un cliente sin facturas asociadas. Devuelve 409 si tiene facturas.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);
    }
}
