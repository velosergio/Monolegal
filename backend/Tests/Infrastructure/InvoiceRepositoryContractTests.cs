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
/// Contract tests for IInvoiceRepository method GetByStatusAsync,
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

        public Task<IEnumerable<Invoice>> GetByNotificationOutcomeAsync(NotificationOutcome outcome, CancellationToken ct = default)
            => Task.FromResult(_store.Values.Where(i => i.LastNotificationOutcome == outcome));

        public Task<long> CountAsync(CancellationToken ct = default)
            => Task.FromResult((long)_store.Count);

        public Task<long> DeleteAllAsync(CancellationToken ct = default)
        {
            var removed = (long)_store.Count;
            _store.Clear();
            return Task.FromResult(removed);
        }

        public Task<bool> DeleteAsync(string id, CancellationToken ct = default)
            => Task.FromResult(_store.Remove(id));

        public Task<long> CountByClientIdAsync(string clientId, CancellationToken ct = default)
            => Task.FromResult((long)_store.Values.Count(i => i.ClientId == clientId));

        public Task<(IReadOnlyList<Invoice> Items, long Total)> GetShipmentsPagedAsync(
            NotificationOutcome? sendStatus, IReadOnlyCollection<string>? clientIds, int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult(((IReadOnlyList<Invoice>)System.Array.Empty<Invoice>(), 0L));

        public Task<(IReadOnlyList<Invoice> Items, long Total)> GetPagedAsync(
            InvoiceStatus? status, string? clientSearch, int page, int pageSize, CancellationToken ct = default)
        {
            var query = status.HasValue ? _store.Values.Where(i => i.Status == status.Value) : _store.Values.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(clientSearch))
                query = query.Where(i => i.ClientId.Contains(clientSearch.Trim(), System.StringComparison.OrdinalIgnoreCase));
            var filtered = query.ToList();
            var items = filtered
                .OrderByDescending(i => i.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            return Task.FromResult(((IReadOnlyList<Invoice>)items, (long)filtered.Count));
        }

        public Task<IReadOnlyDictionary<InvoiceStatus, long>> CountByStatusAsync(CancellationToken ct = default)
            => Task.FromResult((IReadOnlyDictionary<InvoiceStatus, long>)_store.Values
                .GroupBy(i => i.Status).ToDictionary(g => g.Key, g => (long)g.Count()));

        public Task<IReadOnlyDictionary<string, long>> CountByClientAsync(CancellationToken ct = default)
            => Task.FromResult((IReadOnlyDictionary<string, long>)_store.Values
                .GroupBy(i => i.ClientId).ToDictionary(g => g.Key, g => (long)g.Count()));
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
}
