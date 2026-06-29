using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Backend.Tests.Infrastructure.Support;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;
using Shouldly;
using Xunit;

namespace Backend.Tests.Infrastructure.Api;

/// <summary>
/// Tests de integración HTTP del manejo de "no encontrado" (spec 021, US2).
/// Verifican el detalle GET /api/invoices/{id} (200/404) y el 404 del endpoint de transición ante
/// identificadores inexistentes o de formato inválido, sin errores no controlados (500).
/// </summary>
[Trait("Category", "Application")]
public sealed class InvoiceNotFoundApiTests
{
    // ── #7: detalle de factura existente → 200 con objeto completo (FR-006) ──────────

    [Fact]
    public async Task Detail_ExistingInvoice_Returns200WithFullObject()
    {
        var client = new Client("Acme", "acme@correo.com");
        var invoice = InvoiceTestFactory.Create(client.Id, 250m, InvoiceStatus.Pending);
        using var factory = new InvoiceApiFactory().SeedClient(client).SeedInvoice(invoice);
        var http = factory.CreateClient();

        var response = await http.GetAsync($"/api/invoices/{invoice.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("id").GetString().ShouldBe(invoice.Id);
        doc.RootElement.GetProperty("status").GetString().ShouldBe("pending");
        doc.RootElement.GetProperty("amount").GetDecimal().ShouldBe(250m);
        doc.RootElement.TryGetProperty("items", out _).ShouldBeTrue();
    }

    // ── #8: detalle de id inexistente → 404 (FR-006) ────────────────────────────────

    [Fact]
    public async Task Detail_NonexistentId_Returns404()
    {
        using var factory = new InvoiceApiFactory();
        var http = factory.CreateClient();

        var response = await http.GetAsync("/api/invoices/no-existe");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── #9: detalle de id con formato inválido → 404 uniforme, sin 500 (FR-007) ──────

    [Theory]
    [InlineData("/api/invoices/%20%20%20")]
    [InlineData("/api/invoices/!!!not-an-objectid!!!")]
    public async Task Detail_InvalidFormatId_Returns404NotServerError(string url)
    {
        using var factory = new InvoiceApiFactory();
        var http = factory.CreateClient();

        var response = await http.GetAsync(url);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── #14: transición sobre id inexistente → 404 (FR-011) ─────────────────────────

    [Fact]
    public async Task Transition_NonexistentId_Returns404()
    {
        using var factory = new InvoiceApiFactory();
        var http = factory.CreateClient();

        var response = await http.PostAsJsonAsync(
            "/api/invoices/transition/no-existe", new { newStatus = "pagado" });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
