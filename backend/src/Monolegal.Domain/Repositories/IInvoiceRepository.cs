using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Monolegal.Domain.Entities;

namespace Monolegal.Domain.Repositories;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Invoice>> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default);
    Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default);
    Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default);
}
