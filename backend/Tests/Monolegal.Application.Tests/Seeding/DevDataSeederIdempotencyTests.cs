using System.Threading.Tasks;
using Backend.Application.Seeding;
using Microsoft.Extensions.Logging.Abstractions;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;
using Shouldly;
using Xunit;

namespace Backend.Tests.Monolegal.Application.Tests.Seeding;

/// <summary>
/// Tests unitarios de idempotencia/omisión del seeder (spec 008, US2, T015). Cubre CE-005.
/// </summary>
[Trait("Category", "Application")]
public class DevDataSeederIdempotencyTests
{
    private static DevDataSeeder CreateSeeder(FakeInvoiceRepository repo)
        => new(repo, NullLogger<DevDataSeeder>.Instance);

    [Fact]
    public async Task SeedAsync_WhenRepoNotEmpty_DoesNotSeed()
    {
        var repo = new FakeInvoiceRepository();
        var preexisting = new Invoice("preexistente", 500m);
        preexisting.UpdateStatus(InvoiceStatus.Pending);
        await repo.AddAsync(preexisting);
        var addCallsBefore = repo.AddCallCount;

        var result = await CreateSeeder(repo).SeedAsync();

        result.Seeded.ShouldBeFalse();
        result.ClientsCreated.ShouldBe(0);
        result.InvoicesCreated.ShouldBe(0);
        result.Reason.ShouldNotBeNullOrEmpty();
        // No se insertaron nuevas facturas más allá de la preexistente.
        repo.AddCallCount.ShouldBe(addCallsBefore);
        repo.Added.Count.ShouldBe(1);
    }

    [Fact]
    public async Task SeedAsync_RunTwice_DoesNotDuplicate()
    {
        var repo = new FakeInvoiceRepository();
        var seeder = CreateSeeder(repo);

        var first = await seeder.SeedAsync();
        var second = await seeder.SeedAsync();

        first.Seeded.ShouldBeTrue();
        second.Seeded.ShouldBeFalse();
        repo.Added.Count.ShouldBe(8);
    }
}
