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
    /// Atomically updates only the status-related fields of an invoice:
    /// <see cref="Entities.Invoice.Status"/>, <see cref="Entities.Invoice.UpdatedAt"/>
    /// and <see cref="Entities.Invoice.LastStatusTransitionAt"/>. The rest of the document
    /// (Amount, ClientId, RemindersCount, ...) is left untouched. A non-existent id is a no-op.
    /// </summary>
    Task UpdateStatusAsync(string id, InvoiceStatus newStatus, CancellationToken cancellationToken = default);
}
