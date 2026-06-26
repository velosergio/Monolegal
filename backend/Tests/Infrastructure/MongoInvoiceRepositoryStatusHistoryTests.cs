using System.Linq;
using System.Threading.Tasks;
using Backend.Tests.Infrastructure.Support;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;
using Shouldly;
using Xunit;

namespace Backend.Tests.Infrastructure;

/// <summary>
/// spec 015 (T007) — Round-trip del historial de cambios de estado embebido contra MongoDB real.
/// Verifica que <c>StatusHistory</c> se persiste y se lee correctamente y que <c>UpdateAsync</c>
/// (reemplazo completo) conserva el historial acumulado.
/// </summary>
[Trait("Category", "Integration")]
public sealed class MongoInvoiceRepositoryStatusHistoryTests : IClassFixture<MongoIntegrationFixture>
{
    private readonly MongoIntegrationFixture _fixture;

    public MongoInvoiceRepositoryStatusHistoryTests(MongoIntegrationFixture fixture)
        => _fixture = fixture;

    [Fact]
    public async Task StatusHistory_IsPersistedAndReadBack()
    {
        var repo = await _fixture.CreateCleanRepositoryAsync();
        var invoice = new Invoice("c1", 100m);
        invoice.UpdateStatus(InvoiceStatus.PrimerRecordatorio, StatusChangeSource.Automatic);
        await repo.AddAsync(invoice);

        var read = await repo.GetByIdAsync(invoice.Id);

        read.ShouldNotBeNull();
        read!.StatusHistory.Count.ShouldBe(1);
        read.StatusHistory[0].From.ShouldBe(InvoiceStatus.Pending);
        read.StatusHistory[0].To.ShouldBe(InvoiceStatus.PrimerRecordatorio);
        read.StatusHistory[0].Source.ShouldBe(StatusChangeSource.Automatic);
    }

    [Fact]
    public async Task UpdateAsync_PreservesAccumulatedHistory()
    {
        var repo = await _fixture.CreateCleanRepositoryAsync();
        var invoice = new Invoice("c1", 100m);
        invoice.UpdateStatus(InvoiceStatus.PrimerRecordatorio, StatusChangeSource.Automatic);
        await repo.AddAsync(invoice);

        // Segunda transición + persistencia por reemplazo completo.
        invoice.UpdateStatus(InvoiceStatus.SegundoRecordatorio);
        await repo.UpdateAsync(invoice);

        var read = await repo.GetByIdAsync(invoice.Id);

        read!.StatusHistory.Count.ShouldBe(2);
        read.StatusHistory.Last().To.ShouldBe(InvoiceStatus.SegundoRecordatorio);
        read.StatusHistory.Last().Source.ShouldBe(StatusChangeSource.Manual);
    }
}
