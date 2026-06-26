using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Monolegal.Domain.Repositories;

namespace Monolegal.Api.Endpoints.Settings;

/// <summary>
/// POST /api/settings/email/templates/{type}/reset — restablece una plantilla a su contenido por
/// defecto eliminando la personalización (spec 017, FR-014). 404 si el tipo es desconocido.
/// </summary>
public static class ResetEmailTemplate
{
    public static void MapResetEmailTemplate(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/settings/email/templates/{type}/reset", async (
            string type,
            ISystemSettingsRepository repository) =>
        {
            if (!NotificationTypeApi.TryParse(type, out var notificationType))
                return Results.NotFound();

            var settings = await repository.GetSettingsAsync();
            settings.ResetTemplate(notificationType);
            await repository.UpdateSettingsAsync(settings);

            return Results.NoContent();
        })
        .WithName("ResetEmailTemplate")
        .WithTags("Settings")
        .WithSummary("Restablecer una plantilla de email")
        .WithDescription("Elimina la personalización de la plantilla del tipo indicado, volviendo al default. Devuelve 204.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);
    }
}
