using Backend.Tests.Infrastructure.Support;
using Monolegal.Domain.Enums;
using Shouldly;
using Xunit;

namespace Backend.Tests.Infrastructure;

/// <summary>
/// US3 (spec 007, T009) — Tests de integración de <c>UpdateStatusAsync</c> contra MongoDB real.
/// Cubre FR-003, FR-007, FR-009, SC-003, SC-004.
/// </summary>
[Trait("Category", "Integration")]
public sealed class MongoInvoiceRepositoryStatusUpdateTests : IClassFixture<MongoIntegrationFixture>
{
    private readonly MongoIntegrationFixture _fixture;

    public MongoInvoiceRepositoryStatusUpdateTests(MongoIntegrationFixture fixture)
        => _fixture = fixture;

    [Fact]
    public async Task UpdateStatusAsync_ChangesStatusAndUpdatesTransitionTimestamp()
    {
        var repo = await _fixture.CreateCleanRepositoryAsync();
        var invoice = InvoiceTestFactory.Create("c1", status: InvoiceStatus.Pending);
        await repo.AddAsync(invoice);
        var before = (await repo.GetByIdAsync(invoice.Id))!;

        await repo.UpdateStatusAsync(invoice.Id, InvoiceStatus.PrimerRecordatorio);

        var after = (await repo.GetByIdAsync(invoice.Id))!;
        after.Status.ShouldBe(InvoiceStatus.PrimerRecordatorio);
        after.LastStatusTransitionAt.ShouldBeGreaterThanOrEqualTo(before.LastStatusTransitionAt);
        after.UpdatedAt.ShouldBeGreaterThanOrEqualTo(before.UpdatedAt);
    }

    [Fact]
    public async Task UpdateStatusAsync_DoesNotAffectOtherInvoicesOrFields()
    {
        var repo = await _fixture.CreateCleanRepositoryAsync();
        var target = InvoiceTestFactory.Create("c1", amount: 100m, status: InvoiceStatus.Pending);
        var other = InvoiceTestFactory.Create("c2", amount: 250m, status: InvoiceStatus.Pending);
        await repo.AddAsync(target);
        await repo.AddAsync(other);

        await repo.UpdateStatusAsync(target.Id, InvoiceStatus.Pagado);

        // La otra factura permanece intacta.
        var untouched = (await repo.GetByIdAsync(other.Id))!;
        untouched.Status.ShouldBe(InvoiceStatus.Pending);

        // La factura objetivo conserva los campos no relacionados con el estado.
        var updated = (await repo.GetByIdAsync(target.Id))!;
        updated.Status.ShouldBe(InvoiceStatus.Pagado);
        updated.Amount.ShouldBe(100m);
        updated.ClientId.ShouldBe("c1");
        updated.RemindersCount.ShouldBe(target.RemindersCount);
    }

    [Fact]
    public async Task UpdateStatusAsync_IsNoOp_WhenIdNotFound()
    {
        var repo = await _fixture.CreateCleanRepositoryAsync();
        await repo.AddAsync(InvoiceTestFactory.Create("c1", status: InvoiceStatus.Pending));

        // No debe lanzar ni modificar documentos.
        await repo.UpdateStatusAsync("id-inexistente", InvoiceStatus.Pagado);

        var pendientes = (await repo.GetByStatusAsync(InvoiceStatus.Pending)).ToList();
        pendientes.Count.ShouldBe(1);
        (await repo.GetByStatusAsync(InvoiceStatus.Pagado)).ShouldBeEmpty();
    }
}
