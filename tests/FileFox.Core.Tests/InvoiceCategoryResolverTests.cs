using FileFox.Core.Services;

namespace FileFox.Core.Tests;

public class InvoiceCategoryResolverTests
{
    private readonly InvoiceCategoryResolver _resolver = new(TestData.Options());

    [Fact]
    public void Single_category_is_not_mixed()
    {
        var invoice = TestData.Invoice(
            TestData.Item("USB stick", 12.99m, "IT-Hardware"),
            TestData.Item("USB power adapter", 19.99m, "IT-Hardware"));

        var result = _resolver.Resolve(invoice);

        Assert.Equal("IT-Hardware", result.InvoiceCategory);
        Assert.False(result.HasMixedCategories);
    }

    [Fact]
    public void Category_with_highest_amount_wins_and_is_marked_mixed()
    {
        var invoice = TestData.Invoice(
            TestData.Item("USB stick", 12.99m, "IT-Hardware"),
            TestData.Item("Cooking pot", 49.90m, "Household"));

        var result = _resolver.Resolve(invoice);

        Assert.Equal("Household", result.InvoiceCategory);
        Assert.True(result.HasMixedCategories);
    }

    [Fact]
    public void Categories_are_matched_case_insensitive_and_use_configured_spelling()
    {
        var invoice = TestData.Invoice(TestData.Item("USB stick", 12.99m, "it-hardware"));

        var result = _resolver.Resolve(invoice);

        Assert.Equal("IT-Hardware", result.Invoice.LineItems[0].Category);
        Assert.Equal("IT-Hardware", result.InvoiceCategory);
    }

    [Theory]
    [InlineData("Toys")]
    [InlineData("")]
    [InlineData(null)]
    public void Unknown_category_falls_back(string? category)
    {
        var invoice = TestData.Invoice(TestData.Item("Teddy bear", 9.99m, category!));

        var result = _resolver.Resolve(invoice);

        Assert.Equal("Other", result.Invoice.LineItems[0].Category);
    }

    [Fact]
    public void Without_prices_the_most_frequent_category_wins()
    {
        var invoice = TestData.Invoice(
            TestData.Item("USB stick", null, "IT-Hardware"),
            TestData.Item("HDMI cable", null, "IT-Hardware"),
            TestData.Item("Sponge", null, "Household"));

        var result = _resolver.Resolve(invoice);

        Assert.Equal("IT-Hardware", result.InvoiceCategory);
        Assert.True(result.HasMixedCategories);
    }

    [Fact]
    public void Tie_is_broken_by_category_order_in_settings()
    {
        var invoice = TestData.Invoice(
            TestData.Item("Sponge", 10m, "Household"),
            TestData.Item("USB stick", 10m, "IT-Hardware"));

        Assert.Equal("IT-Hardware", _resolver.Resolve(invoice).InvoiceCategory);
    }

    [Fact]
    public void Invoice_without_items_gets_fallback_category()
    {
        var result = _resolver.Resolve(TestData.Invoice());

        Assert.Equal("Other", result.InvoiceCategory);
        Assert.False(result.HasMixedCategories);
    }
}
