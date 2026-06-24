namespace Shared.Enums;

/// <summary>
/// Represents the lifecycle states of an invoice.
/// </summary>
public enum InvoiceStatus
{
    Pending = 0,
    Paid = 1,
    Overdue = 2,
    Cancelled = 3
}
