using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Backend.Tests.Monolegal.Application.Tests.Documentation;

/// <summary>
/// Pruebas del documento OpenAPI servido en /openapi/v1.json (spec 010).
/// Cubren US1 (operaciones presentes), US2 (esquemas y códigos de estado) y
/// US3 (esquema de seguridad). Ver contracts/openapi-document.md.
/// </summary>
[Trait("Category", "Documentation")]
public class OpenApiDocumentTests
{
    private static async Task<JsonDocument> GetDocumentAsync()
    {
        var factory = new DocumentationTestFactory("Development");
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/openapi/v1.json");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        factory.Dispose();
        return JsonDocument.Parse(json);
    }

    // ---------------------------------------------------------------- US1 (FR-002)

    [Theory]
    [InlineData("/api/invoices", "get", "ListInvoices")]
    [InlineData("/api/invoices/{id}", "get", "GetInvoiceById")]
    [InlineData("/api/invoices/transition/{id}", "post", "TransitionInvoice")]
    [InlineData("/api/invoices/stats", "get", "GetInvoiceStats")]
    public async Task Document_ContainsInvoiceOperations(string path, string method, string operationId)
    {
        using var doc = await GetDocumentAsync();
        var paths = doc.RootElement.GetProperty("paths");

        paths.TryGetProperty(path, out var pathItem).ShouldBeTrue($"Falta la ruta {path}");
        pathItem.TryGetProperty(method, out var operation).ShouldBeTrue($"Falta {method.ToUpperInvariant()} {path}");
        operation.GetProperty("operationId").GetString().ShouldBe(operationId);
    }

    [Fact]
    public async Task Document_HasTitleAndVersion()
    {
        using var doc = await GetDocumentAsync();
        var info = doc.RootElement.GetProperty("info");

        info.GetProperty("title").GetString().ShouldBe("Monolegal API");
        info.GetProperty("version").GetString().ShouldBe("v1");
    }

    // ---------------------------------------------------------------- US2 (FR-005, FR-006)

    [Theory]
    [InlineData("InvoiceListItemDto")]
    [InlineData("InvoiceDetailDto")]
    [InlineData("TransitionRequest")]
    [InlineData("InvoiceStatsDto")]
    public async Task Document_ExposesDtoSchemas(string schemaName)
    {
        using var doc = await GetDocumentAsync();
        var schemas = doc.RootElement.GetProperty("components").GetProperty("schemas");

        schemas.TryGetProperty(schemaName, out _).ShouldBeTrue($"Falta el esquema {schemaName}");
    }

    [Fact]
    public async Task Document_ExposesPagedResponseSchema()
    {
        using var doc = await GetDocumentAsync();
        var schemas = doc.RootElement.GetProperty("components").GetProperty("schemas");

        // El esquema de respuesta paginada se genera con un nombre derivado del genérico
        // (p. ej. "PagedResponseOfInvoiceListItemDto"); basta con que exista alguno paginado.
        var hasPaged = false;
        foreach (var schema in schemas.EnumerateObject())
        {
            if (schema.Name.Contains("PagedResponse", System.StringComparison.OrdinalIgnoreCase))
            {
                hasPaged = true;
                break;
            }
        }

        hasPaged.ShouldBeTrue("No se encontró el esquema de respuesta paginada en components.schemas");
    }

    [Theory]
    [InlineData("/api/invoices", "get", new[] { "200", "400" })]
    [InlineData("/api/invoices/{id}", "get", new[] { "200", "404" })]
    [InlineData("/api/invoices/transition/{id}", "post", new[] { "200", "400", "404" })]
    [InlineData("/api/invoices/stats", "get", new[] { "200" })]
    public async Task Document_DeclaresExpectedStatusCodes(string path, string method, string[] expectedCodes)
    {
        using var doc = await GetDocumentAsync();
        var responses = doc.RootElement
            .GetProperty("paths").GetProperty(path)
            .GetProperty(method).GetProperty("responses");

        foreach (var code in expectedCodes)
        {
            responses.TryGetProperty(code, out _)
                .ShouldBeTrue($"La operación {method.ToUpperInvariant()} {path} no declara el código {code}");
        }
    }

    [Fact]
    public async Task Document_InvoiceStatusEnum_UsesLowercaseValues()
    {
        using var doc = await GetDocumentAsync();
        var json = doc.RootElement.GetRawText();

        // Los valores del enum se serializan en minúscula (LowerCaseNamingPolicy).
        foreach (var expected in new[]
                 {
                     "pending", "primerrecordatorio", "segundorecordatorio", "desactivado", "pagado"
                 })
        {
            json.ShouldContain($"\"{expected}\"");
        }
    }

    // ---------------------------------------------------------------- US3 (FR-011)

    [Fact]
    public async Task Document_DeclaresBearerSecurityScheme()
    {
        using var doc = await GetDocumentAsync();
        var schemes = doc.RootElement
            .GetProperty("components")
            .GetProperty("securitySchemes");

        schemes.TryGetProperty("Bearer", out var bearer).ShouldBeTrue("Falta el esquema de seguridad Bearer");
        bearer.GetProperty("type").GetString().ShouldBe("http");
        bearer.GetProperty("scheme").GetString().ShouldBe("bearer");
        bearer.GetProperty("bearerFormat").GetString().ShouldBe("JWT");
    }
}
