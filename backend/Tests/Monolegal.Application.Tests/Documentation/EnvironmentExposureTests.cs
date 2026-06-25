using System.Net;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Backend.Tests.Monolegal.Application.Tests.Documentation;

/// <summary>
/// Verifica que la documentación está restringida a entornos no productivos (spec 010, D3):
/// en Production tanto /swagger como /openapi/v1.json devuelven 404.
/// </summary>
[Trait("Category", "Documentation")]
public class EnvironmentExposureTests
{
    [Theory]
    [InlineData("/swagger/index.html")]
    [InlineData("/openapi/v1.json")]
    public async Task DocumentationEndpoints_InProduction_ReturnNotFound(string path)
    {
        using var factory = new DocumentationTestFactory("Production");
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
