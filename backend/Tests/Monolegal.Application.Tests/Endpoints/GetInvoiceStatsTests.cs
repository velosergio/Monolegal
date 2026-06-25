using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Tests.Infrastructure.Support;
using Monolegal.Api.Endpoints.Invoices;
using Monolegal.Domain.Repositories;
using Monolegal.Domain.Enums;
using Shouldly;
using Xunit;

namespace Backend.Tests.Monolegal.Application.Tests.Endpoints;

/// <summary>
/// Tests del endpoint GET /api/invoices/stats (US4, spec 009).
/// Replica la composición del InvoiceStatsDto sobre el repositorio real (fake en memoria).
/// </summary>
[Trait("Category", "Application")]
public class GetInvoiceStatsTests
{
    /// <summary>Replica la composición del handler GetInvoiceStats.</summary>
    private static async Task<InvoiceStatsDto> BuildStatsAsync(IInvoiceRepository repo)
    {
        var total = await repo.CountAsync();
        var byStatusRaw = await repo.CountByStatusAsync();
        var byClient = await repo.CountByClientAsync();

        var byStatus = new Dictionary<string, long>();
        foreach (var (status, count) in byStatusRaw)
            byStatus[InvoiceStatusApi.ToApiString(status)] = count;

        return new InvoiceStatsDto(total, byStatus, byClient);
    }

    [Fact]
    public async Task Stats_ComputesTotalsByStatusAndClient()
    {
        var repo = new InMemoryInvoiceRepository();
        await repo.AddAsync(InvoiceTestFactory.Create("A", 100m, InvoiceStatus.Pending));
        await repo.AddAsync(InvoiceTestFactory.Create("A", 100m, InvoiceStatus.PrimerRecordatorio));
        await repo.AddAsync(InvoiceTestFactory.Create("A", 100m, InvoiceStatus.Pagado));
        await repo.AddAsync(InvoiceTestFactory.Create("B", 100m, InvoiceStatus.PrimerRecordatorio));
        await repo.AddAsync(InvoiceTestFactory.Create("C", 100m, InvoiceStatus.Pagado));

        var stats = await BuildStatsAsync(repo);

        stats.TotalInvoices.ShouldBe(5);
        stats.ByStatus["primerrecordatorio"].ShouldBe(2);
        stats.ByStatus["pagado"].ShouldBe(2);
        stats.ByStatus["pending"].ShouldBe(1);
        stats.ByClient["A"].ShouldBe(3);
        stats.ByClient["B"].ShouldBe(1);
        stats.ByClient["C"].ShouldBe(1);
    }

    [Fact]
    public async Task Stats_ByStatusSumEqualsTotal()
    {
        var repo = new InMemoryInvoiceRepository();
        await repo.AddAsync(InvoiceTestFactory.Create("A", 100m, InvoiceStatus.Pending));
        await repo.AddAsync(InvoiceTestFactory.Create("B", 100m, InvoiceStatus.SegundoRecordatorio));
        await repo.AddAsync(InvoiceTestFactory.Create("C", 100m, InvoiceStatus.Desactivado));

        var stats = await BuildStatsAsync(repo);

        stats.ByStatus.Values.Sum().ShouldBe(stats.TotalInvoices);
    }

    [Fact]
    public async Task Stats_EmptyDatabase_ReturnsZeroAndEmptyMaps()
    {
        var repo = new InMemoryInvoiceRepository();

        var stats = await BuildStatsAsync(repo);

        stats.TotalInvoices.ShouldBe(0);
        stats.ByStatus.ShouldBeEmpty();
        stats.ByClient.ShouldBeEmpty();
    }
}
