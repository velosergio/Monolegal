using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Monolegal.Domain.Entities;

namespace Monolegal.Domain.Repositories;

/// <summary>
/// Contrato de persistencia para la entidad <see cref="Client"/> (spec 018). Soporta el CRUD de
/// clientes, el listado paginado con búsqueda (RF-012/RF-013) y la verificación de unicidad de
/// email (RF-015a). La integridad referencial con facturas se valida en la capa de aplicación.
/// </summary>
public interface IClientRepository
{
    Task<Client?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Busca un cliente por email normalizado; respalda la validación de unicidad.</summary>
    Task<Client?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Devuelve una página de clientes, opcionalmente filtrada por una coincidencia "contains"
    /// case-insensitive de <paramref name="search"/> sobre nombre y email, ordenada por nombre
    /// ascendente, junto con el total de coincidencias (independiente de la página). Un
    /// <paramref name="search"/> nulo/vacío significa "sin filtro". Lo usa GET /api/clients.
    /// </summary>
    Task<(IReadOnlyList<Client> Items, long Total)> GetPagedAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default);

    Task AddAsync(Client client, CancellationToken cancellationToken = default);
    Task UpdateAsync(Client client, CancellationToken cancellationToken = default);

    /// <summary>Elimina permanentemente un cliente. Devuelve <c>true</c> si existía y se eliminó.</summary>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Número total de clientes en la colección (usado por el seeder para idempotencia).</summary>
    Task<long> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>Elimina todos los clientes y devuelve el número eliminado (zona de peligro).</summary>
    Task<long> DeleteAllAsync(CancellationToken cancellationToken = default);
}
