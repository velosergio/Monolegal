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
/// La herramienta global "Reintentar fallidos" (spec 017) debe incrementar el contador de reintentos
/// del aviso vigente por cada factura reintentada (spec 019, D6).
/// </summary>
[Trait("Category", "Application")]
public sealed class EmailToolsRetryCountTests
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
            builder.UseSetting("MONGODB_URI", "mongodb://localhost:27017/shipments_retrycount_tests");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IHostedService>();
                services.RemoveAll<IInvoiceRepository>();
                services.AddSingleton<IInvoiceRepository>(_invoices);
                services.RemoveAll<IClientRepository>();
                services.AddSingleton<IClientRepository>(_clients);
                services.RemoveAll<IClientEmailResolver>();
                services.AddSingleton<IClientEmailResolver>(new FakeClientEmailResolver("cliente@correo.com"));
            });
        }
    }

    [Fact]
    public async Task ResendFailed_IncrementsRetryCount()
    {
        using var factory = new Factory().Seed(
            Client.CreateForSeed("cli-1", "ACME", "a@b.com"),
            Failed("cli-1"));
        var http = factory.CreateClient();

        var response = await http.PostAsync("/api/settings/email/tools/resend-failed", null);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Tras el reintento masivo, el listado de envíos refleja retryCount == 1.
        using var doc = JsonDocument.Parse(await http.GetStringAsync("/api/invoices/shipments"));
        doc.RootElement.GetProperty("data")[0].GetProperty("retryCount").GetInt32().ShouldBe(1);
    }
}
