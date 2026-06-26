using Backend.Application.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Monolegal.Api.Endpoints.Settings;

/// <summary>
/// POST /api/settings/email/tools/sanitize — marca como <c>Failed</c> las facturas en estado
/// notificable con <c>LastNotificationOutcome == None</c> (spec 017, FR-021/FR-022/FR-023).
/// No reintenta ni borra. Devuelve el conteo saneado; <c>sanitized == 0</c> no es error. El
/// frontend exige confirmación explícita antes de invocar.
/// </summary>
public static class SanitizeStuckNotifications
{
    public static void MapSanitizeStuckNotifications(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/settings/email/tools/sanitize", async (
            IEmailAdminService adminService,
            CancellationToken cancellationToken) =>
        {
            var result = await adminService.SanitizeStuckAsync(cancellationToken);
            return Results.Ok(new { sanitized = result.Sanitized });
        })
        .WithName("SanitizeStuckNotifications")
        .WithTags("Settings")
        .WithSummary("Sanear notificaciones atascadas")
        .WithDescription("Marca como Failed las facturas en estado notificable con notificación no registrada (None), conservando el registro. No reintenta ni borra.")
        .Produces(StatusCodes.Status200OK);
    }
}
