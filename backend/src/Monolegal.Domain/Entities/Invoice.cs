using System;
using Monolegal.Domain.Enums;

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
        RemindersCount = 0;
        LastReminderSentAt = null;
    }

    public void UpdateStatus(InvoiceStatus newStatus)
    {
        Status = newStatus;
        UpdateAuditDate();
    }

    public void RecordReminderSent()
    {
        RemindersCount++;
        LastReminderSentAt = DateTime.UtcNow;
        UpdateAuditDate();
    }

    private void UpdateAuditDate()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}
