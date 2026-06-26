using System.Threading;
using System.Threading.Tasks;

namespace Backend.Application.Abstractions;

/// <summary>Resultado del reenvío masivo de notificaciones fallidas (spec 017, US4).</summary>
/// <param name="Attempted">Facturas en estado <c>Failed</c> consideradas.</param>
/// <param name="Resent">Reenvíos exitosos (<c>Failed → Sent</c>).</param>
/// <param name="Failed">Reintentos que volvieron a fallar (<c>Failed → Failed</c>).</param>
public sealed record ResendFailedResult(int Attempted, int Resent, int Failed);

/// <summary>Resultado del saneamiento de notificaciones atascadas (spec 017, US4).</summary>
/// <param name="Sanitized">Facturas marcadas <c>None → Failed</c>.</param>
public sealed record SanitizeResult(int Sanitized);

/// <summary>
/// Herramientas globales de administración de envíos (spec 017, US4). Operan sobre el estado de
/// notificación embebido en <c>Invoice</c>: reenvío masivo de fallidos y saneamiento de atascados.
/// Ambas son fail-soft (aíslan el fallo por factura) y nunca exponen secretos.
/// </summary>
public interface IEmailAdminService
{
    /// <summary>
    /// Reenvía la notificación correspondiente al estado actual de cada factura con
    /// <c>LastNotificationOutcome == Failed</c>. Devuelve los conteos del lote.
    /// </summary>
    Task<ResendFailedResult> ResendFailedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca como <c>Failed</c> las facturas en estado notificable con
    /// <c>LastNotificationOutcome == None</c> (conservando el registro, sin reintentar ni borrar).
    /// </summary>
    Task<SanitizeResult> SanitizeStuckAsync(CancellationToken cancellationToken = default);
}
