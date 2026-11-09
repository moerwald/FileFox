using FileFox.Core.Abstractions;
using FileFox.Core.Services;

namespace FileFox.Core.Tests;

public class InvoiceFileLocatorTests
{
    private sealed class FakeFolderReader(params string[] files) : IFolderReader
    {
        public IEnumerable<string> GetPdfFiles(string folder) => files.Select(f => Path.Combine(folder, f));

        public bool FolderExists(string folder) => true;
    }

    [Fact]
    [Trait("Spec", "F1-04")]
    [Trait("Spec", "F1-05")]
    public void Bank_statements_are_excluded_and_invoices_are_sorted()
    {
        var reader = new FakeFolderReader("ikea.pdf", "Kontoauszug_2026_11.pdf", "Amazon.pdf", "bank-STATEMENT-nov.pdf");
        var locator = new InvoiceFileLocator(reader, TestData.Options());

        var result = locator.FindInvoiceFiles("folder");

        Assert.Equal(new[] { "Amazon.pdf", "ikea.pdf" }, result.Select(f => Path.GetFileName(f)));
    }

    [Fact]
    [Trait("Spec", "F1-04")]
    public void Without_patterns_every_pdf_is_an_invoice()
    {
        var reader = new FakeFolderReader("a.pdf", "Kontoauszug.pdf");
        var locator = new InvoiceFileLocator(reader, TestData.Options(o => o.StatementFileNamePatterns = []));

        Assert.Equal(2, locator.FindInvoiceFiles("folder").Count);
    }
}
