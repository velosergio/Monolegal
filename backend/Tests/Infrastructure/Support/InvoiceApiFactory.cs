using Backend.Application.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Repositories;

namespace Backend.Tests.Infrastructure.Support;

/// <summary>
/// Fábrica de aplicación de pruebas compartida para los tests de integración HTTP de los endpoints
/// de facturas (spec 021, T002). Arranca la app real en memoria con <see cref="WebApplicationFactory{TEntryPoint}"/>
/// y sustituye las dependencias de infraestructura por dobles en memoria, de modo que la suite
/// ejercite el pipeline HTTP completo (enrutamiento, binding, serialización, traducción a códigos de
/// estado) sin requerir MongoDB ni servicios en segundo plano.
/// </summary>
/// <remarks>
/// Patrón alineado con <c>InvoiceCrudEndpointsTests</c> (spec 018): entorno Development, MONGODB_URI
/// dummy (el registro de Mongo se construye pero sus repositorios quedan reemplazados), worker de
/// transiciones desactivado (<c>RemoveAll&lt;IHostedService&gt;</c>) y notificador no-op.
/// Cada instancia posee sus propios repositorios en memoria ⇒ aislamiento de datos por test.
/// </remarks>
public sealed class InvoiceApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Almacén de facturas aislado de esta instancia.</summary>
    public InMemoryInvoiceRepository Invoices { get; } = new();

    /// <summary>Almacén de clientes aislado de esta instancia.</summary>
    public InMemoryClientRepository Clients { get; } = new();

    /// <summary>Registro de invocaciones del notificador (no envía correo).</summary>
    public FakeTransitionNotifier Notifier { get; } = new();

    /// <summary>Siembra un cliente y devuelve la fábrica para encadenar.</summary>
    public InvoiceApiFactory SeedClient(Client client)
    {
        Clients.AddAsync(client).GetAwaiter().GetResult();
        return this;
    }

    /// <summary>Siembra una factura y devuelve la fábrica para encadenar.</summary>
    public InvoiceApiFactory SeedInvoice(Invoice invoice)
    {
        Invoices.AddAsync(invoice).GetAwaiter().GetResult();
        return this;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("MONGODB_URI", "mongodb://localhost:27017/invoice_api_tests");
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

            services.RemoveAll<IInvoiceTransitionNotifier>();
            services.AddSingleton<IInvoiceTransitionNotifier>(Notifier);
        });
    }
}
