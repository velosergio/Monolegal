using System;
using System.Diagnostics;
using System.Linq;
using Backend.Application.Abstractions;
using Backend.Application.Validation;
using Backend.Infrastructure.Email;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Monolegal.Domain.Repositories;

namespace Monolegal.Api.Endpoints.Settings;

/// <summary>
/// POST /api/settings/email/test — envía un correo de prueba a la dirección indicada usando el
/// proveedor activo y la plantilla real (spec 017, FR-016..FR-019). Un fallo de envío NO es un 5xx:
/// se reporta como <c>result: "failed"</c> con motivo legible para el toast. Nunca expone secretos.
/// </summary>
public static class SendTestEmail
{
    public static void MapSendTestEmail(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/settings/email/test", async (
            [FromBody] SendTestEmailInput request,
            ISystemSettingsRepository repository,
            EmailTemplateProvider templates,
            IEmailProviderFactory factory,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(SendTestEmail));

            var validator = new SendTestEmailValidator();
            var validation = await validator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(
                    validation.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
            }

            NotificationTypeApi.TryParse(request.TemplateType, out var type);

            var settings = await repository.GetSettingsAsync();
            var effective = templates.GetEffective(type, settings.EmailTemplates);
            var (subject, body) = templates.RenderRaw(
                effective.Subject, effective.Body, EmailTemplateProvider.SampleVariables());

            var provider = settings.Email.ActiveProvider;
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await factory.Resolve(provider)
                    .SendAsync(new EmailMessage(request.To!, subject, body), cancellationToken);
                stopwatch.Stop();

                logger.LogInformation(
                    "Correo de prueba enviado. Provider={Provider} TemplateType={TemplateType} Result=sent DurationMs={DurationMs}",
                    provider, type, stopwatch.ElapsedMilliseconds);

                return Results.Ok(new { to = request.To, result = "sent", message = (string?)null });
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                logger.LogWarning(
                    ex,
                    "Fallo al enviar correo de prueba. Provider={Provider} TemplateType={TemplateType} Result=failed DurationMs={DurationMs}",
                    provider, type, stopwatch.ElapsedMilliseconds);

                return Results.Ok(new { to = request.To, result = "failed", message = ex.Message });
            }
        })
        .WithName("SendTestEmail")
        .WithTags("Settings")
        .WithSummary("Enviar un correo de prueba")
        .WithDescription("Envía un correo de prueba con el proveedor y la plantilla reales. Un fallo de envío se reporta como resultado, no como error 5xx.")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem();
    }
}
