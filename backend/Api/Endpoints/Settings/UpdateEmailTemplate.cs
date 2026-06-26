using System.Linq;
using Backend.Application.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Monolegal.Domain.Repositories;

namespace Monolegal.Api.Endpoints.Settings;

/// <summary>
/// PUT /api/settings/email/templates/{type} — actualiza una plantilla (spec 017, FR-011/FR-013).
/// 404 si el tipo es desconocido; 400 si el asunto/cuerpo es inválido o usa variables no admitidas.
/// </summary>
public static class UpdateEmailTemplate
{
    public static void MapUpdateEmailTemplate(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/settings/email/templates/{type}", async (
            string type,
            [FromBody] EmailTemplateInput request,
            ISystemSettingsRepository repository,
            CancellationToken cancellationToken) =>
        {
            if (!NotificationTypeApi.TryParse(type, out var notificationType))
                return Results.NotFound();

            var validator = new UpdateEmailTemplateValidator();
            var validation = await validator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                var first = validation.Errors.Select(e => e.ErrorMessage).FirstOrDefault();
                return Results.BadRequest(new { error = first ?? "Plantilla inválida." });
            }

            var settings = await repository.GetSettingsAsync();
            settings.UpdateTemplate(notificationType, request.Subject!, request.Body!);
            await repository.UpdateSettingsAsync(settings);

            return Results.NoContent();
        })
        .WithName("UpdateEmailTemplate")
        .WithTags("Settings")
        .WithSummary("Actualizar una plantilla de email")
        .WithDescription("Reemplaza el asunto y cuerpo de la plantilla del tipo indicado. Devuelve 204.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);
    }
}
