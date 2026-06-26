using System.Diagnostics;
using Backend.Application.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Monolegal.Domain.Enums;
using Monolegal.Domain.Repositories;

namespace Monolegal.Api.Endpoints.Settings;

/// <summary>Cuerpo opcional de la validación: permite validar un proveedor específico.</summary>
public sealed record ValidateEmailRequest(EmailProvider? Provider);

/// <summary>
/// POST /api/settings/email/validate — valida la credencial del proveedor (activo o indicado)
/// SIN enviar correo (spec 017, FR-004, D4). Devuelve siempre 200 con el resultado; nunca expone
/// el secreto. Registra un log estructurado con la duración y el resultado.
/// </summary>
public static class ValidateEmailCredentials
{
    public static void MapValidateEmailCredentials(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/settings/email/validate", async (
            [FromBody] ValidateEmailRequest? request,
            ISystemSettingsRepository repository,
            IEmailProviderFactory factory,
            IEmailCredentialStatus credentialStatus,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(ValidateEmailCredentials));

            EmailProvider provider;
            if (request?.Provider is { } requested)
            {
                provider = requested;
            }
            else
            {
                var settings = await repository.GetSettingsAsync();
                provider = settings.Email.ActiveProvider;
            }

            var stopwatch = Stopwatch.StartNew();
            var result = await factory.Resolve(provider).ValidateAsync(cancellationToken);
            stopwatch.Stop();

            credentialStatus.MarkValidation(provider, result.State == EmailCredentialState.Validated);

            logger.LogInformation(
                "Validación de credencial de email. Provider={Provider} Result={Result} DurationMs={DurationMs}",
                provider, result.State, stopwatch.ElapsedMilliseconds);

            return Results.Ok(new
            {
                provider,
                status = result.State,
                message = result.Message,
            });
        })
        .WithName("ValidateEmailCredentials")
        .WithTags("Settings")
        .WithSummary("Validar la credencial del proveedor de email")
        .WithDescription("Valida la credencial del proveedor activo (o el indicado) sin enviar correo. Nunca expone el secreto.")
        .Produces(StatusCodes.Status200OK);
    }
}
