using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Backend.Tests.Infrastructure.Support;
using Monolegal.Domain.Enums;
using Shouldly;
using Xunit;

namespace Backend.Tests.Infrastructure.Api;

/// <summary>
/// Tests del aislamiento y determinismo de la infraestructura de pruebas (spec 021, US4).
/// Demuestran que dos instancias de <see cref="InvoiceApiFactory"/> no comparten datos, de modo
/// que la suite es repetible y no depende del orden de ejecución (FR-012, FR-013).
/// </summary>
[Trait("Category", "Application")]
public sealed class ApiTestIsolationTests
{
    [Fact]
    public async Task TwoFactories_DoNotShareSeededData()
    {
        using var seeded = new InvoiceApiFactory();
        seeded.SeedInvoice(InvoiceTestFactory.Create("c1", 100m, InvoiceStatus.Pending));
        using var empty = new InvoiceApiFactory();

        var seededHttp = seeded.CreateClient();
        var emptyHttp = empty.CreateClient();

        var seededTotal = await GetTotalAsync(seededHttp);
        var emptyTotal = await GetTotalAsync(emptyHttp);

        seededTotal.ShouldBe(1);
        emptyTotal.ShouldBe(0);
    }

    private static async Task<long> GetTotalAsync(System.Net.Http.HttpClient http)
    {
        var response = await http.GetAsync("/api/invoices");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("total").GetInt64();
    }
}
