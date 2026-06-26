using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Backend.Tests.Infrastructure.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Monolegal.Domain.Repositories;
using Shouldly;
using Xunit;

namespace Backend.Tests.Infrastructure;

/// <summary>
/// Pruebas de integración HTTP del envío de prueba (spec 017, US3). Sin proveedor configurado el
/// envío falla de forma controlada y se reporta como <c>result: "failed"</c> (no 5xx).
/// </summary>
[Trait("Category", "Application")]
public sealed class SendTestEmailEndpointTests
{
    private sealed class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("MONGODB_URI", "mongodb://localhost:27017/email_test_send_tests");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IHostedService>();
                services.RemoveAll<ISystemSettingsRepository>();
                services.AddSingleton<ISystemSettingsRepository, InMemorySystemSettingsRepository>();
            });
        }
    }

    [Fact]
    public async Task Test_SinProveedorConfigurado_DevuelveFailedNo5xx()
    {
        using var factory = new Factory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/settings/email/test", new
        {
            to = "prueba@dominio.com",
            templateType = "reminder",
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("to").GetString().ShouldBe("prueba@dominio.com");
        doc.RootElement.GetProperty("result").GetString().ShouldBe("failed");
    }

    [Fact]
    public async Task Test_CorreoInvalido_Devuelve400()
    {
        using var factory = new Factory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/settings/email/test", new
        {
            to = "no-es-correo",
            templateType = "reminder",
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
