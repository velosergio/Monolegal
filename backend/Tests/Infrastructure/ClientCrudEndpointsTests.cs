using System;
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
using Monolegal.Domain.Repositories;
using Shouldly;
using Xunit;

namespace Backend.Tests.Infrastructure;

/// <summary>
/// Pruebas de integración HTTP del CRUD de clientes (spec 018, US2): GET/POST/PUT/DELETE /api/clients
/// de extremo a extremo sobre repositorios en memoria. Cubre RF-012/RF-013/RF-014/RF-015/RF-015a/RF-018.
/// </summary>
[Trait("Category", "Application")]
public sealed class ClientCrudEndpointsTests
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
            builder.UseSetting("MONGODB_URI", "mongodb://localhost:27017/client_crud_tests");
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

    // ── GET /api/clients (T037) ─────────────────────────────────────────────────────

    [Fact]
    public async Task List_FiltersBySearch_AndPaginates()
    {
        using var factory = new Factory()
            .SeedClient(new Client("Alfa", "alfa@correo.com"))
            .SeedClient(new Client("Beta", "beta@correo.com"))
            .SeedClient(new Client("Zeta", "zeta@otro.com"));
        var http = factory.CreateClient();

        var response = await http.GetAsync("/api/clients?search=correo.com&page=1&pageSize=10");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("total").GetInt64().ShouldBe(2);
        doc.RootElement.GetProperty("data").GetArrayLength().ShouldBe(2);
    }

    [Fact]
    public async Task List_WithInvalidPageSize_Returns400()
    {
        using var factory = new Factory();
        var http = factory.CreateClient();

        var response = await http.GetAsync("/api/clients?page=1&pageSize=999");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── POST/PUT /api/clients (T038) ────────────────────────────────────────────────

    [Fact]
    public async Task Create_WithValidData_Returns201()
    {
        using var factory = new Factory();
        var http = factory.CreateClient();

        var response = await http.PostAsJsonAsync("/api/clients", new
        {
            name = "Acme",
            email = "Acme@Correo.com",
            phone = "300",
            address = "Calle 1",
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("email").GetString().ShouldBe("acme@correo.com"); // normalizado
    }

    [Fact]
    public async Task Create_WithInvalidEmail_Returns400()
    {
        using var factory = new Factory();
        var http = factory.CreateClient();

        var response = await http.PostAsJsonAsync("/api/clients", new { name = "Acme", email = "no-email" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithDuplicateEmail_Returns400()
    {
        using var factory = new Factory().SeedClient(new Client("Existente", "dup@correo.com"));
        var http = factory.CreateClient();

        var response = await http.PostAsJsonAsync("/api/clients", new { name = "Otro", email = "DUP@correo.com" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_KeepingOwnEmail_Succeeds()
    {
        var client = new Client("Acme", "acme@correo.com");
        using var factory = new Factory().SeedClient(client);
        var http = factory.CreateClient();

        var response = await http.PutAsJsonAsync($"/api/clients/{client.Id}", new
        {
            name = "Acme Renombrada",
            email = "acme@correo.com", // mismo email del propio cliente
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("name").GetString().ShouldBe("Acme Renombrada");
    }

    // ── DELETE /api/clients/{id} (T039) ─────────────────────────────────────────────

    [Fact]
    public async Task Delete_WithoutInvoices_Returns204()
    {
        var client = new Client("Acme", "acme@correo.com");
        using var factory = new Factory().SeedClient(client);
        var http = factory.CreateClient();

        var response = await http.DeleteAsync($"/api/clients/{client.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_WithAssociatedInvoices_Returns409()
    {
        var client = new Client("Acme", "acme@correo.com");
        var invoice = Invoice.Create(client.Id,
            new[] { new InvoiceItem("X", 1m, 100m) }, DateTime.UtcNow.AddDays(10));
        using var factory = new Factory().SeedClient(client).SeedInvoice(invoice);
        var http = factory.CreateClient();

        var response = await http.DeleteAsync($"/api/clients/{client.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await factory.Clients.GetByIdAsync(client.Id)).ShouldNotBeNull();
    }

    [Fact]
    public async Task Delete_NonexistentClient_Returns404()
    {
        using var factory = new Factory();
        var http = factory.CreateClient();

        var response = await http.DeleteAsync("/api/clients/no-existe");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
