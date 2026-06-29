using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
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
/// Pruebas de integración HTTP de la cancelación de envío POST /api/invoices/{id}/cancel-notification
/// (spec 019, US4): pendiente → omitido (200), no pendiente (409), inexistente (404).
/// </summary>
[Trait("Category", "Application")]
public sealed class CancelInvoiceNotificationEndpointTests
{
    private static Invoice Pending(string clientId)
    {
        var invoice = new Invoice(clientId, 100m);
        invoice.UpdateStatus(InvoiceStatus.PrimerRecordatorio); // notificable, LastNotificationOutcome == None
        return invoice;
    }

    private static Invoice Sent(string clientId)
    {
        var invoice = new Invoice(clientId, 100m);
        invoice.UpdateStatus(InvoiceStatus.Pagado);
        invoice.RecordNotificationResult(NotificationType.PaymentConfirmation, NotificationOutcome.Sent, DateTime.UtcNow);
        return invoice;
    }

    private sealed class Factory : WebApplicationFactory<Program>
    {
        private readonly InMemoryInvoiceRepository _invoices = new();
        private readonly InMemoryClientRepository _clients = new();

        public Factory Seed(Client client, params Invoice[] invoices)
        {
            _clients.AddAsync(client).GetAwaiter().GetResult();
            foreach (var invoice in invoices)
                _invoices.AddAsync(invoice).GetAwaiter().GetResult();
            return this;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("MONGODB_URI", "mongodb://localhost:27017/shipments_cancel_tests");
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
    public async Task Cancel_PendingInvoice_BecomesSkipped()
    {
        var invoice = Pending("cli-1");
        using var factory = new Factory().Seed(Client.CreateForSeed("cli-1", "ACME", "a@b.com"), invoice);
        var http = factory.CreateClient();

        var response = await http.PostAsync($"/api/invoices/{invoice.Id}/cancel-notification", null);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("sendStatus").GetString().ShouldBe("skipped");
    }

    [Fact]
    public async Task Cancel_NonPendingInvoice_Returns409()
    {
        var invoice = Sent("cli-1");
        using var factory = new Factory().Seed(Client.CreateForSeed("cli-1", "ACME", "a@b.com"), invoice);
        var http = factory.CreateClient();

        var response = await http.PostAsync($"/api/invoices/{invoice.Id}/cancel-notification", null);
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Cancel_UnknownId_Returns404()
    {
        using var factory = new Factory().Seed(Client.CreateForSeed("cli-1", "ACME", "a@b.com"));
        var http = factory.CreateClient();

        var response = await http.PostAsync("/api/invoices/nope/cancel-notification", null);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
