using FileFox.Core.Abstractions;
using FileFox.Core.Models;
using FileFox.Core.Options;
using Microsoft.Extensions.Options;

namespace FileFox.Core.Services;

/// <summary>
/// Maps every line item category to a configured category (case-insensitive, otherwise the fallback category)
/// and picks the category of the whole invoice: the category with the highest summed line total.
/// If no line has a price, the category with the most line items wins.
/// </summary>
public sealed class InvoiceCategoryResolver(IOptions<FileFoxOptions> options) : IInvoiceCategoryResolver
{
    private readonly FileFoxOptions _options = options.Value;

    public CategoryResolution Resolve(InvoiceData invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        var items = invoice.LineItems
            .Select(item => item with { Category = Normalize(item.Category) })
            .ToList();

        var normalizedInvoice = invoice with { LineItems = items };

        if (items.Count == 0)
        {
            return new CategoryResolution(normalizedInvoice, _options.FallbackCategory, HasMixedCategories: false);
        }

        var groups = items.GroupBy(i => i.Category, StringComparer.OrdinalIgnoreCase).ToList();
        var hasPrices = items.Any(i => i.TotalPrice is not null);

        var winner = groups
            .OrderByDescending(g => hasPrices ? g.Sum(i => Math.Abs(i.TotalPrice ?? 0m)) : g.Count())
            .ThenBy(g => CategoryIndex(g.Key))
            .First();

        return new CategoryResolution(normalizedInvoice, winner.Key, HasMixedCategories: groups.Count > 1);
    }

    internal string Normalize(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return _options.FallbackCategory;
        }

        var match = _options.Categories.FirstOrDefault(c =>
            string.Equals(c.Name, category.Trim(), StringComparison.OrdinalIgnoreCase));

        return match?.Name ?? _options.FallbackCategory;
    }

    private int CategoryIndex(string name)
    {
        var index = _options.Categories.FindIndex(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
        return index < 0 ? int.MaxValue : index;
    }
}
