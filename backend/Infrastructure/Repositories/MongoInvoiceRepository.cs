using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;
using Monolegal.Domain.Repositories;

namespace Monolegal.Infrastructure.Repositories;

/// <summary>
/// MongoDB implementation of <see cref="IInvoiceRepository"/>.
/// </summary>
public sealed class MongoInvoiceRepository : IInvoiceRepository
{
    private readonly IMongoCollection<Invoice> _collection;

    public MongoInvoiceRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<Invoice>("Invoices");
    }

    public async Task<Invoice?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Invoice>> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(x => x.ClientId == clientId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        await _collection
            .InsertOneAsync(invoice, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        await _collection
            .ReplaceOneAsync(
                x => x.Id == invoice.Id,
                invoice,
                new ReplaceOptions { IsUpsert = false },
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Returns all invoices in states eligible for automatic time-based transition:
    /// <see cref="InvoiceStatus.Pending"/>, <see cref="InvoiceStatus.PrimerRecordatorio"/>,
    /// <see cref="InvoiceStatus.SegundoRecordatorio"/>.
    /// </summary>
    public async Task<IEnumerable<Invoice>> GetTransitionableAsync(CancellationToken cancellationToken = default)
    {
        var transitionableStatuses = new[]
        {
            InvoiceStatus.Pending,
            InvoiceStatus.PrimerRecordatorio,
            InvoiceStatus.SegundoRecordatorio
        };

        return await _collection
            .Find(x => transitionableStatuses.Contains(x.Status))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Invoice>> GetByStatusAsync(InvoiceStatus status, CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(x => x.Status == status)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _collection
            .CountDocumentsAsync(FilterDefinition<Invoice>.Empty, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateStatusAsync(string id, InvoiceStatus newStatus, CancellationToken cancellationToken = default)
    {
        var update = Builders<Invoice>.Update
            .Set(x => x.Status, newStatus)
            .Set(x => x.UpdatedAt, DateTime.UtcNow)
            .Set(x => x.LastStatusTransitionAt, DateTime.UtcNow);

        await _collection
            .UpdateOneAsync(x => x.Id == id, update, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }
}
