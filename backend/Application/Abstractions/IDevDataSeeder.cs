using System.Threading;
using System.Threading.Tasks;
using Backend.Application.Seeding;

namespace Backend.Application.Abstractions;

/// <summary>
/// Sembrador de datos de desarrollo. Crea un conjunto mínimo, predecible y representativo
/// (3 clientes, 8 facturas) <b>únicamente</b> cuando la base de datos está vacía respecto a
/// facturas. Idempotente: si ya existen datos, omite la siembra sin modificar registros.
/// Ver specs/008-seed-data-clientes/contracts/dev-data-seeder.md.
/// </summary>
public interface IDevDataSeeder
{
    /// <summary>
    /// Ejecuta la siembra si la colección de facturas está vacía. Devuelve un
    /// <see cref="SeedResult"/> observable indicando si sembró u omitió y los conteos resultantes.
    /// </summary>
    Task<SeedResult> SeedAsync(CancellationToken cancellationToken = default);
}
