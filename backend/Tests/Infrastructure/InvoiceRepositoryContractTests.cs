using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;
using Monolegal.Domain.Repositories;
using Shouldly;
using Xunit;

namespace Backend.Tests.Infrastructure;

/// <summary>
/// Contract tests for IInvoiceRepository methods GetByStatusAsync and UpdateStatusAsync,
/// verified against the in-memory fake implementation.
/// </summary>
[Trait("Category", "Repository")]
public class InvoiceRepositoryContractTests
{
    // ── Minimal in-memory fake (mirrors the one in InvoiceWorkerTests) ────────

    private sealed class InMemoryInvoiceRepository : IInvoiceRepository
    {
        private readonly Dictionary<string, Invoice> _store = new();

        public Task<Invoice?> GetByIdAsync(string id, CancellationToken ct = default)
            => Task.FromResult(_store.TryGetValue(id, out var inv) ? inv : null);

        public Task<IEnumerable<Invoice>> GetByClientIdAsync(string clientId, CancellationToken ct = default)
            => Task.FromResult(_store.Values.Where(i => i.ClientId == clientId));

        public Task AddAsync(Invoice invoice, CancellationToken ct = default)
        {
            _store[invoice.Id] = invoice;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Invoice invoice, CancellationToken ct = default)
        {
            _store[invoice.Id] = invoice;
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Invoice>> GetTransitionableAsync(CancellationToken ct = default)
            => Task.FromResult(_store.Values.Where(i =>
                i.Status is InvoiceStatus.Pending
                    or InvoiceStatus.PrimerRecordatorio
                    or InvoiceStatus.SegundoRecordatorio));

        public Task<IEnumerable<Invoice>> GetByStatusAsync(InvoiceStatus status, CancellationToken ct = default)
            => Task.FromResult(_store.Values.Where(i => i.Status == status));

        public Task UpdateStatusAsync(string id, InvoiceStatus newStatus, CancellationToken ct = default)
        {
            if (_store.TryGetValue(id, out var invoice))
                invoice.UpdateStatus(newStatus);
            return Task.CompletedTask;
        }

        public Task<long> CountAsync(CancellationToken ct = default)
            => Task.FromResult((long)_store.Count);
    }

    private static async Task<IInvoiceRepository> RepoWithAsync(params Invoice[] invoices)
    {
        var repo = new InMemoryInvoiceRepository();
        foreach (var inv in invoices)
            await repo.AddAsync(inv);
        return repo;
    }

    // ── GetByStatusAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetByStatusAsync_ReturnsOnlyMatchingStatus()
    {
        var pending = new Invoice("c1", 100);
        pending.UpdateStatus(InvoiceStatus.Pending);

        var primer = new Invoice("c2", 200);
        primer.UpdateStatus(InvoiceStatus.PrimerRecordatorio);

        var repo = await RepoWithAsync(pending, primer);

        var result = (await repo.GetByStatusAsync(InvoiceStatus.Pending)).ToList();

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(pending.Id);
    }

    [Fact]
    public async Task GetByStatusAsync_ReturnsEmpty_WhenNoMatch()
    {
        var invoice = new Invoice("c1", 100);
        invoice.UpdateStatus(InvoiceStatus.Pending);
        var repo = await RepoWithAsync(invoice);

        var result = await repo.GetByStatusAsync(InvoiceStatus.Pagado);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetByStatusAsync_ReturnsMultiple_WhenSeveralMatch()
    {
        var a = new Invoice("c1", 100);
        a.UpdateStatus(InvoiceStatus.PrimerRecordatorio);
        var b = new Invoice("c2", 200);
        b.UpdateStatus(InvoiceStatus.PrimerRecordatorio);
        var repo = await RepoWithAsync(a, b);

        var result = (await repo.GetByStatusAsync(InvoiceStatus.PrimerRecordatorio)).ToList();

        result.Count.ShouldBe(2);
    }

    // ── UpdateStatusAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatusAsync_ChangesStatusOfTargetInvoice()
    {
        var invoice = new Invoice("c1", 100);
        invoice.UpdateStatus(InvoiceStatus.Pending);
        var repo = await RepoWithAsync(invoice);

        await repo.UpdateStatusAsync(invoice.Id, InvoiceStatus.PrimerRecordatorio);

        var updated = await repo.GetByIdAsync(invoice.Id);
        updated!.Status.ShouldBe(InvoiceStatus.PrimerRecordatorio);
    }

    [Fact]
    public async Task UpdateStatusAsync_DoesNotAffectOtherInvoices()
    {
        var target = new Invoice("c1", 100);
        target.UpdateStatus(InvoiceStatus.Pending);
        var other = new Invoice("c2", 200);
        other.UpdateStatus(InvoiceStatus.Pending);
        var repo = await RepoWithAsync(target, other);

        await repo.UpdateStatusAsync(target.Id, InvoiceStatus.Pagado);

        var untouched = await repo.GetByIdAsync(other.Id);
        untouched!.Status.ShouldBe(InvoiceStatus.Pending);
    }

    [Fact]
    public async Task UpdateStatusAsync_IsNoOp_WhenIdNotFound()
    {
        var repo = await RepoWithAsync();

        // Should not throw
        await repo.UpdateStatusAsync("nonexistent-id", InvoiceStatus.Pagado);
    }
}
