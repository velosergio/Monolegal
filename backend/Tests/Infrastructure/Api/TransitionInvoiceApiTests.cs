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
/// Tests de integración HTTP de la transición POST /api/invoices/transition/{id} (spec 021, US3).
/// Verifican que una transición permitida persiste (200), una prohibida se rechaza (400) sin
/// cambios, y un cuerpo inválido se rechaza (400), todo a través del pipeline HTTP real.
/// </summary>
[Trait("Category", "Application")]
public sealed class TransitionInvoiceApiTests
{
    // ── #10: transición permitida → 200 + estado persistido (FR-008) ────────────────

    [Fact]
    public async Task Transition_Allowed_Returns200AndPersistsNewStatus()
    {
        var client = new Client("Acme", "acme@correo.com");
        var invoice = InvoiceTestFactory.Create(client.Id, 100m, InvoiceStatus.PrimerRecordatorio);
        using var factory = new InvoiceApiFactory().SeedClient(client).SeedInvoice(invoice);
        var http = factory.CreateClient();

        var response = await http.PostAsJsonAsync(
            $"/api/invoices/transition/{invoice.Id}", new { newStatus = "segundorecordatorio" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("status").GetString().ShouldBe("segundorecordatorio");

        var stored = await factory.Invoices.GetByIdAsync(invoice.Id);
        stored!.Status.ShouldBe(InvoiceStatus.SegundoRecordatorio);
    }

    // ── #11: transición prohibida → 400 + estado sin cambios (FR-009) ───────────────

    [Fact]
    public async Task Transition_Disallowed_Returns400AndKeepsStatus()
    {
        var invoice = InvoiceTestFactory.Create("c1", 100m, InvoiceStatus.Pending);
        using var factory = new InvoiceApiFactory().SeedInvoice(invoice);
        var http = factory.CreateClient();

        var response = await http.PostAsJsonAsync(
            $"/api/invoices/transition/{invoice.Id}", new { newStatus = "desactivado" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var stored = await factory.Invoices.GetByIdAsync(invoice.Id);
        stored!.Status.ShouldBe(InvoiceStatus.Pending);
    }

    // ── #12, #13: cuerpo inválido → 400 (FR-010) ────────────────────────────────────

    [Fact]
    public async Task Transition_InvalidNewStatus_Returns400()
    {
        var invoice = InvoiceTestFactory.Create("c1", 100m, InvoiceStatus.Pending);
        using var factory = new InvoiceApiFactory().SeedInvoice(invoice);
        var http = factory.CreateClient();

        var response = await http.PostAsJsonAsync(
            $"/api/invoices/transition/{invoice.Id}", new { newStatus = "foo" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Transition_MissingNewStatus_Returns400()
    {
        var invoice = InvoiceTestFactory.Create("c1", 100m, InvoiceStatus.Pending);
        using var factory = new InvoiceApiFactory().SeedInvoice(invoice);
        var http = factory.CreateClient();

        var response = await http.PostAsJsonAsync(
            $"/api/invoices/transition/{invoice.Id}", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
