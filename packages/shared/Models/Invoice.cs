using Shared.Enums;

namespace Shared.Models;

/// <summary>
/// Shared invoice model used across backend and worker contexts.
/// </summary>
public sealed class Invoice
{
    public string Id { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;
    public DateTime DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
