using Shared.Enums;

namespace Shared.Dtos;

/// <summary>
/// Data transfer object for reading invoice data from the API.
/// </summary>
public sealed class InvoiceDto
{
    public string Id { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public InvoiceStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
