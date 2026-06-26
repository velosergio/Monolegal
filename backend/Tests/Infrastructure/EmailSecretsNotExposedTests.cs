using System.Net.Http.Json;
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
/// Test de seguridad (spec 017, SC-007, FR-008): con secretos configurados por entorno
/// (contraseña SMTP y API key de Resend), ninguna respuesta de los endpoints de email debe
/// contener el valor del secreto.
/// </summary>
[Trait("Category", "Application")]
public sealed class EmailSecretsNotExposedTests
{
    private const string SmtpPasswordSecret = "S3cr3t-SMTP-PASSWORD-xyz";
    private const string ResendApiKeySecret = "re_SECRET_APIKEY_abc123";

    private sealed class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("MONGODB_URI", "mongodb://localhost:27017/email_secrets_tests");
            // Secretos configurados como si vinieran del entorno.
            builder.UseSetting("Email:Host", "smtp.example.com");
            builder.UseSetting("Email:Password", SmtpPasswordSecret);
            builder.UseSetting("Email:Resend:ApiKey", ResendApiKeySecret);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IHostedService>();
                services.RemoveAll<ISystemSettingsRepository>();
                services.AddSingleton<ISystemSettingsRepository, InMemorySystemSettingsRepository>();
            });
        }
    }

    private static void ShouldNotContainSecrets(string json)
    {
        json.ShouldNotContain(SmtpPasswordSecret);
        json.ShouldNotContain(ResendApiKeySecret);
    }

    [Fact]
    public async Task Get_NoExponeSecretos()
    {
        using var factory = new Factory();
        var client = factory.CreateClient();

        var json = await (await client.GetAsync("/api/settings/email")).Content.ReadAsStringAsync();
        ShouldNotContainSecrets(json);
    }

    [Fact]
    public async Task Validate_NoExponeSecretos()
    {
        using var factory = new Factory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/settings/email/validate", new { provider = "resend" });
        var json = await response.Content.ReadAsStringAsync();
        ShouldNotContainSecrets(json);
    }

    [Fact]
    public async Task Test_NoExponeSecretos()
    {
        using var factory = new Factory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/settings/email/test", new
        {
            to = "prueba@dominio.com",
            templateType = "reminder",
        });
        var json = await response.Content.ReadAsStringAsync();
        ShouldNotContainSecrets(json);
    }
}
