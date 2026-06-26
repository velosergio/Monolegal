using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
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

    public async Task<IEnumerable<Invoice>> GetByNotificationOutcomeAsync(NotificationOutcome outcome, CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(x => x.LastNotificationOutcome == outcome)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _collection
            .CountDocumentsAsync(FilterDefinition<Invoice>.Empty, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<(IReadOnlyList<Invoice> Items, long Total)> GetPagedAsync(
        InvoiceStatus? status, string? clientSearch, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<Invoice>>();

        if (status.HasValue)
            filters.Add(Builders<Invoice>.Filter.Eq(x => x.Status, status.Value));

        if (!string.IsNullOrWhiteSpace(clientSearch))
        {
            // Escapamos los metacaracteres para tratar la entrada como literal (sin inyección de regex)
            // y aplicamos coincidencia "contains" case-insensitive sobre ClientId.
            var pattern = Regex.Escape(clientSearch.Trim());
            filters.Add(Builders<Invoice>.Filter.Regex(
                x => x.ClientId, new BsonRegularExpression(pattern, "i")));
        }

        var filter = filters.Count > 0
            ? Builders<Invoice>.Filter.And(filters)
            : Builders<Invoice>.Filter.Empty;

        var total = await _collection
            .CountDocumentsAsync(filter, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var items = await _collection
            .Find(filter)
            .SortByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (items, total);
    }

    public async Task<IReadOnlyDictionary<InvoiceStatus, long>> CountByStatusAsync(CancellationToken cancellationToken = default)
    {
        var results = await _collection
            .Aggregate()
            .Group(x => x.Status, g => new { Status = g.Key, Count = g.LongCount() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return results.ToDictionary(r => r.Status, r => r.Count);
    }

    public async Task<IReadOnlyDictionary<string, long>> CountByClientAsync(CancellationToken cancellationToken = default)
    {
        var results = await _collection
            .Aggregate()
            .Group(x => x.ClientId, g => new { ClientId = g.Key, Count = g.LongCount() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return results.ToDictionary(r => r.ClientId, r => r.Count);
    }
}
