using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;

namespace Monolegal.Domain.Repositories;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Invoice>> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default);
    Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default);
    Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all invoices in states that can be automatically transitioned:
    /// Pending, PrimerRecordatorio, SegundoRecordatorio.
    /// </summary>
    Task<IEnumerable<Invoice>> GetTransitionableAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<Invoice>> GetByStatusAsync(InvoiceStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the total number of invoices in the collection. Used by the
    /// development data seeder to verify the "empty database" precondition and
    /// guarantee idempotency (only seeds when the count is zero). See
    /// specs/008-seed-data-clientes/contracts/dev-data-seeder.md.
    /// </summary>
    Task<long> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically updates only the status-related fields of an invoice:
    /// <see cref="Entities.Invoice.Status"/>, <see cref="Entities.Invoice.UpdatedAt"/>
    /// and <see cref="Entities.Invoice.LastStatusTransitionAt"/>. The rest of the document
    /// (Amount, ClientId, RemindersCount, ...) is left untouched. A non-existent id is a no-op.
    /// </summary>
    Task UpdateStatusAsync(string id, InvoiceStatus newStatus, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a page of invoices, optionally filtered by <paramref name="status"/>, ordered by
    /// <see cref="Entities.Invoice.CreatedAt"/> descending, together with the total number of
    /// matches for the applied filter (independent of the returned page). Used by
    /// GET /api/invoices. See specs/009-invoice-api-endpoints/contracts/list-invoices.md.
    /// </summary>
    Task<(IReadOnlyList<Invoice> Items, long Total)> GetPagedAsync(
        InvoiceStatus? status, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the count of invoices grouped by status across the whole collection.
    /// Used by GET /api/invoices/stats (byStatus aggregate).
    /// </summary>
    Task<IReadOnlyDictionary<InvoiceStatus, long>> CountByStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the count of invoices grouped by client id across the whole collection.
    /// Used by GET /api/invoices/stats (byClient aggregate).
    /// </summary>
    Task<IReadOnlyDictionary<string, long>> CountByClientAsync(CancellationToken cancellationToken = default);
}
