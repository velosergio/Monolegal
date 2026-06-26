using Backend.Application.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Monolegal.Domain.Repositories;

namespace Monolegal.Api.Endpoints.Settings;

/// <summary>
/// GET /api/settings/email — devuelve la configuración NO secreta del email y el estado de la
/// credencial del proveedor activo (spec 017, FR-001/FR-002/FR-008). Nunca incluye secretos.
/// </summary>
public static class GetEmailSettings
{
    public static void MapGetEmailSettings(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/settings/email", async (
            ISystemSettingsRepository repository,
            IEmailCredentialStatus credentialStatus) =>
        {
            var settings = await repository.GetSettingsAsync();
            var email = settings.Email;

            return Results.Ok(new
            {
                activeProvider = email.ActiveProvider,
                fromAddress = email.FromAddress,
                fromName = email.FromName,
                smtp = new
                {
                    host = email.Smtp.Host,
                    port = email.Smtp.Port,
                    username = email.Smtp.Username,
                    useStartTls = email.Smtp.UseStartTls,
                },
                resend = new
                {
                    fromDomain = email.Resend.FromDomain,
                },
                credentialStatus = credentialStatus.GetStatus(email.ActiveProvider),
            });
        })
        .WithName("GetEmailSettings")
        .WithTags("Settings")
        .WithSummary("Obtener la configuración de email")
        .WithDescription("Devuelve la configuración no secreta del proveedor de email y el estado de su credencial. Nunca expone secretos.")
        .Produces(StatusCodes.Status200OK);
    }
}
