using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Monolegal.Domain.Repositories;

namespace Monolegal.Api.Endpoints.Settings;

public static class GetInvoiceTransitions
{
    public static void MapGetInvoiceTransitions(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/settings/invoice-transitions", async (ISystemSettingsRepository repository) =>
        {
            var settings = await repository.GetSettingsAsync();
            return Results.Ok(settings.InvoiceTransitions);
        })
        .WithName("GetInvoiceTransitions")
        .WithTags("Settings");
    }
}
