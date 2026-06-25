using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;
using Monolegal.Domain.Repositories;

namespace Backend.Tests.Infrastructure.Support;

/// <summary>
/// Decorador de <see cref="IInvoiceRepository"/> que lanza una excepción al intentar
/// actualizar (<see cref="UpdateAsync"/>) una factura con un id determinado. Sirve para
/// verificar el aislamiento de errores por factura del worker: un fallo en una factura
/// no debe abortar el procesamiento del resto del lote (spec 012, FR-007, SC-004).
/// </summary>
public sealed class ThrowingInvoiceRepository : IInvoiceRepository
{
    private readonly IInvoiceRepository _inner;
    private readonly string _failingInvoiceId;

    public ThrowingInvoiceRepository(IInvoiceRepository inner, string failingInvoiceId)
    {
        _inner = inner;
        _failingInvoiceId = failingInvoiceId;
    }

    public Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        if (invoice.Id == _failingInvoiceId)
            throw new InvalidOperationException(
                $"Fallo simulado al actualizar la factura '{invoice.Id}'.");

        return _inner.UpdateAsync(invoice, cancellationToken);
    }

    // ── Delegación directa del resto del contrato ──────────────────────────────
    public Task<Invoice?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => _inner.GetByIdAsync(id, cancellationToken);

    public Task<IEnumerable<Invoice>> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
        => _inner.GetByClientIdAsync(clientId, cancellationToken);

    public Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
        => _inner.AddAsync(invoice, cancellationToken);

    public Task<IEnumerable<Invoice>> GetTransitionableAsync(CancellationToken cancellationToken = default)
        => _inner.GetTransitionableAsync(cancellationToken);

    public Task<IEnumerable<Invoice>> GetByStatusAsync(InvoiceStatus status, CancellationToken cancellationToken = default)
        => _inner.GetByStatusAsync(status, cancellationToken);

    public Task<long> CountAsync(CancellationToken cancellationToken = default)
        => _inner.CountAsync(cancellationToken);

    public Task UpdateStatusAsync(string id, InvoiceStatus newStatus, CancellationToken cancellationToken = default)
        => _inner.UpdateStatusAsync(id, newStatus, cancellationToken);

    public Task<(IReadOnlyList<Invoice> Items, long Total)> GetPagedAsync(
        InvoiceStatus? status, int page, int pageSize, CancellationToken cancellationToken = default)
        => _inner.GetPagedAsync(status, page, pageSize, cancellationToken);

    public Task<IReadOnlyDictionary<InvoiceStatus, long>> CountByStatusAsync(CancellationToken cancellationToken = default)
        => _inner.CountByStatusAsync(cancellationToken);

    public Task<IReadOnlyDictionary<string, long>> CountByClientAsync(CancellationToken cancellationToken = default)
        => _inner.CountByClientAsync(cancellationToken);
}
