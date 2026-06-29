using System;
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
/// Tests de integración HTTP del listado GET /api/invoices (spec 021, US1).
/// Ejercitan el endpoint a través del pipeline real (WebApplicationFactory) verificando el
/// contrato observable: 200 + estructura, filtro por estado y rechazo de parámetros inválidos.
/// </summary>
[Trait("Category", "Application")]
public sealed class ListInvoicesApiTests
{
    private static Invoice At(string clientId, InvoiceStatus status, DateTime createdAt)
    {
        var invoice = InvoiceTestFactory.Create(clientId, 100m, status);
        invoice.OverrideCreatedAt(createdAt);
        return invoice;
    }

    // ── #1, #3: 200 + estructura + paginación (FR-003) ──────────────────────────────

    [Fact]
    public async Task List_WithInvoices_Returns200WithDataTotalAndPageSize()
    {
        using var factory = new InvoiceApiFactory();
        factory.SeedInvoice(At("c1", InvoiceStatus.Pending, DateTime.UtcNow));
        factory.SeedInvoice(At("c2", InvoiceStatus.PrimerRecordatorio, DateTime.UtcNow.AddSeconds(1)));
        var http = factory.CreateClient();

        var response = await http.GetAsync("/api/invoices");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.TryGetProperty("data", out _).ShouldBeTrue();
        doc.RootElement.GetProperty("total").GetInt64().ShouldBe(2);
        doc.RootElement.GetProperty("pageSize").GetInt32().ShouldBe(10);
    }

    [Fact]
    public async Task List_WithPaging_ReturnsAtMostPageSizeAndFullTotal()
    {
        using var factory = new InvoiceApiFactory();
        for (var i = 0; i < 25; i++)
            factory.SeedInvoice(At($"c{i}", InvoiceStatus.Pending, DateTime.UtcNow.AddSeconds(i)));
        var http = factory.CreateClient();

        var response = await http.GetAsync("/api/invoices?page=1&pageSize=10");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("data").GetArrayLength().ShouldBe(10);
        doc.RootElement.GetProperty("total").GetInt64().ShouldBe(25);
    }

    // ── #2: filtro por estado (FR-004) ──────────────────────────────────────────────

    [Fact]
    public async Task List_WithStatusFilter_ReturnsOnlyMatchingAndCoherentTotal()
    {
        using var factory = new InvoiceApiFactory();
        factory.SeedInvoice(At("c1", InvoiceStatus.Pending, DateTime.UtcNow));
        factory.SeedInvoice(At("c2", InvoiceStatus.PrimerRecordatorio, DateTime.UtcNow.AddSeconds(1)));
        factory.SeedInvoice(At("c3", InvoiceStatus.PrimerRecordatorio, DateTime.UtcNow.AddSeconds(2)));
        var http = factory.CreateClient();

        var response = await http.GetAsync("/api/invoices?status=primerrecordatorio");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("total").GetInt64().ShouldBe(2);
        foreach (var item in doc.RootElement.GetProperty("data").EnumerateArray())
            item.GetProperty("status").GetString().ShouldBe("primerrecordatorio");
    }

    // ── #4, #5, #6: parámetros inválidos → 400 (FR-005) ─────────────────────────────

    [Theory]
    [InlineData("/api/invoices?status=foo")]
    [InlineData("/api/invoices?page=0")]
    [InlineData("/api/invoices?pageSize=51")]
    public async Task List_WithInvalidParameters_Returns400(string url)
    {
        using var factory = new InvoiceApiFactory();
        var http = factory.CreateClient();

        var response = await http.GetAsync(url);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── #17: página fuera de rango → 200 vacío + total real (FR-003) ────────────────

    [Fact]
    public async Task List_PageOutOfRange_Returns200EmptyWithRealTotal()
    {
        using var factory = new InvoiceApiFactory();
        for (var i = 0; i < 5; i++)
            factory.SeedInvoice(At($"c{i}", InvoiceStatus.Pending, DateTime.UtcNow.AddSeconds(i)));
        var http = factory.CreateClient();

        var response = await http.GetAsync("/api/invoices?page=99&pageSize=10");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("data").GetArrayLength().ShouldBe(0);
        doc.RootElement.GetProperty("total").GetInt64().ShouldBe(5);
    }
}
