using System.Linq;
using Backend.Application.Validation;
using Backend.Infrastructure.Email;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Monolegal.Api.Endpoints.Settings;

/// <summary>
/// POST /api/settings/email/templates/{type}/preview — renderiza una plantilla con datos de
/// ejemplo deterministas (spec 017, FR-012), aplicando la misma validación de variables que PUT.
/// </summary>
public static class PreviewEmailTemplate
{
    public static void MapPreviewEmailTemplate(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/settings/email/templates/{type}/preview", async (
            string type,
            [FromBody] EmailTemplateInput request,
            EmailTemplateProvider templates,
            CancellationToken cancellationToken) =>
        {
            if (!NotificationTypeApi.TryParse(type, out _))
                return Results.NotFound();

            var validator = new UpdateEmailTemplateValidator();
            var validation = await validator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                var first = validation.Errors.Select(e => e.ErrorMessage).FirstOrDefault();
                return Results.BadRequest(new { error = first ?? "Plantilla inválida." });
            }

            var (subject, body) = templates.RenderRaw(
                request.Subject!, request.Body!, EmailTemplateProvider.SampleVariables());

            return Results.Ok(new { subject, body });
        })
        .WithName("PreviewEmailTemplate")
        .WithTags("Settings")
        .WithSummary("Previsualizar una plantilla de email")
        .WithDescription("Renderiza la plantilla con datos de ejemplo. Aplica la misma validación de variables que la edición.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);
    }
}
