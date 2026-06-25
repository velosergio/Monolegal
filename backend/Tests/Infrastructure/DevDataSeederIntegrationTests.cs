using System.Linq;
using System.Threading.Tasks;
using Backend.Application.Seeding;
using Backend.Tests.Infrastructure.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Monolegal.Domain.Enums;
using Shouldly;
using Xunit;

namespace Backend.Tests.Infrastructure;

/// <summary>
/// Tests de integración del sembrador de datos de desarrollo (spec 008, US1/US2, T009/T016)
/// contra MongoDB real. Cubre la siembra sobre base vacía y la idempotencia.
/// </summary>
[Trait("Category", "Integration")]
public sealed class DevDataSeederIntegrationTests : IClassFixture<MongoIntegrationFixture>
{
    private readonly MongoIntegrationFixture _fixture;

    public DevDataSeederIntegrationTests(MongoIntegrationFixture fixture)
        => _fixture = fixture;

    [Fact]
    public async Task SeedAsync_OnEmptyDatabase_Persists8InvoicesAcross3Clients()
    {
        var repo = await _fixture.CreateCleanRepositoryAsync();
        var seeder = new DevDataSeeder(repo, NullLogger<DevDataSeeder>.Instance);

        var result = await seeder.SeedAsync();

        result.Seeded.ShouldBeTrue();
        (await repo.CountAsync()).ShouldBe(8);

        var clientA = (await repo.GetByClientIdAsync(SeedDataDefinition.ClienteA)).ToList();
        var clientB = (await repo.GetByClientIdAsync(SeedDataDefinition.ClienteB)).ToList();
        var clientC = (await repo.GetByClientIdAsync(SeedDataDefinition.ClienteC)).ToList();
        clientA.Count.ShouldBe(3);
        clientB.Count.ShouldBe(2);
        clientC.Count.ShouldBe(3);

        (await repo.GetByStatusAsync(InvoiceStatus.PrimerRecordatorio)).Count().ShouldBeGreaterThanOrEqualTo(1);
        (await repo.GetByStatusAsync(InvoiceStatus.SegundoRecordatorio)).Count().ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task SeedAsync_RunTwice_DoesNotDuplicate()
    {
        var repo = await _fixture.CreateCleanRepositoryAsync();
        var seeder = new DevDataSeeder(repo, NullLogger<DevDataSeeder>.Instance);

        var first = await seeder.SeedAsync();
        var second = await seeder.SeedAsync();

        first.Seeded.ShouldBeTrue();
        second.Seeded.ShouldBeFalse();
        (await repo.CountAsync()).ShouldBe(8);
    }

    [Fact]
    public async Task SeedAsync_WithPreexistingData_IsSkipped()
    {
        var repo = await _fixture.CreateCleanRepositoryAsync();
        await repo.AddAsync(InvoiceTestFactory.Create("otro-cliente", status: InvoiceStatus.Pending));
        var seeder = new DevDataSeeder(repo, NullLogger<DevDataSeeder>.Instance);

        var result = await seeder.SeedAsync();

        result.Seeded.ShouldBeFalse();
        (await repo.CountAsync()).ShouldBe(1);
    }
}
