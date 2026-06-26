using System.Linq;
using Backend.Application.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Repositories;

namespace Monolegal.Api.Endpoints.Settings;

/// <summary>
/// PUT /api/settings/email — actualiza la configuración NO secreta del email (spec 017,
/// FR-003/FR-005/FR-006). Valida con <see cref="UpdateEmailSettingsValidator"/> y persiste,
/// preservando el resto del agregado. El cambio aplica en runtime (FR-002b). Devuelve 204.
/// </summary>
public static class UpdateEmailSettings
{
    public static void MapUpdateEmailSettings(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/settings/email", async (
            [FromBody] EmailSettingsRequest request,
            ISystemSettingsRepository repository,
            CancellationToken cancellationToken) =>
        {
            var validator = new UpdateEmailSettingsValidator();
            var validation = await validator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(
                    validation.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
            }

            var settings = await repository.GetSettingsAsync();
            settings.UpdateEmailSettings(new EmailSettings
            {
                ActiveProvider = request.ActiveProvider,
                FromAddress = request.FromAddress ?? string.Empty,
                FromName = request.FromName ?? string.Empty,
                Smtp = new SmtpSettings
                {
                    Host = request.Smtp?.Host,
                    Port = request.Smtp?.Port ?? 587,
                    Username = request.Smtp?.Username,
                    UseStartTls = request.Smtp?.UseStartTls ?? true,
                },
                Resend = new ResendSettings
                {
                    FromDomain = request.Resend?.FromDomain,
                },
            });

            await repository.UpdateSettingsAsync(settings);
            return Results.NoContent();
        })
        .WithName("UpdateEmailSettings")
        .WithTags("Settings")
        .WithSummary("Actualizar la configuración de email")
        .WithDescription("Reemplaza la configuración no secreta del proveedor de email. Devuelve 204. Los secretos siguen viniendo del entorno.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem();
    }
}
