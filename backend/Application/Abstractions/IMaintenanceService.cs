using System.Threading;
using System.Threading.Tasks;

namespace Backend.Application.Abstractions;

/// <summary>Resultado de "Eliminar todos los datos" (zona de peligro).</summary>
/// <param name="DeletedInvoices">Cantidad de facturas eliminadas.</param>
public sealed record DeleteAllDataResult(long DeletedInvoices);

/// <summary>Resultado de "Flush DB" (zona de peligro): la base se vacía y se vuelve a sembrar.</summary>
/// <param name="DeletedInvoices">Facturas eliminadas antes del re-sembrado.</param>
/// <param name="Seeded"><c>true</c> si el sembrador insertó datos tras el vaciado.</param>
/// <param name="ClientsCreated">Clientes distintos creados por el sembrador.</param>
/// <param name="InvoicesCreated">Facturas creadas por el sembrador.</param>
public sealed record FlushDatabaseResult(long DeletedInvoices, bool Seeded, int ClientsCreated, int InvoicesCreated);

/// <summary>
/// Operaciones destructivas de mantenimiento expuestas en la "zona de peligro" de
/// <c>/configuracion</c>. Son irreversibles y deben invocarse sólo tras confirmación explícita
/// del usuario. No exponen secretos.
/// </summary>
public interface IMaintenanceService
{
    /// <summary>
    /// Elimina todos los registros de negocio (facturas), conservando la base de datos y la
    /// configuración del sistema. Devuelve el conteo eliminado.
    /// </summary>
    Task<DeleteAllDataResult> DeleteAllDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Vacía por completo la base de datos (incluida la configuración del sistema), reconstruye
    /// los índices y ejecuta nuevamente el sembrador de datos. Devuelve el resultado del re-sembrado.
    /// </summary>
    Task<FlushDatabaseResult> FlushDatabaseAsync(CancellationToken cancellationToken = default);
}
