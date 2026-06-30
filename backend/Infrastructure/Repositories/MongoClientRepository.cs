using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Repositories;

namespace Monolegal.Infrastructure.Repositories;

/// <summary>
/// Implementación MongoDB de <see cref="IClientRepository"/> (spec 018). La búsqueda del listado
/// aplica una coincidencia "contains" case-insensitive sobre nombre y email (regex escapada, sin
/// inyección), ordenada por nombre ascendente, replicando el patrón de
/// <see cref="MongoInvoiceRepository.GetPagedAsync"/>.
/// </summary>
/// <remarks>
/// SOLID: DIP — implementa <see cref="IClientRepository"/>; la capa Application depende de la abstracción,
/// no de MongoDB. SRP — única responsabilidad: traducir entre el dominio y la colección <c>Clients</c>.
/// </remarks>
public sealed class MongoClientRepository : IClientRepository
{
    private readonly IMongoCollection<Client> _collection;

    public MongoClientRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<Client>("Clients");
    }

    public async Task<Client?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Client?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = Client.NormalizeEmail(email);
        return await _collection
            .Find(x => x.Email == normalized)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<(IReadOnlyList<Client> Items, long Total)> GetPagedAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        FilterDefinition<Client> filter;
        if (string.IsNullOrWhiteSpace(search))
        {
            filter = Builders<Client>.Filter.Empty;
        }
        else
        {
            // Escapamos metacaracteres para tratar la entrada como literal y aplicamos
            // "contains" case-insensitive sobre Name y Email (OR).
            var pattern = Regex.Escape(search.Trim());
            var regex = new BsonRegularExpression(pattern, "i");
            filter = Builders<Client>.Filter.Or(
                Builders<Client>.Filter.Regex(x => x.Name, regex),
                Builders<Client>.Filter.Regex(x => x.Email, regex));
        }

        var total = await _collection
            .CountDocumentsAsync(filter, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var items = await _collection
            .Find(filter)
            .SortBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (items, total);
    }

    public async Task AddAsync(Client client, CancellationToken cancellationToken = default)
    {
        await _collection
            .InsertOneAsync(client, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(Client client, CancellationToken cancellationToken = default)
    {
        await _collection
            .ReplaceOneAsync(
                x => x.Id == client.Id,
                client,
                new ReplaceOptions { IsUpsert = false },
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = await _collection
            .DeleteOneAsync(x => x.Id == id, cancellationToken)
            .ConfigureAwait(false);

        return result.DeletedCount > 0;
    }

    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _collection
            .CountDocumentsAsync(FilterDefinition<Client>.Empty, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<long> DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        var result = await _collection
            .DeleteManyAsync(FilterDefinition<Client>.Empty, cancellationToken)
            .ConfigureAwait(false);

        return result.DeletedCount;
    }
}
