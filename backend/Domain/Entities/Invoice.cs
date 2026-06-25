using System;
using System.Runtime.CompilerServices;
using Monolegal.Domain.Enums;

// Permite que los proyectos de tests accedan a los miembros internos
[assembly: InternalsVisibleTo("Monolegal.Domain.Tests")]
[assembly: InternalsVisibleTo("Tests")]

namespace Monolegal.Domain.Entities;

public class Invoice
{
    public string Id { get; private set; }
    public string ClientId { get; private set; }
    public decimal Amount { get; private set; }
    public InvoiceStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public int RemindersCount { get; private set; }
    public DateTime? LastReminderSentAt { get; private set; }
    public DateTime LastStatusTransitionAt { get; private set; }

    // Último resultado de notificación por correo asociado a una transición (spec 013).
    public NotificationType? LastNotificationType { get; private set; }
    public NotificationOutcome LastNotificationOutcome { get; private set; }
    public DateTime? LastNotificationAt { get; private set; }
    public string? LastNotificationError { get; private set; }

    public Invoice(string clientId, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId cannot be null or empty.", nameof(clientId));

        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.", nameof(amount));

        Id = Guid.NewGuid().ToString("N");
        ClientId = clientId;
        Amount = amount;
        Status = InvoiceStatus.Draft;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
        LastStatusTransitionAt = CreatedAt;
        RemindersCount = 0;
        LastReminderSentAt = null;
        LastNotificationType = null;
        LastNotificationOutcome = NotificationOutcome.None;
        LastNotificationAt = null;
        LastNotificationError = null;
    }

    public void UpdateStatus(InvoiceStatus newStatus)
    {
        Status = newStatus;
        LastStatusTransitionAt = DateTime.UtcNow;
        UpdateAuditDate();
    }

    public void RecordReminderSent()
    {
        RemindersCount++;
        LastReminderSentAt = DateTime.UtcNow;
        UpdateAuditDate();
    }

    /// <summary>
    /// Registra el resultado del último intento de notificación por correo asociado a una
    /// transición de estado (spec 013). No modifica el estado de la factura ni los contadores
    /// de recordatorio; esos efectos se manejan por separado (<see cref="RecordReminderSent"/>).
    /// </summary>
    /// <param name="type">Tipo de notificación intentada.</param>
    /// <param name="outcome">Resultado del intento (Sent/Skipped/Failed).</param>
    /// <param name="at">Momento del intento.</param>
    /// <param name="error">Motivo resumido del fallo; sólo no nulo cuando <paramref name="outcome"/> es Failed.</param>
    public void RecordNotificationResult(
        NotificationType? type,
        NotificationOutcome outcome,
        DateTime at,
        string? error = null)
    {
        LastNotificationType = type;
        LastNotificationOutcome = outcome;
        LastNotificationAt = at;
        LastNotificationError = outcome == NotificationOutcome.Failed ? error : null;
        UpdateAuditDate();
    }

    /// <summary>
    /// Permite sobrescribir <see cref="LastStatusTransitionAt"/> para pruebas deterministas.
    /// Sólo accesible desde el ensamblado de tests de dominio (InternalsVisibleTo).
    /// </summary>
    internal void OverrideLastStatusTransitionAt(DateTime dateTime)
    {
        LastStatusTransitionAt = dateTime;
    }

    /// <summary>
    /// Permite sobrescribir <see cref="CreatedAt"/> para pruebas deterministas de orden
    /// (p. ej. paginación por fecha de creación). Sólo accesible desde los ensamblados de tests.
    /// </summary>
    internal void OverrideCreatedAt(DateTime dateTime)
    {
        CreatedAt = dateTime;
    }

    private void UpdateAuditDate()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}
