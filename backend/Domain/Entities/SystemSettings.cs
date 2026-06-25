using System;

namespace Monolegal.Domain.Entities;

public class SystemSettings
{
    public string Id { get; set; } = string.Empty;
    public InvoiceTransitionsConfig InvoiceTransitions { get; set; } = new();
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public void UpdateTransitions(InvoiceTransitionsConfig config)
    {
        InvoiceTransitions = config;
        UpdatedAt = DateTime.UtcNow;
    }
}

public class InvoiceTransitionsConfig
{
    public int PendingToFirstReminderDays { get; set; } = 3;
    public int FirstToSecondReminderDays { get; set; } = 3;
    public int SecondToDeactivatedDays { get; set; } = 3;
}
