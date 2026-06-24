namespace Shared.Dtos;

/// <summary>
/// Data transfer object for creating a new invoice.
/// </summary>
public sealed class CreateInvoiceDto
{
    public string ClientId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public DateTime DueDate { get; init; }
}
