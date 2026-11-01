namespace FileFox.Core.Models;

/// <summary>A single position (line item) on an invoice.</summary>
/// <param name="Description">Short item name, e.g. "SanDisk USB stick 64 GB".</param>
/// <param name="Quantity">Quantity, if printed on the invoice.</param>
/// <param name="TotalPrice">Total price of this line, if printed on the invoice.</param>
/// <param name="Category">Category the language model assigned to this item.</param>
public sealed record LineItem(string Description, decimal? Quantity, decimal? TotalPrice, string Category);

/// <summary>Structured data extracted from an invoice.</summary>
public sealed record InvoiceData(
    string Company,
    string InvoiceNumber,
    DateOnly? InvoiceDate,
    string? Currency,
    decimal? TotalAmount,
    IReadOnlyList<LineItem> LineItems);
