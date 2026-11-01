using FileFox.Core.Models;
using FileFox.Core.Options;
using Microsoft.Extensions.Options;

namespace FileFox.Core.Tests;

internal static class TestData
{
    public static IOptions<FileFoxOptions> Options(Action<FileFoxOptions>? configure = null)
    {
        var options = new FileFoxOptions
        {
            Categories =
            [
                new() { Name = "IT-Hardware" },
                new() { Name = "Software" },
                new() { Name = "Household" },
                new() { Name = "Other" },
            ],
            FallbackCategory = "Other",
            StatementFileNamePatterns = ["*Kontoauszug*", "*statement*"],
        };
        configure?.Invoke(options);
        return Microsoft.Extensions.Options.Options.Create(options);
    }

    public static PdfContent Pdf(params PdfPageContent[] pages) => new("invoice.pdf", pages);

    public static PdfPageContent Page(string text, double imageCoverage = 0) => new(1, text, imageCoverage);

    public const string InvoiceText = "Rechnung Nr. 123-4567 Amazon EU S.a r.l. SanDisk USB-Stick 64 GB 12,99 EUR";

    public static InvoiceData Invoice(params LineItem[] items) =>
        new("Amazon", "123-4567", new DateOnly(2026, 11, 10), "EUR", items.Sum(i => i.TotalPrice ?? 0), items);

    public static LineItem Item(string description, decimal? price, string category) =>
        new(description, 1, price, category);
}
