using System.Linq;
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
/// Pruebas de integración HTTP de los endpoints de plantillas de email (spec 017, US2): listado,
/// actualización con validación de variables, restablecer y vista previa. Sin MongoDB.
/// </summary>
[Trait("Category", "Application")]
public sealed class EmailTemplatesEndpointsTests
{
    private sealed class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("MONGODB_URI", "mongodb://localhost:27017/email_templates_tests");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IHostedService>();
                services.RemoveAll<ISystemSettingsRepository>();
                services.AddSingleton<ISystemSettingsRepository, InMemorySystemSettingsRepository>();
            });
        }
    }

    [Fact]
    public async Task Get_DevuelvePlantillasYVariablesAdmitidas()
    {
        using var factory = new Factory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/settings/email/templates");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("allowedVariables").GetArrayLength().ShouldBe(9);
        doc.RootElement.GetProperty("templates").GetArrayLength().ShouldBe(3);
    }

    [Fact]
    public async Task Put_Valido_PersisteYMarcaCustomized()
    {
        using var factory = new Factory();
        var client = factory.CreateClient();

        var put = await client.PutAsJsonAsync("/api/settings/email/templates/reminder", new
        {
            subject = "Recordatorio {{factura.id}}",
            body = "Hola {{cliente.nombre}}, su factura {{factura.id}}.",
        });
        put.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var get = await client.GetAsync("/api/settings/email/templates");
        using var doc = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        var reminder = doc.RootElement.GetProperty("templates").EnumerateArray()
            .First(t => t.GetProperty("type").GetString() == "reminder");
        reminder.GetProperty("isCustomized").GetBoolean().ShouldBeTrue();
        reminder.GetProperty("subject").GetString().ShouldBe("Recordatorio {{factura.id}}");
    }

    [Fact]
    public async Task Put_VariableNoAdmitida_Devuelve400()
    {
        using var factory = new Factory();
        var client = factory.CreateClient();

        var put = await client.PutAsJsonAsync("/api/settings/email/templates/reminder", new
        {
            subject = "Asunto {{factura.xyz}}",
            body = "Cuerpo {{factura.id}}",
        });
        put.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Put_TipoDesconocido_Devuelve404()
    {
        using var factory = new Factory();
        var client = factory.CreateClient();

        var put = await client.PutAsJsonAsync("/api/settings/email/templates/desconocido", new
        {
            subject = "Asunto",
            body = "Cuerpo",
        });
        put.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Reset_EliminaPersonalizacion()
    {
        using var factory = new Factory();
        var client = factory.CreateClient();

        await client.PutAsJsonAsync("/api/settings/email/templates/reminder", new
        {
            subject = "Custom {{factura.id}}",
            body = "Custom {{factura.id}}",
        });

        var reset = await client.PostAsync("/api/settings/email/templates/reminder/reset", content: null);
        reset.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var get = await client.GetAsync("/api/settings/email/templates");
        using var doc = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        var reminder = doc.RootElement.GetProperty("templates").EnumerateArray()
            .First(t => t.GetProperty("type").GetString() == "reminder");
        reminder.GetProperty("isCustomized").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task Preview_RenderizaConDatosDeEjemplo()
    {
        using var factory = new Factory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/settings/email/templates/reminder/preview", new
        {
            subject = "Factura {{factura.id}}",
            body = "Hola {{cliente.nombre}}",
        });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("subject").GetString()!.ShouldContain("F-2026-000123");
        doc.RootElement.GetProperty("body").GetString()!.ShouldContain("Cliente de ejemplo");
    }
}
