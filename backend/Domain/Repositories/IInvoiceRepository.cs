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
    /// Elimina permanentemente (hard delete) una factura por su id (spec 018, RF-010). Devuelve
    /// <c>true</c> si existía y se eliminó, <c>false</c> si no había una factura con ese id.
    /// </summary>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cuenta las facturas asociadas a un cliente (spec 018, RF-018). Lo usa el borrado de cliente
    /// para impedir la eliminación de clientes con facturas asociadas.
    /// </summary>
    Task<long> CountByClientIdAsync(string clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all invoices in states that can be automatically transitioned:
    /// Pending, PrimerRecordatorio, SegundoRecordatorio.
    /// </summary>
    Task<IEnumerable<Invoice>> GetTransitionableAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<Invoice>> GetByStatusAsync(InvoiceStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all invoices whose last email notification ended with the given
    /// <paramref name="outcome"/>. Backs the global admin tools (spec 017, US4): the resend tool
    /// queries <see cref="NotificationOutcome.Failed"/> and the sanitize tool queries
    /// <see cref="NotificationOutcome.None"/>. Supported by the <c>LastNotificationOutcome</c> index.
    /// </summary>
    Task<IEnumerable<Invoice>> GetByNotificationOutcomeAsync(NotificationOutcome outcome, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the total number of invoices in the collection. Used by the
    /// development data seeder to verify the "empty database" precondition and
    /// guarantee idempotency (only seeds when the count is zero). See
    /// specs/008-seed-data-clientes/contracts/dev-data-seeder.md.
    /// </summary>
    Task<long> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes every invoice in the collection and returns the number of documents
    /// removed. Used by the maintenance "danger zone" (delete all records) to clear
    /// business data without dropping the database. Irreversible.
    /// </summary>
    Task<long> DeleteAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a page of invoices, optionally filtered by <paramref name="status"/> and/or by a
    /// case-insensitive "contains" match of <paramref name="clientSearch"/> against
    /// <see cref="Entities.Invoice.ClientId"/>, ordered by <see cref="Entities.Invoice.CreatedAt"/>
    /// descending, together with the total number of matches for the applied filters (independent
    /// of the returned page). Both filters combine with AND. A null/empty <paramref name="clientSearch"/>
    /// means "no client search". Used by GET /api/invoices. See
    /// specs/014-admin-panel-invoices/contracts/list-invoices-search.md.
    /// </summary>
    Task<(IReadOnlyList<Invoice> Items, long Total)> GetPagedAsync(
        InvoiceStatus? status, string? clientSearch, int page, int pageSize, CancellationToken cancellationToken = default);

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
