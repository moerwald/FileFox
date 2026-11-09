using FileFox.Core.Abstractions;
using FileFox.Core.Models;
using FileFox.Core.Services;

namespace FileFox.Core.Tests;

public class InvoiceAnalysisServiceTests
{
    private sealed class FakeExtractor(PdfContent content) : IPdfTextExtractor
    {
        public Task<PdfContent> ExtractAsync(string filePath, CancellationToken cancellationToken = default) =>
            Task.FromResult(content with { FilePath = filePath });
    }

    private sealed class FakeAnalyzer(Func<string, InvoiceData> analyze) : IInvoiceAnalyzer
    {
        public int Calls { get; private set; }

        public Task<InvoiceData> AnalyzeAsync(string invoiceText, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            return Task.FromResult(analyze(invoiceText));
        }
    }

    private sealed class FakeModelStatus : ILanguageModelStatusProvider
    {
        public Task<ModelStatus> CheckAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new ModelStatus(true, "test-model", "ready"));
    }

    private sealed class NoFolder : IFolderReader
    {
        public IEnumerable<string> GetPdfFiles(string folder) => [];

        public bool FolderExists(string folder) => false;
    }

    private sealed class BrokenExtractor : IPdfTextExtractor
    {
        public Task<PdfContent> ExtractAsync(string filePath, CancellationToken cancellationToken = default) =>
            throw new InvalidDataException("Not a valid PDF file.");
    }

    private static InvoiceAnalysisService CreateService(PdfContent content, FakeAnalyzer analyzer) =>
        CreateService(new FakeExtractor(content), analyzer);

    private static InvoiceAnalysisService CreateService(IPdfTextExtractor extractor, FakeAnalyzer analyzer)
    {
        var options = TestData.Options();
        return new InvoiceAnalysisService(
            new InvoiceFolderResolver(new NoFolder(), options),
            new InvoiceFileLocator(new NoFolder(), options),
            extractor,
            new PdfTypeDetector(options),
            analyzer,
            new InvoiceCategoryResolver(options),
            new FakeModelStatus());
    }

    private static readonly PdfContent DigitalPdf = TestData.Pdf(TestData.Page(TestData.InvoiceText));

    [Fact]
    [Trait("Spec", "F1-24")]
    [Trait("Spec", "F1-50")]
    public async Task Digital_invoice_with_one_category_is_successful()
    {
        var analyzer = new FakeAnalyzer(_ => TestData.Invoice(TestData.Item("USB stick", 12.99m, "IT-Hardware")));
        var service = CreateService(DigitalPdf, analyzer);

        var result = await service.AnalyzeAsync("amazon.pdf");

        Assert.Equal(AnalysisStatus.Success, result.Status);
        Assert.Equal(PdfType.Digital, result.PdfType);
        Assert.Equal("IT-Hardware", result.InvoiceCategory);
        Assert.Empty(result.Warnings);
        Assert.Equal("amazon.pdf", result.FileName);
    }

    [Fact]
    [Trait("Spec", "F1-30")]
    public async Task Invoice_text_is_passed_to_the_analyzer()
    {
        string? received = null;
        var analyzer = new FakeAnalyzer(text =>
        {
            received = text;
            return TestData.Invoice(TestData.Item("USB stick", 12.99m, "IT-Hardware"));
        });

        await CreateService(DigitalPdf, analyzer).AnalyzeAsync("amazon.pdf");

        Assert.Contains("SanDisk USB-Stick", received);
    }

    [Fact]
    [Trait("Spec", "F1-22")]
    public async Task Scan_is_skipped_and_not_sent_to_the_model()
    {
        var analyzer = new FakeAnalyzer(_ => throw new InvalidOperationException("must not be called"));
        var service = CreateService(TestData.Pdf(TestData.Page("", imageCoverage: 1.0)), analyzer);

        var result = await service.AnalyzeAsync("scan.pdf");

        Assert.Equal(AnalysisStatus.Skipped, result.Status);
        Assert.Equal(PdfType.Scan, result.PdfType);
        Assert.Null(result.Invoice);
        Assert.Equal(0, analyzer.Calls);
    }

    [Fact]
    [Trait("Spec", "F1-44")]
    [Trait("Spec", "F1-50")]
    public async Task Mixed_categories_need_review()
    {
        var analyzer = new FakeAnalyzer(_ => TestData.Invoice(
            TestData.Item("USB stick", 12.99m, "IT-Hardware"),
            TestData.Item("Cooking pot", 49.90m, "Household")));

        var result = await CreateService(DigitalPdf, analyzer).AnalyzeAsync("amazon.pdf");

        Assert.Equal(AnalysisStatus.NeedsReview, result.Status);
        Assert.True(result.HasMixedCategories);
        Assert.Contains(result.Warnings, w => w.Contains("several categories"));
    }

    [Fact]
    [Trait("Spec", "F1-23")]
    [Trait("Spec", "F1-50")]
    public async Task Scan_with_text_layer_needs_review()
    {
        var analyzer = new FakeAnalyzer(_ => TestData.Invoice(TestData.Item("USB stick", 12.99m, "IT-Hardware")));
        var service = CreateService(TestData.Pdf(TestData.Page(TestData.InvoiceText, imageCoverage: 1.0)), analyzer);

        var result = await service.AnalyzeAsync("scan.pdf");

        Assert.Equal(AnalysisStatus.NeedsReview, result.Status);
        Assert.Equal(PdfType.ScanWithTextLayer, result.PdfType);
    }

    [Fact]
    [Trait("Spec", "F1-50")]
    public async Task Missing_invoice_number_needs_review()
    {
        var analyzer = new FakeAnalyzer(_ =>
            TestData.Invoice(TestData.Item("USB stick", 12.99m, "IT-Hardware")) with { InvoiceNumber = "" });

        var result = await CreateService(DigitalPdf, analyzer).AnalyzeAsync("amazon.pdf");

        Assert.Equal(AnalysisStatus.NeedsReview, result.Status);
        Assert.Contains(result.Warnings, w => w.Contains("invoice number"));
    }

    [Fact]
    [Trait("Spec", "F1-37")]
    public async Task Analyzer_error_is_reported_as_failed()
    {
        var analyzer = new FakeAnalyzer(_ => throw new InvalidOperationException("model broke"));

        var result = await CreateService(DigitalPdf, analyzer).AnalyzeAsync("amazon.pdf");

        Assert.Equal(AnalysisStatus.Failed, result.Status);
        Assert.Equal("model broke", result.Message);
        Assert.Equal(PdfType.Digital, result.PdfType);
    }

    [Fact]
    [Trait("Spec", "F1-72")]
    public async Task Cancellation_is_not_swallowed()
    {
        var analyzer = new FakeAnalyzer(_ => TestData.Invoice());
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CreateService(DigitalPdf, analyzer).AnalyzeAsync("amazon.pdf", cts.Token));
    }

    // --- Additional cases from the specification ---

    [Fact]
    [Trait("Spec", "F1-50")]
    public async Task Missing_company_needs_review()
    {
        var analyzer = new FakeAnalyzer(_ =>
            TestData.Invoice(TestData.Item("USB stick", 12.99m, "IT-Hardware")) with { Company = " " });

        var result = await CreateService(DigitalPdf, analyzer).AnalyzeAsync("amazon.pdf");

        Assert.Equal(AnalysisStatus.NeedsReview, result.Status);
        Assert.Contains(result.Warnings, w => w.Contains("company"));
    }

    [Fact]
    [Trait("Spec", "F1-50")]
    public async Task Invoice_without_line_items_needs_review()
    {
        var analyzer = new FakeAnalyzer(_ => TestData.Invoice());

        var result = await CreateService(DigitalPdf, analyzer).AnalyzeAsync("amazon.pdf");

        Assert.Equal(AnalysisStatus.NeedsReview, result.Status);
        Assert.Contains(result.Warnings, w => w.Contains("line items"));
    }

    [Fact]
    [Trait("Spec", "F1-50")]
    public async Task Scan_with_text_layer_warning_explains_the_reason()
    {
        var analyzer = new FakeAnalyzer(_ => TestData.Invoice(TestData.Item("USB stick", 12.99m, "IT-Hardware")));
        var service = CreateService(TestData.Pdf(TestData.Page(TestData.InvoiceText, imageCoverage: 1.0)), analyzer);

        var result = await service.AnalyzeAsync("scan.pdf");

        Assert.Contains(result.Warnings, w => w.Contains("Scanned PDF"));
    }

    [Fact]
    [Trait("Spec", "F1-41")]
    [Trait("Spec", "F1-40")]
    public async Task Result_contains_normalized_item_categories()
    {
        var analyzer = new FakeAnalyzer(_ => TestData.Invoice(
            TestData.Item("USB stick", 12.99m, "it-hardware"),
            TestData.Item("Teddy bear", 1m, "Toys")));

        var result = await CreateService(DigitalPdf, analyzer).AnalyzeAsync("amazon.pdf");

        Assert.Equal(new[] { "IT-Hardware", "Other" }, result.Invoice!.LineItems.Select(i => i.Category));
        Assert.Equal("IT-Hardware", result.InvoiceCategory);
    }

    [Fact]
    [Trait("Spec", "F1-25")]
    public async Task Unreadable_pdf_is_reported_as_failed_without_calling_the_model()
    {
        var analyzer = new FakeAnalyzer(_ => TestData.Invoice());

        var result = await CreateService(new BrokenExtractor(), analyzer).AnalyzeAsync("broken.pdf");

        Assert.Equal(AnalysisStatus.Failed, result.Status);
        Assert.Null(result.PdfType);
        Assert.Contains("Not a valid PDF", result.Message);
        Assert.Equal(0, analyzer.Calls);
    }

    [Fact]
    [Trait("Spec", "F1-37")]
    public async Task Timeout_of_the_model_is_reported_as_failed_and_not_as_cancellation()
    {
        // HttpClient reports a timeout as TaskCanceledException although the user did not cancel.
        var analyzer = new FakeAnalyzer(_ => throw new TaskCanceledException("The request timed out."));

        var result = await CreateService(DigitalPdf, analyzer).AnalyzeAsync("amazon.pdf");

        Assert.Equal(AnalysisStatus.Failed, result.Status);
        Assert.Contains("timed out", result.Message);
    }
}
