using Backend.Application.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Monolegal.Api.Endpoints.Settings;

/// <summary>
/// POST /api/settings/email/tools/resend-failed — reenvía las notificaciones de todas las
/// facturas con <c>LastNotificationOutcome == Failed</c> (spec 017, FR-020/FR-022/FR-023).
/// Devuelve los conteos del lote; <c>attempted == 0</c> no es error.
/// </summary>
public static class ResendFailedNotifications
{
    public static void MapResendFailedNotifications(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/settings/email/tools/resend-failed", async (
            IEmailAdminService adminService,
            CancellationToken cancellationToken) =>
        {
            var result = await adminService.ResendFailedAsync(cancellationToken);
            return Results.Ok(new
            {
                attempted = result.Attempted,
                resent = result.Resent,
                failed = result.Failed,
            });
        })
        .WithName("ResendFailedNotifications")
        .WithTags("Settings")
        .WithSummary("Reenviar notificaciones fallidas")
        .WithDescription("Reintenta el envío de las notificaciones de todas las facturas con último resultado Failed. Devuelve conteos (attempted/resent/failed).")
        .Produces(StatusCodes.Status200OK);
    }
}
