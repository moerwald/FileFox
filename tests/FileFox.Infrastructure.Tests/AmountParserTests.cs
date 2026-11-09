using FileFox.Infrastructure.Ollama;

namespace FileFox.Infrastructure.Tests;

public class AmountParserTests
{
    [Theory]
    [Trait("Spec", "F1-34")]
    [InlineData("12.34", "12.34")]
    [InlineData("12,34", "12.34")]
    [InlineData("12,34 €", "12.34")]
    [InlineData("EUR 1.234,56", "1234.56")]
    [InlineData("1,234.56", "1234.56")]
    [InlineData("1.234.567", "1234567")]
    [InlineData("1,234,567", "1234567")]
    [InlineData("-5,00", "-5.00")]
    [InlineData("7", "7")]
    public void Parses_common_formats(string input, string expected)
    {
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), AmountParser.Parse(input));
    }

    [Theory]
    [Trait("Spec", "F1-34")]
    [Trait("Spec", "F1-35")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("n/a")]
    [InlineData("€")]
    public void Returns_null_for_non_amounts(string? input)
    {
        Assert.Null(AmountParser.Parse(input));
    }
}
