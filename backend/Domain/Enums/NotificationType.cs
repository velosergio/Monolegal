namespace Monolegal.Domain.Enums;

/// <summary>
/// Tipo de notificación por correo enviada al cliente según el nuevo estado de la factura
/// (spec 013, data-model.md).
/// </summary>
public enum NotificationType
{
    /// <summary>Recordatorio de cobro (estados PrimerRecordatorio / SegundoRecordatorio).</summary>
    Reminder = 0,

    /// <summary>Confirmación de pago (estado Pagado).</summary>
    PaymentConfirmation = 1,

    /// <summary>Aviso de desactivación / última notificación (estado Desactivado).</summary>
    DeactivationNotice = 2
}
