namespace Monolegal.Domain.Enums;

/// <summary>
/// Origen de un cambio de estado de una factura (spec 015).
/// </summary>
public enum StatusChangeSource
{
    /// <summary>Transición aplicada automáticamente por el worker (basada en tiempo).</summary>
    Automatic = 0,

    /// <summary>Transición/pago solicitado manualmente por un administrador.</summary>
    Manual = 1
}
