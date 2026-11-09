using System.Globalization;
using System.Text;

namespace FileFox.Infrastructure.Ollama;

/// <summary>
/// Parses amounts that a model may return as text, e.g. "12,34", "1.234,56 €", "1,234.56" or "-5.00".
/// </summary>
internal static class AmountParser
{
    public static decimal? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        // Keep digits, separators and the minus sign; drop currency symbols, spaces etc.
        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (char.IsDigit(c) || c is ',' or '.' or '-')
            {
                sb.Append(c);
            }
        }

        var cleaned = sb.ToString();
        if (cleaned.Length == 0 || !cleaned.Any(char.IsDigit))
        {
            return null;
        }

        var lastComma = cleaned.LastIndexOf(',');
        var lastDot = cleaned.LastIndexOf('.');

        // The separator that comes last is the decimal separator; the other one groups thousands.
        if (lastComma >= 0 && lastDot >= 0)
        {
            cleaned = lastComma > lastDot
                ? cleaned.Replace(".", "").Replace(',', '.')
                : cleaned.Replace(",", "");
        }
        else if (lastComma >= 0)
        {
            // Only commas: a single comma is the (German) decimal separator, e.g. "12,34".
            // Several commas group thousands, e.g. "1,234,567".
            cleaned = cleaned.Count(c => c == ',') == 1
                ? cleaned.Replace(',', '.')
                : cleaned.Replace(",", "");
        }
        else if (lastDot >= 0 && cleaned.Count(c => c == '.') > 1)
        {
            // "1.234.567" – dots only used for grouping.
            cleaned = cleaned.Replace(".", "");
        }

        return decimal.TryParse(cleaned, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }
}
