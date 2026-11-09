using System.Globalization;
using System.Text.Json;
using FileFox.Core.Models;

namespace FileFox.Infrastructure.Ollama;

/// <summary>Turns the model's JSON answer into <see cref="InvoiceData"/>. Tolerant of missing fields and amounts sent as text.</summary>
internal static class InvoiceResponseParser
{
    private static readonly string[] DateFormats = ["yyyy-MM-dd", "dd.MM.yyyy", "d.M.yyyy", "dd.MM.yy", "dd/MM/yyyy", "yyyy/MM/dd"];

    public static InvoiceData Parse(string json)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(StripCodeFence(json));
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"The language model did not return valid JSON: {ex.Message}", ex);
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("The language model did not return a JSON object.");
            }

            var items = new List<LineItem>();
            if (root.TryGetProperty("lineItems", out var lineItems) && lineItems.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in lineItems.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    var description = GetString(item, "description");
                    if (string.IsNullOrWhiteSpace(description))
                    {
                        continue;
                    }

                    items.Add(new LineItem(
                        description,
                        GetDecimal(item, "quantity"),
                        GetDecimal(item, "totalPrice"),
                        GetString(item, "category")));
                }
            }

            return new InvoiceData(
                Company: GetString(root, "company"),
                InvoiceNumber: GetString(root, "invoiceNumber"),
                InvoiceDate: ParseDate(GetString(root, "invoiceDate")),
                Currency: NullIfEmpty(GetString(root, "currency")),
                TotalAmount: GetDecimal(root, "totalAmount"),
                LineItems: items);
        }
    }

    internal static DateOnly? ParseDate(string? text) =>
        DateOnly.TryParseExact(text?.Trim(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;

    private static string GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined)
            ? (value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : value.GetRawText()).Trim()
            : "";

    private static decimal? GetDecimal(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetDecimal(out var number) => number,
            JsonValueKind.String => AmountParser.Parse(value.GetString()),
            _ => null,
        };
    }

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    /// <summary>Some models wrap JSON in a markdown code fence. Remove it.</summary>
    private static string StripCodeFence(string text)
    {
        var trimmed = text.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            return trimmed;
        }

        var firstNewLine = trimmed.IndexOf('\n');
        var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
        return firstNewLine >= 0 && lastFence > firstNewLine
            ? trimmed[(firstNewLine + 1)..lastFence].Trim()
            : trimmed.Trim('`').Trim();
    }
}
