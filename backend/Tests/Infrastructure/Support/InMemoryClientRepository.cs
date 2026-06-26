using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Repositories;

namespace Backend.Tests.Infrastructure.Support;

/// <summary>
/// Implementación en memoria de <see cref="IClientRepository"/> para los tests de la spec 018.
/// Replica la semántica de búsqueda (contains case-insensitive sobre nombre/email), orden por
/// nombre y paginación de la implementación MongoDB.
/// </summary>
public sealed class InMemoryClientRepository : IClientRepository
{
    private readonly Dictionary<string, Client> _store = new();

    public IReadOnlyCollection<Client> All => _store.Values.ToList().AsReadOnly();

    public Task<Client?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.TryGetValue(id, out var c) ? c : null);

    public Task<Client?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = Client.NormalizeEmail(email);
        return Task.FromResult(_store.Values.FirstOrDefault(c => c.Email == normalized));
    }

    public Task<(IReadOnlyList<Client> Items, long Total)> GetPagedAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _store.Values.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c =>
                c.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || c.Email.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var filtered = query.ToList();
        long total = filtered.Count;
        var items = filtered
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(((IReadOnlyList<Client>)items, total));
    }

    public Task AddAsync(Client client, CancellationToken cancellationToken = default)
    {
        _store[client.Id] = client;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Client client, CancellationToken cancellationToken = default)
    {
        _store[client.Id] = client;
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.Remove(id));

    public Task<long> CountAsync(CancellationToken cancellationToken = default)
        => Task.FromResult((long)_store.Count);

    public Task<long> DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        var removed = (long)_store.Count;
        _store.Clear();
        return Task.FromResult(removed);
    }
}
