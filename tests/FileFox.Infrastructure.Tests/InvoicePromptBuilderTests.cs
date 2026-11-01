using FileFox.Core.Options;
using FileFox.Infrastructure.Ollama;

namespace FileFox.Infrastructure.Tests;

public class InvoicePromptBuilderTests
{
    private static FileFoxOptions Options() => new()
    {
        Categories =
        [
            new() { Name = "IT-Hardware", Description = "USB sticks, power adapters" },
            new() { Name = "Household", Description = "Kitchen, cleaning" },
        ],
        FallbackCategory = "Other",
    };

    [Fact]
    public void System_prompt_lists_categories_with_descriptions()
    {
        var prompt = InvoicePromptBuilder.BuildSystemPrompt(Options());

        Assert.Contains("- IT-Hardware: USB sticks, power adapters", prompt);
        Assert.Contains("- Household: Kitchen, cleaning", prompt);
        Assert.Contains("\"Other\"", prompt);
    }

    [Fact]
    public void Schema_restricts_categories_to_configured_names_plus_fallback()
    {
        var schema = InvoicePromptBuilder.BuildSchema(Options());

        var categoryEnum = schema["properties"]!["lineItems"]!["items"]!["properties"]!["category"]!["enum"]!.AsArray()
            .Select(n => n!.GetValue<string>())
            .ToArray();

        Assert.Equal(new[] { "IT-Hardware", "Household", "Other" }, categoryEnum);
    }

    [Fact]
    public void Fallback_is_not_duplicated_when_it_is_a_configured_category()
    {
        var options = Options();
        options.Categories.Add(new CategoryOption { Name = "Other" });

        var schema = InvoicePromptBuilder.BuildSchema(options);
        var categoryEnum = schema["properties"]!["lineItems"]!["items"]!["properties"]!["category"]!["enum"]!.AsArray();

        Assert.Equal(3, categoryEnum.Count);
    }

    [Fact]
    public void Long_invoice_text_is_truncated()
    {
        var prompt = InvoicePromptBuilder.BuildUserPrompt(new string('x', 500), maxCharacters: 100);

        Assert.Contains(new string('x', 100), prompt);
        Assert.DoesNotContain(new string('x', 101), prompt);
    }
}
