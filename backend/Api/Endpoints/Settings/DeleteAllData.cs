using Backend.Application.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Monolegal.Api.Endpoints.Settings;

/// <summary>
/// POST /api/settings/maintenance/delete-all-data — elimina todos los registros de negocio
/// (facturas), conservando la base de datos y la configuración. Operación irreversible de la
/// "zona de peligro"; el frontend exige confirmación explícita antes de invocar.
/// </summary>
public static class DeleteAllData
{
    public static void MapDeleteAllData(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/settings/maintenance/delete-all-data", async (
            IMaintenanceService maintenance,
            CancellationToken cancellationToken) =>
        {
            var result = await maintenance.DeleteAllDataAsync(cancellationToken);
            return Results.Ok(new { deletedInvoices = result.DeletedInvoices });
        })
        .WithName("DeleteAllData")
        .WithTags("Settings")
        .WithSummary("Eliminar todos los datos")
        .WithDescription("Elimina todos los registros de negocio (facturas) conservando la base de datos y la configuración del sistema. Irreversible.")
        .Produces(StatusCodes.Status200OK);
    }
}
