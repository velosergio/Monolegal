using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Repositories;

namespace Monolegal.Api.Endpoints.Settings;

public static class UpdateInvoiceTransitions
{
    public static void MapUpdateInvoiceTransitions(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/settings/invoice-transitions", async (
            [FromBody] InvoiceTransitionsConfig request,
            ISystemSettingsRepository repository) =>
        {
            var settings = await repository.GetSettingsAsync();
            settings.UpdateTransitions(request);
            await repository.UpdateSettingsAsync(settings);
            
            return Results.NoContent();
        })
        .WithName("UpdateInvoiceTransitions")
        .WithTags("Settings")
        .WithSummary("Actualizar la configuración de transiciones")
        .WithDescription("Reemplaza la configuración de transiciones automáticas de facturas. Devuelve 204 sin contenido.")
        .Produces(StatusCodes.Status204NoContent);
    }
}
