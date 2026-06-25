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

    public Task UpdateStatusAsync(string id, InvoiceStatus newStatus, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<long> CountAsync(CancellationToken ct = default)
        => Task.FromResult((long)_store.Count);
}
