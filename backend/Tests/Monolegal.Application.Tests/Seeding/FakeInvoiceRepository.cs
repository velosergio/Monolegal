using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;
using Monolegal.Domain.Repositories;

namespace Backend.Tests.Monolegal.Application.Tests.Seeding;

/// <summary>
/// Repositorio de facturas en memoria para los tests unitarios del seeder (spec 008).
/// Permite inspeccionar las facturas insertadas y el número de llamadas a <c>AddAsync</c>.
/// </summary>
internal sealed class FakeInvoiceRepository : IInvoiceRepository
{
    private readonly List<Invoice> _store = new();

    public IReadOnlyList<Invoice> Added => _store;
    public int AddCallCount { get; private set; }

    public Task<Invoice?> GetByIdAsync(string id, CancellationToken ct = default)
        => Task.FromResult(_store.FirstOrDefault(i => i.Id == id));

    public Task<IEnumerable<Invoice>> GetByClientIdAsync(string clientId, CancellationToken ct = default)
        => Task.FromResult(_store.Where(i => i.ClientId == clientId));

    public Task AddAsync(Invoice invoice, CancellationToken ct = default)
    {
        AddCallCount++;
        _store.Add(invoice);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Invoice invoice, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<IEnumerable<Invoice>> GetTransitionableAsync(CancellationToken ct = default)
        => Task.FromResult(_store.Where(i =>
            i.Status is InvoiceStatus.Pending
                or InvoiceStatus.PrimerRecordatorio
                or InvoiceStatus.SegundoRecordatorio));

    public Task<IEnumerable<Invoice>> GetByStatusAsync(InvoiceStatus status, CancellationToken ct = default)
        => Task.FromResult(_store.Where(i => i.Status == status));

    public Task<long> CountAsync(CancellationToken ct = default)
        => Task.FromResult((long)_store.Count);

    public Task<(IReadOnlyList<Invoice> Items, long Total)> GetPagedAsync(
        InvoiceStatus? status, string? clientSearch, int page, int pageSize, CancellationToken ct = default)
    {
        var query = status.HasValue ? _store.Where(i => i.Status == status.Value) : _store.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(clientSearch))
        {
            var term = clientSearch.Trim();
            query = query.Where(i => i.ClientId.Contains(term, System.StringComparison.OrdinalIgnoreCase));
        }
        var filtered = query.ToList();
        var items = filtered
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        return Task.FromResult(((IReadOnlyList<Invoice>)items, (long)filtered.Count));
    }

    public Task<IReadOnlyDictionary<InvoiceStatus, long>> CountByStatusAsync(CancellationToken ct = default)
        => Task.FromResult((IReadOnlyDictionary<InvoiceStatus, long>)_store
            .GroupBy(i => i.Status).ToDictionary(g => g.Key, g => (long)g.Count()));

    public Task<IReadOnlyDictionary<string, long>> CountByClientAsync(CancellationToken ct = default)
        => Task.FromResult((IReadOnlyDictionary<string, long>)_store
            .GroupBy(i => i.ClientId).ToDictionary(g => g.Key, g => (long)g.Count()));
}
