using Backend.Application.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Monolegal.Api.Endpoints.Settings;

/// <summary>
/// POST /api/settings/maintenance/flush-database — vacía por completo la base de datos
/// (incluida la configuración del sistema), reconstruye los índices y vuelve a ejecutar el
/// sembrador de datos. Operación irreversible de la "zona de peligro"; el frontend exige
/// confirmación explícita antes de invocar.
/// </summary>
public static class FlushDatabase
{
    public static void MapFlushDatabase(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/settings/maintenance/flush-database", async (
            IMaintenanceService maintenance,
            CancellationToken cancellationToken) =>
        {
            var result = await maintenance.FlushDatabaseAsync(cancellationToken);
            return Results.Ok(new
            {
                deletedInvoices = result.DeletedInvoices,
                seeded = result.Seeded,
                clientsCreated = result.ClientsCreated,
                invoicesCreated = result.InvoicesCreated,
            });
        })
        .WithName("FlushDatabase")
        .WithTags("Settings")
        .WithSummary("Flush DB")
        .WithDescription("Vacía toda la base de datos (incluida la configuración), reconstruye índices y ejecuta nuevamente el sembrador de datos. Irreversible.")
        .Produces(StatusCodes.Status200OK);
    }
}
