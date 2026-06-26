using System.Threading.Tasks;
using Backend.Tests.Infrastructure.Support;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Repositories;
using Shouldly;
using Xunit;

namespace Backend.Tests.Monolegal.Application.Tests.Endpoints;

/// <summary>
/// Tests de la lógica de los endpoints de cliente (spec 018, US2) sin depender de MongoDB ni de la
/// capa HTTP: replican las reglas que aplican los handlers (unicidad de email RF-015a, guard de
/// borrado con facturas asociadas RF-018) sobre los repositorios en memoria.
/// </summary>
[Trait("Category", "Application")]
public class ClientCrudLogicTests
{
    private sealed record DeleteResult(bool Success, string? Error = null);

    /// <summary>Replica el handler de DELETE /api/clients/{id} (guard de facturas asociadas).</summary>
    private static async Task<DeleteResult> DeleteClientAsync(
        IClientRepository clients, IInvoiceRepository invoices, string id)
    {
        var client = await clients.GetByIdAsync(id);
        if (client is null) return new DeleteResult(false, "NotFound");

        var associated = await invoices.CountByClientIdAsync(id);
        if (associated > 0) return new DeleteResult(false, "Conflict");

        await clients.DeleteAsync(id);
        return new DeleteResult(true);
    }

    [Fact]
    public async Task DeleteClient_WithAssociatedInvoices_IsRejected()
    {
        var clients = new InMemoryClientRepository();
        var invoices = new InMemoryInvoiceRepository();
        var client = new Client("Acme", "acme@correo.com");
        await clients.AddAsync(client);
        await invoices.AddAsync(Invoice.Create(client.Id,
            new[] { new InvoiceItem("Servicio", 1m, 100m) }, System.DateTime.UtcNow.AddDays(30)));

        var result = await DeleteClientAsync(clients, invoices, client.Id);

        result.Success.ShouldBeFalse();
        result.Error.ShouldBe("Conflict");
        (await clients.GetByIdAsync(client.Id)).ShouldNotBeNull();
    }

    [Fact]
    public async Task DeleteClient_WithoutInvoices_Succeeds()
    {
        var clients = new InMemoryClientRepository();
        var invoices = new InMemoryInvoiceRepository();
        var client = new Client("Acme", "acme@correo.com");
        await clients.AddAsync(client);

        var result = await DeleteClientAsync(clients, invoices, client.Id);

        result.Success.ShouldBeTrue();
        (await clients.GetByIdAsync(client.Id)).ShouldBeNull();
    }

    [Fact]
    public async Task GetByEmail_IsCaseInsensitive_ForUniquenessChecks()
    {
        var clients = new InMemoryClientRepository();
        await clients.AddAsync(new Client("Acme", "Contacto@Acme.com"));

        var found = await clients.GetByEmailAsync("contacto@acme.com");

        found.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetPaged_FiltersByNameOrEmail_OrderedByName()
    {
        var clients = new InMemoryClientRepository();
        await clients.AddAsync(new Client("Zeta", "zeta@correo.com"));
        await clients.AddAsync(new Client("Alfa", "alfa@correo.com"));
        await clients.AddAsync(new Client("Beta", "otro@dominio.com"));

        var (items, total) = await clients.GetPagedAsync("correo.com", 1, 10);

        total.ShouldBe(2);
        items[0].Name.ShouldBe("Alfa"); // orden ascendente por nombre
    }
}
