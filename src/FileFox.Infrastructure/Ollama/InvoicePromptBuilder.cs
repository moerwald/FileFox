using System.Text;
using System.Text.Json.Nodes;
using FileFox.Core.Options;

namespace FileFox.Infrastructure.Ollama;

/// <summary>Builds the prompt and the JSON schema that tell the model what to extract.</summary>
internal static class InvoicePromptBuilder
{
    public static string BuildSystemPrompt(FileFoxOptions options)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You extract structured data from invoices. The invoice text can be German or English.");
        sb.AppendLine("Answer only with JSON that matches the given schema. Do not invent data that is not in the text.");
        sb.AppendLine();
        sb.AppendLine("Fields:");
        sb.AppendLine("- company: short name of the company that issued the invoice (the seller, not the customer), e.g. \"Amazon\", \"IKEA\".");
        sb.AppendLine("- invoiceNumber: the invoice number exactly as printed (Rechnungsnummer). Not the order number, customer number or tax ID. Empty string if missing.");
        sb.AppendLine("- invoiceDate: the invoice date in the format YYYY-MM-DD. Empty string if missing.");
        sb.AppendLine("- currency: ISO currency code, e.g. EUR.");
        sb.AppendLine("- totalAmount: final total including tax, as a number with a dot as decimal separator (e.g. 12.34).");
        sb.AppendLine("- lineItems: every purchased item or service.");
        sb.AppendLine("  - description: short item name in the language of the invoice.");
        sb.AppendLine("  - quantity: number of units, 1 if not printed.");
        sb.AppendLine("  - totalPrice: total of this line including tax if printed, as a number.");
        sb.AppendLine("  - category: the best fitting category for this item (see below).");
        sb.AppendLine("  Do not list subtotals, tax lines, payment information or free shipping as items.");
        sb.AppendLine("  List shipping costs as their own item only if they are charged.");
        sb.AppendLine();
        sb.AppendLine("Categories (use the exact name):");
        foreach (var category in options.Categories)
        {
            sb.Append("- ").Append(category.Name);
            if (!string.IsNullOrWhiteSpace(category.Description))
            {
                sb.Append(": ").Append(category.Description);
            }

            sb.AppendLine();
        }

        sb.Append("If no category fits, use \"").Append(options.FallbackCategory).AppendLine("\".");
        return sb.ToString();
    }

    public static string BuildUserPrompt(string invoiceText, int maxCharacters)
    {
        var text = invoiceText.Length > maxCharacters ? invoiceText[..maxCharacters] : invoiceText;
        return $"Invoice text:\n\"\"\"\n{text}\n\"\"\"";
    }

    /// <summary>JSON schema for Ollama structured outputs (the <c>format</c> field).</summary>
    public static JsonObject BuildSchema(FileFoxOptions options)
    {
        var categoryNames = options.Categories.Select(c => c.Name)
            .Append(options.FallbackCategory)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var categoryEnum = new JsonArray();
        foreach (var name in categoryNames)
        {
            categoryEnum.Add(name);
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["company"] = new JsonObject { ["type"] = "string" },
                ["invoiceNumber"] = new JsonObject { ["type"] = "string" },
                ["invoiceDate"] = new JsonObject { ["type"] = "string" },
                ["currency"] = new JsonObject { ["type"] = "string" },
                ["totalAmount"] = new JsonObject { ["type"] = "number" },
                ["lineItems"] = new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["description"] = new JsonObject { ["type"] = "string" },
                            ["quantity"] = new JsonObject { ["type"] = "number" },
                            ["totalPrice"] = new JsonObject { ["type"] = "number" },
                            ["category"] = new JsonObject { ["type"] = "string", ["enum"] = categoryEnum },
                        },
                        ["required"] = new JsonArray("description", "quantity", "totalPrice", "category"),
                    },
                },
            },
            ["required"] = new JsonArray("company", "invoiceNumber", "invoiceDate", "currency", "totalAmount", "lineItems"),
        };
    }
}
