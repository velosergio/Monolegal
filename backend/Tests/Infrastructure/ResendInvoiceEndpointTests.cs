using System;
using System.Net;
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
/// Pruebas de integración HTTP del reenvío por factura POST /api/invoices/{id}/resend (spec 019, US2).
/// </summary>
[Trait("Category", "Application")]
public sealed class ResendInvoiceEndpointTests
{
    private static Invoice Failed(string clientId)
    {
        var invoice = new Invoice(clientId, 100m);
        invoice.UpdateStatus(InvoiceStatus.PrimerRecordatorio);
        invoice.RecordNotificationResult(NotificationType.Reminder, NotificationOutcome.Failed, DateTime.UtcNow, "fallo");
        return invoice;
    }

    private sealed class Factory : WebApplicationFactory<Program>
    {
        private readonly InMemoryInvoiceRepository _invoices = new();
        private readonly InMemoryClientRepository _clients = new();
        private readonly string _email;

        public Factory(string email = "cliente@correo.com") => _email = email;

        public Factory Seed(Client client, params Invoice[] invoices)
        {
            _clients.AddAsync(client).GetAwaiter().GetResult();
            foreach (var invoice in invoices)
                _invoices.AddAsync(invoice).GetAwaiter().GetResult();
            return this;
        }

        public Invoice? Get(string id) => _invoices.GetByIdAsync(id).GetAwaiter().GetResult();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("MONGODB_URI", "mongodb://localhost:27017/shipments_resend_tests");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IHostedService>();
                services.RemoveAll<IInvoiceRepository>();
                services.AddSingleton<IInvoiceRepository>(_invoices);
                services.RemoveAll<IClientRepository>();
                services.AddSingleton<IClientRepository>(_clients);
                services.RemoveAll<IClientEmailResolver>();
                services.AddSingleton<IClientEmailResolver>(new FakeClientEmailResolver(_email));
            });
        }
    }

    [Fact]
    public async Task Resend_Failed_BecomesSent_AndIncrementsRetry()
    {
        var invoice = Failed("cli-1");
        using var factory = new Factory().Seed(Client.CreateForSeed("cli-1", "ACME", "a@b.com"), invoice);
        var http = factory.CreateClient();

        var response = await http.PostAsync($"/api/invoices/{invoice.Id}/resend", null);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("sendStatus").GetString().ShouldBe("sent");
        doc.RootElement.GetProperty("retryCount").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task Resend_WithoutResolvableEmail_IsSkipped()
    {
        var invoice = Failed("cli-1");
        using var factory = new Factory(email: "").Seed(Client.CreateForSeed("cli-1", "ACME", "a@b.com"), invoice);
        var http = factory.CreateClient();

        var response = await http.PostAsync($"/api/invoices/{invoice.Id}/resend", null);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("sendStatus").GetString().ShouldBe("skipped");
        doc.RootElement.GetProperty("retryCount").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task Resend_UnknownId_Returns404()
    {
        using var factory = new Factory();
        var http = factory.CreateClient();

        var response = await http.PostAsync("/api/invoices/does-not-exist/resend", null);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
