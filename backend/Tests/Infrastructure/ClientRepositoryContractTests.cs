using System.Threading.Tasks;
using MongoDB.Driver;
using Monolegal.Domain.Entities;
using Monolegal.Infrastructure.Repositories;
using Backend.Tests.Infrastructure.Support;
using Shouldly;
using Xunit;

namespace Backend.Tests.Infrastructure;

/// <summary>
/// Tests de contrato de <see cref="MongoClientRepository"/> contra MongoDB real (spec 018, T004):
/// alta/lectura/edición/borrado, búsqueda por nombre/email con orden y paginación, unicidad de email
/// (índice) y conteos. Requiere un MongoDB en ejecución (ver <see cref="MongoIntegrationFixture"/>).
/// </summary>
[Trait("Category", "Integration")]
public sealed class ClientRepositoryContractTests : IClassFixture<MongoIntegrationFixture>
{
    private readonly MongoIntegrationFixture _fixture;

    public ClientRepositoryContractTests(MongoIntegrationFixture fixture) => _fixture = fixture;

    private async Task<MongoClientRepository> CleanRepoAsync()
    {
        await _fixture.Database.DropCollectionAsync("Clients");
        return new MongoClientRepository(_fixture.Database);
    }

    [Fact]
    public async Task AddAndGetById_RoundTrips()
    {
        var repo = await CleanRepoAsync();
        var client = new Client("Acme", "acme@correo.com", "300", "Calle 1");

        await repo.AddAsync(client);

        var fetched = await repo.GetByIdAsync(client.Id);
        fetched.ShouldNotBeNull();
        fetched.Name.ShouldBe("Acme");
        fetched.Email.ShouldBe("acme@correo.com");
        fetched.Phone.ShouldBe("300");
    }

    [Fact]
    public async Task GetByEmail_IsCaseInsensitiveViaNormalization()
    {
        var repo = await CleanRepoAsync();
        await repo.AddAsync(new Client("Acme", "Contacto@Acme.com"));

        var found = await repo.GetByEmailAsync("contacto@acme.com");

        found.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetPaged_FiltersBySearch_OrdersByName()
    {
        var repo = await CleanRepoAsync();
        await repo.AddAsync(new Client("Zeta", "zeta@correo.com"));
        await repo.AddAsync(new Client("Alfa", "alfa@correo.com"));
        await repo.AddAsync(new Client("Beta", "beta@otro.com"));

        var (items, total) = await repo.GetPagedAsync("correo.com", 1, 10);

        total.ShouldBe(2);
        items[0].Name.ShouldBe("Alfa"); // orden ascendente por nombre
    }

    [Fact]
    public async Task Update_PersistsChanges()
    {
        var repo = await CleanRepoAsync();
        var client = new Client("Acme", "acme@correo.com");
        await repo.AddAsync(client);

        client.Update("Acme 2", "acme2@correo.com", "999", null);
        await repo.UpdateAsync(client);

        var fetched = await repo.GetByIdAsync(client.Id);
        fetched!.Name.ShouldBe("Acme 2");
        fetched.Email.ShouldBe("acme2@correo.com");
    }

    [Fact]
    public async Task Delete_RemovesClient()
    {
        var repo = await CleanRepoAsync();
        var client = new Client("Acme", "acme@correo.com");
        await repo.AddAsync(client);

        (await repo.DeleteAsync(client.Id)).ShouldBeTrue();
        (await repo.GetByIdAsync(client.Id)).ShouldBeNull();
    }

    [Fact]
    public async Task UniqueEmailIndex_RejectsDuplicate()
    {
        var repo = await CleanRepoAsync();
        await _fixture.CreateIndexBuilder().EnsureIndexesAsync();
        await repo.AddAsync(new Client("Acme", "dup@correo.com"));

        // Un segundo cliente con el mismo email (normalizado) debe violar el índice único.
        await Should.ThrowAsync<MongoWriteException>(
            async () => await repo.AddAsync(new Client("Otro", "DUP@correo.com")));
    }
}
