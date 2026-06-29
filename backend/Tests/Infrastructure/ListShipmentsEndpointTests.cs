using System;
using System.Net;
using System.Net.Http;
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
/// Pruebas de integración HTTP del listado de envíos GET /api/invoices/shipments (spec 019, US1).
/// </summary>
[Trait("Category", "Application")]
public sealed class ListShipmentsEndpointTests
{
    private static Invoice MakeInvoice(string clientId, InvoiceStatus status, NotificationOutcome outcome)
    {
        var invoice = new Invoice(clientId, 100m);
        if (status != InvoiceStatus.Pending)
            invoice.UpdateStatus(status);

        if (outcome != NotificationOutcome.None)
        {
            var type = status switch
            {
                InvoiceStatus.Pagado => NotificationType.PaymentConfirmation,
                InvoiceStatus.Desactivado => NotificationType.DeactivationNotice,
                _ => NotificationType.Reminder,
            };
            invoice.RecordNotificationResult(type, outcome, DateTime.UtcNow, outcome == NotificationOutcome.Failed ? "fallo" : null);
        }

        return invoice;
    }

    private sealed class Factory : WebApplicationFactory<Program>
    {
        private readonly InMemoryInvoiceRepository _invoices = new();
        private readonly InMemoryClientRepository _clients = new();

        public Factory SeedClient(Client client)
        {
            _clients.AddAsync(client).GetAwaiter().GetResult();
            return this;
        }

        public Factory SeedInvoices(params Invoice[] invoices)
        {
            foreach (var invoice in invoices)
                _invoices.AddAsync(invoice).GetAwaiter().GetResult();
            return this;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("MONGODB_URI", "mongodb://localhost:27017/shipments_list_tests");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IHostedService>();
                services.RemoveAll<IInvoiceRepository>();
                services.AddSingleton<IInvoiceRepository>(_invoices);
                services.RemoveAll<IClientRepository>();
                services.AddSingleton<IClientRepository>(_clients);
            });
        }
    }

    [Fact]
    public async Task List_ReturnsOnlyNotifiableInvoices()
    {
        var client = Client.CreateForSeed("cli-1", "ACME", "pagos@acme.com");
        using var factory = new Factory()
            .SeedClient(client)
            .SeedInvoices(
                MakeInvoice("cli-1", InvoiceStatus.PrimerRecordatorio, NotificationOutcome.Failed),
                MakeInvoice("cli-1", InvoiceStatus.Pagado, NotificationOutcome.Sent),
                MakeInvoice("cli-1", InvoiceStatus.Pending, NotificationOutcome.None)); // no notificable
        var http = factory.CreateClient();

        using var doc = JsonDocument.Parse(await http.GetStringAsync("/api/invoices/shipments"));
        doc.RootElement.GetProperty("total").GetInt32().ShouldBe(2);
        var first = doc.RootElement.GetProperty("data")[0];
        first.GetProperty("clientName").GetString().ShouldBe("ACME");
        first.GetProperty("clientEmail").GetString().ShouldBe("pagos@acme.com");
    }

    [Fact]
    public async Task List_FiltersBySendStatus()
    {
        using var factory = new Factory()
            .SeedClient(Client.CreateForSeed("cli-1", "ACME", "a@b.com"))
            .SeedInvoices(
                MakeInvoice("cli-1", InvoiceStatus.PrimerRecordatorio, NotificationOutcome.Failed),
                MakeInvoice("cli-1", InvoiceStatus.Pagado, NotificationOutcome.Sent));
        var http = factory.CreateClient();

        using var doc = JsonDocument.Parse(await http.GetStringAsync("/api/invoices/shipments?sendStatus=failed"));
        doc.RootElement.GetProperty("total").GetInt32().ShouldBe(1);
        doc.RootElement.GetProperty("data")[0].GetProperty("sendStatus").GetString().ShouldBe("failed");
    }

    [Fact]
    public async Task List_SearchesByClientNameOrEmail()
    {
        using var factory = new Factory()
            .SeedClient(Client.CreateForSeed("cli-1", "ACME", "pagos@acme.com"))
            .SeedClient(Client.CreateForSeed("cli-2", "Globex", "info@globex.com"))
            .SeedInvoices(
                MakeInvoice("cli-1", InvoiceStatus.Pagado, NotificationOutcome.Sent),
                MakeInvoice("cli-2", InvoiceStatus.Pagado, NotificationOutcome.Sent));
        var http = factory.CreateClient();

        using var byName = JsonDocument.Parse(await http.GetStringAsync("/api/invoices/shipments?search=globex"));
        byName.RootElement.GetProperty("total").GetInt32().ShouldBe(1);
        byName.RootElement.GetProperty("data")[0].GetProperty("clientName").GetString().ShouldBe("Globex");

        using var byEmail = JsonDocument.Parse(await http.GetStringAsync("/api/invoices/shipments?search=acme.com"));
        byEmail.RootElement.GetProperty("total").GetInt32().ShouldBe(1);
        byEmail.RootElement.GetProperty("data")[0].GetProperty("clientName").GetString().ShouldBe("ACME");
    }

    [Fact]
    public async Task List_InvalidSendStatus_ReturnsBadRequest()
    {
        using var factory = new Factory();
        var http = factory.CreateClient();

        var response = await http.GetAsync("/api/invoices/shipments?sendStatus=bogus");
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_NoMatches_ReturnsEmptyPage()
    {
        using var factory = new Factory()
            .SeedClient(Client.CreateForSeed("cli-1", "ACME", "a@b.com"))
            .SeedInvoices(MakeInvoice("cli-1", InvoiceStatus.Pagado, NotificationOutcome.Sent));
        var http = factory.CreateClient();

        using var doc = JsonDocument.Parse(await http.GetStringAsync("/api/invoices/shipments?search=zzz"));
        doc.RootElement.GetProperty("total").GetInt32().ShouldBe(0);
        doc.RootElement.GetProperty("data").GetArrayLength().ShouldBe(0);
    }
}
