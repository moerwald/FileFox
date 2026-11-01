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
    }

    private static InvoiceAnalysisService CreateService(PdfContent content, FakeAnalyzer analyzer)
    {
        var options = TestData.Options();
        return new InvoiceAnalysisService(
            new InvoiceFileLocator(new NoFolder(), options),
            new FakeExtractor(content),
            new PdfTypeDetector(options),
            analyzer,
            new InvoiceCategoryResolver(options),
            new FakeModelStatus());
    }

    private static readonly PdfContent DigitalPdf = TestData.Pdf(TestData.Page(TestData.InvoiceText));

    [Fact]
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
    public async Task Scan_with_text_layer_needs_review()
    {
        var analyzer = new FakeAnalyzer(_ => TestData.Invoice(TestData.Item("USB stick", 12.99m, "IT-Hardware")));
        var service = CreateService(TestData.Pdf(TestData.Page(TestData.InvoiceText, imageCoverage: 1.0)), analyzer);

        var result = await service.AnalyzeAsync("scan.pdf");

        Assert.Equal(AnalysisStatus.NeedsReview, result.Status);
        Assert.Equal(PdfType.ScanWithTextLayer, result.PdfType);
    }

    [Fact]
    public async Task Missing_invoice_number_needs_review()
    {
        var analyzer = new FakeAnalyzer(_ =>
            TestData.Invoice(TestData.Item("USB stick", 12.99m, "IT-Hardware")) with { InvoiceNumber = "" });

        var result = await CreateService(DigitalPdf, analyzer).AnalyzeAsync("amazon.pdf");

        Assert.Equal(AnalysisStatus.NeedsReview, result.Status);
        Assert.Contains(result.Warnings, w => w.Contains("invoice number"));
    }

    [Fact]
    public async Task Analyzer_error_is_reported_as_failed()
    {
        var analyzer = new FakeAnalyzer(_ => throw new InvalidOperationException("model broke"));

        var result = await CreateService(DigitalPdf, analyzer).AnalyzeAsync("amazon.pdf");

        Assert.Equal(AnalysisStatus.Failed, result.Status);
        Assert.Equal("model broke", result.Message);
        Assert.Equal(PdfType.Digital, result.PdfType);
    }

    [Fact]
    public async Task Cancellation_is_not_swallowed()
    {
        var analyzer = new FakeAnalyzer(_ => TestData.Invoice());
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CreateService(DigitalPdf, analyzer).AnalyzeAsync("amazon.pdf", cts.Token));
    }
}
