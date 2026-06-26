using System;
using System.Linq;
using Backend.Infrastructure.Email;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Monolegal.Domain.Email;
using Monolegal.Domain.Enums;
using Monolegal.Domain.Repositories;

namespace Monolegal.Api.Endpoints.Settings;

/// <summary>
/// GET /api/settings/email/templates — lista las plantillas efectivas (personalizada o default)
/// por tipo, junto con el catálogo de variables admitidas (spec 017, FR-009/FR-010).
/// </summary>
public static class GetEmailTemplates
{
    public static void MapGetEmailTemplates(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/settings/email/templates", async (
            ISystemSettingsRepository repository,
            EmailTemplateProvider templates) =>
        {
            var settings = await repository.GetSettingsAsync();

            var list = Enum.GetValues<NotificationType>().Select(type =>
            {
                var effective = templates.GetEffective(type, settings.EmailTemplates);
                var isCustomized = settings.EmailTemplates.ContainsKey(type);
                return new
                {
                    type,
                    subject = effective.Subject,
                    body = effective.Body,
                    isCustomized,
                };
            });

            return Results.Ok(new
            {
                allowedVariables = EmailTemplateVariables.All,
                templates = list,
            });
        })
        .WithName("GetEmailTemplates")
        .WithTags("Settings")
        .WithSummary("Listar las plantillas de email")
        .WithDescription("Devuelve las plantillas efectivas por tipo y el catálogo de variables admitidas.")
        .Produces(StatusCodes.Status200OK);
    }
}
