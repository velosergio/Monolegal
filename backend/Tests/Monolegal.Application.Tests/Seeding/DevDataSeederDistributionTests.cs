using System.Linq;
using System.Threading.Tasks;
using Backend.Application.Seeding;
using Microsoft.Extensions.Logging.Abstractions;
using Monolegal.Domain.Enums;
using Shouldly;
using Xunit;

namespace Backend.Tests.Monolegal.Application.Tests.Seeding;

/// <summary>
/// Tests unitarios de la distribución del dataset sembrado (spec 008, US1, T008).
/// Cubre CE-001..CE-004, CE-006 sin depender de MongoDB.
/// </summary>
[Trait("Category", "Application")]
public class DevDataSeederDistributionTests
{
    private static DevDataSeeder CreateSeeder(FakeInvoiceRepository repo)
        => new(repo, NullLogger<DevDataSeeder>.Instance);

    [Fact]
    public async Task SeedAsync_OnEmptyRepo_Creates8InvoicesAcross3Clients()
    {
        var repo = new FakeInvoiceRepository();
        var seeder = CreateSeeder(repo);

        var result = await seeder.SeedAsync();

        result.Seeded.ShouldBeTrue();
        result.ClientsCreated.ShouldBe(3);
        result.InvoicesCreated.ShouldBe(8);
        repo.Added.Count.ShouldBe(8);
        repo.Added.Select(i => i.ClientId).Distinct().Count().ShouldBe(3);
    }

    [Fact]
    public async Task SeedAsync_DistributesInvoices_3_2_3()
    {
        var repo = new FakeInvoiceRepository();
        await CreateSeeder(repo).SeedAsync();

        repo.Added.Count(i => i.ClientId == SeedDataDefinition.ClienteA).ShouldBe(3);
        repo.Added.Count(i => i.ClientId == SeedDataDefinition.ClienteB).ShouldBe(2);
        repo.Added.Count(i => i.ClientId == SeedDataDefinition.ClienteC).ShouldBe(3);
    }

    [Fact]
    public async Task SeedAsync_ClienteA_HasVariedStatuses()
    {
        var repo = new FakeInvoiceRepository();
        await CreateSeeder(repo).SeedAsync();

        var clienteAStatuses = repo.Added
            .Where(i => i.ClientId == SeedDataDefinition.ClienteA)
            .Select(i => i.Status)
            .Distinct()
            .ToList();

        clienteAStatuses.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task SeedAsync_Covers_PrimerAndSegundoRecordatorio()
    {
        var repo = new FakeInvoiceRepository();
        await CreateSeeder(repo).SeedAsync();

        repo.Added.Count(i => i.Status == InvoiceStatus.PrimerRecordatorio).ShouldBeGreaterThanOrEqualTo(1);
        repo.Added.Count(i => i.Status == InvoiceStatus.SegundoRecordatorio).ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task SeedAsync_AllStatuses_AreValidLifecycleValues()
    {
        var repo = new FakeInvoiceRepository();
        await CreateSeeder(repo).SeedAsync();

        foreach (var invoice in repo.Added)
            System.Enum.IsDefined(typeof(InvoiceStatus), invoice.Status).ShouldBeTrue();
    }

    [Theory]
    [InlineData(InvoiceStatus.Pending, 0)]
    [InlineData(InvoiceStatus.Pagado, 0)]
    [InlineData(InvoiceStatus.PrimerRecordatorio, 1)]
    [InlineData(InvoiceStatus.SegundoRecordatorio, 2)]
    [InlineData(InvoiceStatus.Desactivado, 2)]
    public async Task SeedAsync_RemindersCount_IsCoherentWithStatus(InvoiceStatus status, int expectedReminders)
    {
        var repo = new FakeInvoiceRepository();
        await CreateSeeder(repo).SeedAsync();

        foreach (var invoice in repo.Added.Where(i => i.Status == status))
            invoice.RemindersCount.ShouldBe(expectedReminders);
    }
}
