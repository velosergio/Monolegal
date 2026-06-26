using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;
using Monolegal.Domain.Repositories;

namespace Backend.Tests.Infrastructure.Support;

/// <summary>
/// Implementación en memoria de <see cref="IInvoiceRepository"/> compartida por los tests
/// de aplicación (spec 009). Centraliza el fake para que la adición de métodos al contrato
/// se refleje en un único lugar.
/// </summary>
public sealed class InMemoryInvoiceRepository : IInvoiceRepository
{
    private readonly Dictionary<string, Invoice> _store = new();

    public IReadOnlyCollection<Invoice> All => _store.Values.ToList().AsReadOnly();

    public Task<Invoice?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.TryGetValue(id, out var invoice) ? invoice : null);

    public Task<IEnumerable<Invoice>> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.Values.Where(i => i.ClientId == clientId));

    public Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        _store[invoice.Id] = invoice;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        _store[invoice.Id] = invoice;
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Invoice>> GetTransitionableAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_store.Values.Where(i =>
            i.Status is InvoiceStatus.Pending
                or InvoiceStatus.PrimerRecordatorio
                or InvoiceStatus.SegundoRecordatorio));

    public Task<IEnumerable<Invoice>> GetByStatusAsync(InvoiceStatus status, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.Values.Where(i => i.Status == status));

    public Task<long> CountAsync(CancellationToken cancellationToken = default)
        => Task.FromResult((long)_store.Count);

    public Task UpdateStatusAsync(string id, InvoiceStatus newStatus, CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(id, out var invoice))
            invoice.UpdateStatus(newStatus);
        return Task.CompletedTask;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Métodos añadidos por la spec 009 (listado paginado y agregaciones)
    // ──────────────────────────────────────────────────────────────────────────

    public Task<(IReadOnlyList<Invoice> Items, long Total)> GetPagedAsync(
        InvoiceStatus? status, string? clientSearch, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _store.Values.AsEnumerable();
        if (status.HasValue)
            query = query.Where(i => i.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(clientSearch))
        {
            var term = clientSearch.Trim();
            query = query.Where(i => i.ClientId.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var filtered = query.ToList();
        long total = filtered.Count;

        var items = filtered
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(((IReadOnlyList<Invoice>)items, total));
    }

    public Task<IReadOnlyDictionary<InvoiceStatus, long>> CountByStatusAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyDictionary<InvoiceStatus, long> result = _store.Values
            .GroupBy(i => i.Status)
            .ToDictionary(g => g.Key, g => (long)g.Count());
        return Task.FromResult(result);
    }

    public Task<IReadOnlyDictionary<string, long>> CountByClientAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyDictionary<string, long> result = _store.Values
            .GroupBy(i => i.ClientId)
            .ToDictionary(g => g.Key, g => (long)g.Count());
        return Task.FromResult(result);
    }
}
