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
/// Pruebas de integración HTTP de los endpoints de configuración de email (spec 017, US1).
/// Arrancan el host real sin MongoDB (repositorio en memoria, sin hosted services) y verifican
/// GET/PUT/validate, incluida la garantía de que la respuesta NUNCA expone secretos (SC-007).
/// </summary>
[Trait("Category", "Application")]
public sealed class EmailSettingsEndpointsTests
{
    /// <summary>Host de pruebas aislado: cada test crea el suyo para no compartir estado.</summary>
    private sealed class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("MONGODB_URI", "mongodb://localhost:27017/email_settings_tests");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IHostedService>();
                services.RemoveAll<ISystemSettingsRepository>();
                services.AddSingleton<ISystemSettingsRepository, InMemorySystemSettingsRepository>();
            });
        }
    }

    // El contrato no debe exponer estas claves de secreto en ninguna respuesta (SC-007, FR-008).
    private static void ShouldNotLeakSecrets(string json)
    {
        var lower = json.ToLowerInvariant();
        lower.ShouldNotContain("\"password\"");
        lower.ShouldNotContain("\"apikey\"");
    }

    [Fact]
    public async Task Get_DevuelveConfiguracionYEstado_SinExponerSecretos()
    {
        using var factory = new Factory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/settings/email");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        ShouldNotLeakSecrets(json);

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("activeProvider").GetString().ShouldBe("smtp");
        doc.RootElement.GetProperty("credentialStatus").GetString().ShouldBe("notconfigured");
    }

    [Fact]
    public async Task Put_Valido_PersisteYDevuelve204()
    {
        using var factory = new Factory();
        var client = factory.CreateClient();

        var body = new
        {
            activeProvider = "resend",
            fromAddress = "facturas@monolegal.co",
            fromName = "Monolegal Facturación",
            smtp = new { host = "smtp.example.com", port = 587, username = "smtp-user", useStartTls = true },
            resend = new { fromDomain = "mg.monolegal.co" },
        };

        var put = await client.PutAsJsonAsync("/api/settings/email", body);
        put.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var get = await client.GetAsync("/api/settings/email");
        using var doc = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("activeProvider").GetString().ShouldBe("resend");
        doc.RootElement.GetProperty("fromAddress").GetString().ShouldBe("facturas@monolegal.co");
        doc.RootElement.GetProperty("resend").GetProperty("fromDomain").GetString().ShouldBe("mg.monolegal.co");
    }

    [Fact]
    public async Task Put_Invalido_Devuelve400()
    {
        using var factory = new Factory();
        var client = factory.CreateClient();

        var body = new
        {
            activeProvider = "smtp",
            fromAddress = "no-es-correo",
            fromName = "",
            smtp = new { host = (string?)null, port = 587 },
            resend = new { fromDomain = (string?)null },
        };

        var put = await client.PutAsJsonAsync("/api/settings/email", body);
        put.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Validate_SinCredencial_DevuelveNotConfigured()
    {
        using var factory = new Factory();
        var client = factory.CreateClient();

        var response = await client.PostAsync("/api/settings/email/validate", content: null);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        ShouldNotLeakSecrets(json);

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("status").GetString().ShouldBe("notconfigured");
    }
}
