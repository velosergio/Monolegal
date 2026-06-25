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
