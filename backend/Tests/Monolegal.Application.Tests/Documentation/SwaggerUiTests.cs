using System.Net;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Backend.Tests.Monolegal.Application.Tests.Documentation;

/// <summary>
/// Pruebas de la interfaz Swagger UI (spec 010, US1, FR-001).
/// Verifican que /swagger se sirve en Development (contracts/swagger-ui.md).
/// </summary>
[Trait("Category", "Documentation")]
public class SwaggerUiTests
{
    [Fact]
    public async Task Swagger_InDevelopment_ReturnsHtmlPage()
    {
        using var factory = new DocumentationTestFactory("Development");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/index.html");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/html");
    }

    [Fact]
    public async Task SwaggerRoot_InDevelopment_RedirectsToUi()
    {
        using var factory = new DocumentationTestFactory("Development");
        using var client = factory.CreateClient();

        // El cliente sigue la redirección 301 de /swagger → /swagger/index.html.
        var response = await client.GetAsync("/swagger");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/html");
    }
}
