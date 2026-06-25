using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Backend.Tests.Monolegal.Application.Tests.Documentation;

/// <summary>
/// Fábrica de host para las pruebas de integración de la documentación (spec 010, T007).
///
/// Fuerza el entorno indicado (por defecto Development, donde se exponen /openapi/v1.json
/// y /swagger) y evita toda dependencia de red: la generación del documento OpenAPI y la
/// UI Swagger no requieren MongoDB. Para ello:
///   1. Define una cadena MONGODB_URI ficticia, ya que BuildMongoOptions exige su presencia.
///   2. Elimina los hosted services de infraestructura (verificador de conexión, worker de
///      transiciones y seeder de desarrollo) para no intentar conectar ni introducir latencia.
/// </summary>
public sealed class DocumentationTestFactory : WebApplicationFactory<Program>
{
    private readonly string _environment;

    public DocumentationTestFactory(string environment = "Development")
    {
        _environment = environment;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environment);
        builder.UseSetting("MONGODB_URI", "mongodb://localhost:27017/monolegal_docs_tests");

        builder.ConfigureServices(services =>
        {
            // La documentación no accede a la base de datos; quitamos los background services
            // para que el host de pruebas arranque sin red y sin esperas de reintento.
            services.RemoveAll<IHostedService>();
        });
    }
}
