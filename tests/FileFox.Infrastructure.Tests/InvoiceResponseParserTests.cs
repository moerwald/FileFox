using FileFox.Infrastructure.Ollama;

namespace FileFox.Infrastructure.Tests;

public class InvoiceResponseParserTests
{
    private const string FullAnswer = """
        {
          "company": "Amazon",
          "invoiceNumber": "DE5ABC123",
          "invoiceDate": "2026-11-08",
          "currency": "EUR",
          "totalAmount": 32.98,
          "lineItems": [
            { "description": "SanDisk USB stick 64 GB", "quantity": 1, "totalPrice": 12.99, "category": "IT-Hardware" },
            { "description": "Anker USB-C power adapter", "quantity": 1, "totalPrice": 19.99, "category": "IT-Hardware" }
          ]
        }
        """;

    [Fact]
    [Trait("Spec", "F1-30")]
    [Trait("Spec", "F1-31")]
    public void Parses_a_complete_answer()
    {
        var invoice = InvoiceResponseParser.Parse(FullAnswer);

        Assert.Equal("Amazon", invoice.Company);
        Assert.Equal("DE5ABC123", invoice.InvoiceNumber);
        Assert.Equal(new DateOnly(2026, 11, 8), invoice.InvoiceDate);
        Assert.Equal("EUR", invoice.Currency);
        Assert.Equal(32.98m, invoice.TotalAmount);
        Assert.Equal(2, invoice.LineItems.Count);
        Assert.Equal("SanDisk USB stick 64 GB", invoice.LineItems[0].Description);
        Assert.Equal(12.99m, invoice.LineItems[0].TotalPrice);
        Assert.Equal("IT-Hardware", invoice.LineItems[1].Category);
    }

    [Fact]
    [Trait("Spec", "F1-34")]
    public void Accepts_amounts_and_dates_in_german_format()
    {
        const string answer = """
            { "company": "IKEA", "invoiceNumber": "4711", "invoiceDate": "10.11.2026", "currency": "EUR",
              "totalAmount": "1.234,56",
              "lineItems": [ { "description": "Sofa", "quantity": "1", "totalPrice": "1.234,56 €", "category": "Household" } ] }
            """;

        var invoice = InvoiceResponseParser.Parse(answer);

        Assert.Equal(new DateOnly(2026, 11, 10), invoice.InvoiceDate);
        Assert.Equal(1234.56m, invoice.TotalAmount);
        Assert.Equal(1234.56m, invoice.LineItems[0].TotalPrice);
        Assert.Equal(1m, invoice.LineItems[0].Quantity);
    }

    [Fact]
    [Trait("Spec", "F1-35")]
    public void Missing_fields_become_empty_or_null()
    {
        var invoice = InvoiceResponseParser.Parse("""{ "company": "Amazon" }""");

        Assert.Equal("Amazon", invoice.Company);
        Assert.Equal("", invoice.InvoiceNumber);
        Assert.Null(invoice.InvoiceDate);
        Assert.Null(invoice.Currency);
        Assert.Null(invoice.TotalAmount);
        Assert.Empty(invoice.LineItems);
    }

    [Fact]
    [Trait("Spec", "F1-35")]
    public void Line_items_without_description_are_ignored()
    {
        var invoice = InvoiceResponseParser.Parse("""
            { "lineItems": [ { "description": "", "totalPrice": 1 }, { "description": "Cable", "totalPrice": 5 }, 42 ] }
            """);

        Assert.Single(invoice.LineItems);
        Assert.Equal("Cable", invoice.LineItems[0].Description);
    }

    [Fact]
    [Trait("Spec", "F1-30")]
    public void Numeric_invoice_number_is_kept_as_text()
    {
        var invoice = InvoiceResponseParser.Parse("""{ "invoiceNumber": 123456 }""");

        Assert.Equal("123456", invoice.InvoiceNumber);
    }

    [Fact]
    [Trait("Spec", "F1-36")]
    public void Markdown_code_fence_is_removed()
    {
        var invoice = InvoiceResponseParser.Parse("```json\n{ \"company\": \"Amazon\" }\n```");

        Assert.Equal("Amazon", invoice.Company);
    }

    [Theory]
    [Trait("Spec", "F1-36")]
    [InlineData("this is not json")]
    [InlineData("[1, 2, 3]")]
    public void Invalid_answer_throws_a_clear_error(string answer)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => InvoiceResponseParser.Parse(answer));
        Assert.Contains("language model", ex.Message);
    }

    [Theory]
    [Trait("Spec", "F1-34")]
    [InlineData("2026-11-10")]
    [InlineData("10.11.2026")]
    [InlineData("10/11/2026")]
    public void Parses_date_formats(string text)
    {
        Assert.Equal(new DateOnly(2026, 11, 10), InvoiceResponseParser.ParseDate(text));
    }
}
