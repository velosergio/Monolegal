using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Backend.Application.Abstractions;
using Backend.Tests.Infrastructure.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;
using Monolegal.Domain.Repositories;
using Shouldly;
using Xunit;

namespace Backend.Tests.Infrastructure;

/// <summary>
/// Pruebas de integración HTTP del CRUD de facturas (spec 018, US1): POST/PUT/DELETE /api/invoices
/// ejercitados de extremo a extremo sobre los repositorios en memoria (sin MongoDB ni hosted services).
/// Cubre RF-001/RF-002/RF-003/RF-004a/RF-005/RF-010/RF-011.
/// </summary>
[Trait("Category", "Application")]
public sealed class InvoiceCrudEndpointsTests
{
    private sealed class Factory : WebApplicationFactory<Program>
    {
        public InMemoryInvoiceRepository Invoices { get; } = new();
        public InMemoryClientRepository Clients { get; } = new();

        public Factory SeedClient(Client client)
        {
            Clients.AddAsync(client).GetAwaiter().GetResult();
            return this;
        }

        public Factory SeedInvoice(Invoice invoice)
        {
            Invoices.AddAsync(invoice).GetAwaiter().GetResult();
            return this;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("MONGODB_URI", "mongodb://localhost:27017/invoice_crud_tests");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IHostedService>();
                services.RemoveAll<ISystemSettingsRepository>();
                services.AddSingleton<ISystemSettingsRepository, InMemorySystemSettingsRepository>();
                services.RemoveAll<IInvoiceRepository>();
                services.AddSingleton<IInvoiceRepository>(Invoices);
                services.RemoveAll<IClientRepository>();
                services.AddSingleton<IClientRepository>(Clients);
                services.RemoveAll<IClientEmailResolver>();
                services.AddSingleton<IClientEmailResolver>(new FakeClientEmailResolver("cliente@correo.com"));
            });
        }
    }

    private static object CreateBody(string clientId) => new
    {
        clientId,
        dueDate = "2026-09-01T00:00:00Z",
        items = new[]
        {
            new { description = "Asesoría", quantity = 2, unitPrice = 150 },
            new { description = "Trámite", quantity = 1, unitPrice = 50 },
        },
    };

    // ── POST /api/invoices (T019) ──────────────────────────────────────────────────

    [Fact]
    public async Task Create_WithValidClientAndItems_Returns201AndDerivesAmount()
    {
        var client = new Client("Acme", "acme@correo.com");
        using var factory = new Factory().SeedClient(client);
        var http = factory.CreateClient();

        var response = await http.PostAsJsonAsync("/api/invoices", CreateBody(client.Id));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("amount").GetDecimal().ShouldBe(350m); // 2*150 + 1*50
        doc.RootElement.GetProperty("items").GetArrayLength().ShouldBe(2);
        doc.RootElement.GetProperty("status").GetString().ShouldBe("pending");
    }

    [Fact]
    public async Task Create_WithNonexistentClient_Returns400()
    {
        using var factory = new Factory();
        var http = factory.CreateClient();

        var response = await http.PostAsJsonAsync("/api/invoices", CreateBody("no-existe"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithNoItems_Returns400()
    {
        var client = new Client("Acme", "acme@correo.com");
        using var factory = new Factory().SeedClient(client);
        var http = factory.CreateClient();

        var body = new { clientId = client.Id, dueDate = "2026-09-01T00:00:00Z", items = Array.Empty<object>() };
        var response = await http.PostAsJsonAsync("/api/invoices", body);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── PUT /api/invoices/{id} (T020) ───────────────────────────────────────────────

    [Fact]
    public async Task Update_NonexistentInvoice_Returns404()
    {
        var client = new Client("Acme", "acme@correo.com");
        using var factory = new Factory().SeedClient(client);
        var http = factory.CreateClient();

        var response = await http.PutAsJsonAsync("/api/invoices/no-existe", CreateBody(client.Id));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_RecalculatesAmount()
    {
        var client = new Client("Acme", "acme@correo.com");
        var invoice = Invoice.Create(client.Id,
            new[] { new InvoiceItem("Inicial", 1m, 100m) }, DateTime.UtcNow.AddDays(10));
        using var factory = new Factory().SeedClient(client).SeedInvoice(invoice);
        var http = factory.CreateClient();

        var response = await http.PutAsJsonAsync($"/api/invoices/{invoice.Id}", CreateBody(client.Id));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("amount").GetDecimal().ShouldBe(350m);
    }

    [Fact]
    public async Task Update_TerminalInvoice_Returns409()
    {
        var client = new Client("Acme", "acme@correo.com");
        var invoice = Invoice.Create(client.Id,
            new[] { new InvoiceItem("Inicial", 1m, 100m) }, DateTime.UtcNow.AddDays(10));
        invoice.UpdateStatus(InvoiceStatus.Pagado); // estado terminal
        using var factory = new Factory().SeedClient(client).SeedInvoice(invoice);
        var http = factory.CreateClient();

        var response = await http.PutAsJsonAsync($"/api/invoices/{invoice.Id}", CreateBody(client.Id));

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // ── DELETE /api/invoices/{id} (T021) ────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingInvoice_Returns204_InAnyState()
    {
        var client = new Client("Acme", "acme@correo.com");
        var invoice = Invoice.Create(client.Id,
            new[] { new InvoiceItem("X", 1m, 100m) }, DateTime.UtcNow.AddDays(10));
        invoice.UpdateStatus(InvoiceStatus.Pagado); // permitido borrar en estado terminal
        using var factory = new Factory().SeedClient(client).SeedInvoice(invoice);
        var http = factory.CreateClient();

        var response = await http.DeleteAsync($"/api/invoices/{invoice.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await factory.Invoices.GetByIdAsync(invoice.Id)).ShouldBeNull();
    }

    [Fact]
    public async Task Delete_NonexistentInvoice_Returns404()
    {
        using var factory = new Factory();
        var http = factory.CreateClient();

        var response = await http.DeleteAsync("/api/invoices/no-existe");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
