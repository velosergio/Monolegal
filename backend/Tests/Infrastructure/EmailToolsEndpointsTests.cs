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
using Monolegal.Domain.Enums;
using Monolegal.Domain.Repositories;
using Shouldly;
using Xunit;

namespace Backend.Tests.Infrastructure;

/// <summary>
/// Pruebas de integración HTTP de las herramientas globales de envío (spec 017, US4):
/// reenvío de fallidas y saneamiento de atascadas, con datos sembrados en memoria.
/// </summary>
[Trait("Category", "Application")]
public sealed class EmailToolsEndpointsTests
{
    private static Invoice MakeInvoice(InvoiceStatus status, NotificationOutcome outcome)
    {
        var invoice = new Invoice("cli-1", 100m);
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
            invoice.RecordNotificationResult(type, outcome, DateTime.UtcNow, outcome == NotificationOutcome.Failed ? "fallo previo" : null);
        }

        return invoice;
    }

    private sealed class Factory : WebApplicationFactory<Program>
    {
        private readonly InMemoryInvoiceRepository _invoices = new();

        public Factory Seed(params Invoice[] invoices)
        {
            foreach (var invoice in invoices)
                _invoices.AddAsync(invoice).GetAwaiter().GetResult();
            return this;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("MONGODB_URI", "mongodb://localhost:27017/email_tools_tests");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IHostedService>();
                services.RemoveAll<ISystemSettingsRepository>();
                services.AddSingleton<ISystemSettingsRepository, InMemorySystemSettingsRepository>();

                // Repositorio de facturas sembrado en memoria.
                services.RemoveAll<IInvoiceRepository>();
                services.AddSingleton<IInvoiceRepository>(_invoices);

                // Resolver con correo válido para que el reenvío pueda completarse (NoOp envía OK).
                services.RemoveAll<IClientEmailResolver>();
                services.AddSingleton<IClientEmailResolver>(new FakeClientEmailResolver("cliente@correo.com"));
            });
        }
    }

    [Fact]
    public async Task ResendFailed_ReenviaYDevuelveConteos()
    {
        using var factory = new Factory().Seed(
            MakeInvoice(InvoiceStatus.PrimerRecordatorio, NotificationOutcome.Failed),
            MakeInvoice(InvoiceStatus.Pagado, NotificationOutcome.Failed),
            MakeInvoice(InvoiceStatus.Pagado, NotificationOutcome.Sent));
        var client = factory.CreateClient();

        var response = await client.PostAsync("/api/settings/email/tools/resend-failed", null);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("attempted").GetInt32().ShouldBe(2);
        doc.RootElement.GetProperty("resent").GetInt32().ShouldBe(2);
        doc.RootElement.GetProperty("failed").GetInt32().ShouldBe(0);
    }

    [Fact]
    public async Task ResendFailed_SinCandidatos_DevuelveCeros()
    {
        using var factory = new Factory().Seed(
            MakeInvoice(InvoiceStatus.Pagado, NotificationOutcome.Sent));
        var client = factory.CreateClient();

        var response = await client.PostAsync("/api/settings/email/tools/resend-failed", null);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("attempted").GetInt32().ShouldBe(0);
    }

    [Fact]
    public async Task Sanitize_MarcaAtascadasYDevuelveConteo()
    {
        using var factory = new Factory().Seed(
            MakeInvoice(InvoiceStatus.PrimerRecordatorio, NotificationOutcome.None),
            MakeInvoice(InvoiceStatus.Pagado, NotificationOutcome.None),
            MakeInvoice(InvoiceStatus.Pending, NotificationOutcome.None));
        var client = factory.CreateClient();

        var response = await client.PostAsync("/api/settings/email/tools/sanitize", null);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("sanitized").GetInt32().ShouldBe(2);
    }
}
