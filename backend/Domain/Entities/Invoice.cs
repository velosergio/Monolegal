using System;
using System.Collections.Generic;
using System.Linq;
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

    /// <summary>
    /// Monto total de la factura. Derivado (spec 018, RF-011): siempre igual a la suma de los
    /// subtotales de <see cref="Items"/>. No se ingresa de forma independiente.
    /// </summary>
    public decimal Amount { get; private set; }

    /// <summary>Líneas de detalle de la factura (spec 018). Siempre contiene al menos una.</summary>
    public List<InvoiceItem> Items { get; private set; }

    /// <summary>Fecha de vencimiento de la factura (spec 018).</summary>
    public DateTime DueDate { get; private set; }

    public InvoiceStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public int RemindersCount { get; private set; }
    public DateTime? LastReminderSentAt { get; private set; }
    public DateTime LastStatusTransitionAt { get; private set; }

    // Historial completo de cambios de estado, embebido en el documento (spec 015).
    // Se *appendea* en cada llamada a UpdateStatus; es la única vía de cambio de estado.
    public List<StatusChange> StatusHistory { get; private set; }

    // Último resultado de notificación por correo asociado a una transición (spec 013).
    public NotificationType? LastNotificationType { get; private set; }
    public NotificationOutcome LastNotificationOutcome { get; private set; }
    public DateTime? LastNotificationAt { get; private set; }
    public string? LastNotificationError { get; private set; }

    /// <summary>
    /// Reintentos del aviso vigente (intentos de notificación posteriores al primero) — spec 019.
    /// Se reinicia a 0 al entrar en un nuevo estado notificable (ver <see cref="UpdateStatus"/>);
    /// lo incrementan los reenvíos manuales (POST /resend) y masivos (resend-failed) vía
    /// <see cref="RecordNotificationRetry"/>. El primer intento de la transición no lo modifica.
    /// </summary>
    public int NotificationRetryCount { get; private set; }

    /// <summary>
    /// Estados terminales a efectos de edición (spec 018, RF-004a): una factura en estos estados
    /// no admite modificación de sus campos (cliente, items, vencimiento).
    /// </summary>
    public bool IsTerminal => Status is InvoiceStatus.Pagado or InvoiceStatus.Desactivado;

    /// <summary>
    /// Constructor de compatibilidad (preexistente, spec 005/008): crea una factura con un único
    /// item sintético de importe igual a <paramref name="amount"/> y un vencimiento por defecto a
    /// 30 días, de modo que el invariante <c>Amount == Σ Items.Subtotal</c> se cumpla siempre.
    /// Lo usan el sembrador, ciertos tests y el deserializador de MongoDB; las altas de la spec 018
    /// usan <see cref="Create(string, IReadOnlyList{InvoiceItem}, DateTime)"/>.
    /// </summary>
    public Invoice(string clientId, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId cannot be null or empty.", nameof(clientId));

        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.", nameof(amount));

        Id = Guid.NewGuid().ToString("N");
        ClientId = clientId;
        Amount = amount;
        Items = new List<InvoiceItem> { new("Concepto", 1m, amount) };
        // Estado inicial del conjunto activo (spec 015, FR-031): se retiraron los estados legacy.
        Status = InvoiceStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
        DueDate = CreatedAt.AddDays(30);
        LastStatusTransitionAt = CreatedAt;
        StatusHistory = new List<StatusChange>();
        RemindersCount = 0;
        LastReminderSentAt = null;
        LastNotificationType = null;
        LastNotificationOutcome = NotificationOutcome.None;
        LastNotificationAt = null;
        LastNotificationError = null;
        NotificationRetryCount = 0;
    }

    /// <summary>
    /// Crea una factura nueva (spec 018, RF-001) a partir de sus líneas de detalle y su fecha de
    /// vencimiento. El monto se deriva de la suma de subtotales (RF-011).
    /// </summary>
    public static Invoice Create(string clientId, IReadOnlyList<InvoiceItem> items, DateTime dueDate)
    {
        if (items is null || items.Count == 0)
            throw new ArgumentException("La factura debe tener al menos una línea de detalle.", nameof(items));

        var invoice = new Invoice(clientId, items.Sum(i => i.Subtotal));
        invoice.Items = new List<InvoiceItem>(items);
        invoice.DueDate = dueDate;
        return invoice;
    }

    /// <summary>
    /// Edita los campos no controlados por el ciclo de estado (cliente, items, vencimiento) y
    /// recalcula el monto (spec 018, RF-003/RF-011). Lanza <see cref="InvalidOperationException"/>
    /// si la factura está en estado terminal (RF-004a).
    /// </summary>
    public void UpdateDetails(string clientId, IReadOnlyList<InvoiceItem> items, DateTime dueDate)
    {
        if (IsTerminal)
            throw new InvalidOperationException(
                "No se puede editar una factura en estado terminal (pagado/desactivado).");

        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId cannot be null or empty.", nameof(clientId));

        if (items is null || items.Count == 0)
            throw new ArgumentException("La factura debe tener al menos una línea de detalle.", nameof(items));

        ClientId = clientId;
        Items = new List<InvoiceItem>(items);
        Amount = items.Sum(i => i.Subtotal);
        DueDate = dueDate;
        UpdateAuditDate();
    }

    /// <summary>
    /// Cambia el estado de la factura, actualiza las marcas de auditoría y registra el evento
    /// en <see cref="StatusHistory"/> (spec 015). Es la **única vía** de cambio de estado del
    /// dominio, de modo que el historial nunca se desincroniza del estado actual (FR-029).
    /// </summary>
    /// <param name="newStatus">Estado destino.</param>
    /// <param name="source">
    /// Origen del cambio. Por defecto <see cref="StatusChangeSource.Manual"/>; el worker pasa
    /// <see cref="StatusChangeSource.Automatic"/> explícitamente.
    /// </param>
    public void UpdateStatus(InvoiceStatus newStatus, StatusChangeSource source = StatusChangeSource.Manual)
    {
        var previousStatus = Status;
        var at = DateTime.UtcNow;

        Status = newStatus;
        LastStatusTransitionAt = at;
        StatusHistory.Add(new StatusChange(previousStatus, newStatus, at, source));

        // Al entrar en un nuevo estado notificable comienza el conteo del aviso vigente (spec 019).
        if (IsNotifiableStatus(newStatus))
            NotificationRetryCount = 0;

        UpdateAuditDate();
    }

    /// <summary>
    /// Indica si el estado tiene una notificación por correo aplicable (mismo criterio que el
    /// worker / las herramientas de envío): recordatorios, pago y desactivación.
    /// </summary>
    private static bool IsNotifiableStatus(InvoiceStatus status) =>
        status is InvoiceStatus.PrimerRecordatorio
            or InvoiceStatus.SegundoRecordatorio
            or InvoiceStatus.Pagado
            or InvoiceStatus.Desactivado;

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
    /// Incrementa el contador de reintentos del aviso vigente (spec 019). Lo invocan las rutas de
    /// reenvío (por factura y masiva); NO lo invoca la primera notificación de la transición.
    /// </summary>
    public void RecordNotificationRetry()
    {
        NotificationRetryCount++;
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
