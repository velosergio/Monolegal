namespace Monolegal.Domain.Enums;

/// <summary>
/// Resultado del último intento de notificación por correo asociado a una transición
/// de estado de la factura (spec 013, data-model.md).
/// </summary>
public enum NotificationOutcome
{
    /// <summary>Aún no se ha intentado notificar, o el estado no tiene plantilla aplicable.</summary>
    None = 0,

    /// <summary>El envío se realizó con éxito.</summary>
    Sent = 1,

    /// <summary>No se intentó el envío (sin plantilla para el estado o sin correo de destinatario).</summary>
    Skipped = 2,

    /// <summary>Se intentó el envío y falló (proveedor caído, correo inválido, excepción).</summary>
    Failed = 3
}
