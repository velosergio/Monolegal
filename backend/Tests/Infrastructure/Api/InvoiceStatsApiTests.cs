using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Backend.Tests.Infrastructure.Support;
using Monolegal.Domain.Enums;
using Shouldly;
using Xunit;

namespace Backend.Tests.Infrastructure.Api;

/// <summary>
/// Tests de integración HTTP de las estadísticas GET /api/invoices/stats (spec 021, US4).
/// Verifican la respuesta ante base vacía (200 con ceros) y el invariante Σ(byStatus)==totalInvoices.
/// </summary>
[Trait("Category", "Application")]
public sealed class InvoiceStatsApiTests
{
    // ── #15: base vacía → 200 con ceros y agregados vacíos (FR-002) ──────────────────

    [Fact]
    public async Task Stats_EmptyDatabase_Returns200WithZeroAndEmptyAggregates()
    {
        using var factory = new InvoiceApiFactory();
        var http = factory.CreateClient();

        var response = await http.GetAsync("/api/invoices/stats");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("totalInvoices").GetInt64().ShouldBe(0);
        doc.RootElement.GetProperty("byStatus").GetRawText().ShouldBe("{}");
        doc.RootElement.GetProperty("byClient").GetRawText().ShouldBe("{}");
    }

    // ── #16: Σ(byStatus) == totalInvoices (FR-002) ──────────────────────────────────

    [Fact]
    public async Task Stats_WithMixedInvoices_ByStatusSumEqualsTotal()
    {
        using var factory = new InvoiceApiFactory();
        factory.SeedInvoice(InvoiceTestFactory.Create("c1", 100m, InvoiceStatus.Pending));
        factory.SeedInvoice(InvoiceTestFactory.Create("c2", 100m, InvoiceStatus.Pending));
        factory.SeedInvoice(InvoiceTestFactory.Create("c3", 100m, InvoiceStatus.Pagado));
        var http = factory.CreateClient();

        var response = await http.GetAsync("/api/invoices/stats");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var total = doc.RootElement.GetProperty("totalInvoices").GetInt64();
        total.ShouldBe(3);

        long sum = 0;
        foreach (var entry in doc.RootElement.GetProperty("byStatus").EnumerateObject())
            sum += entry.Value.GetInt64();
        sum.ShouldBe(total);
    }
}
