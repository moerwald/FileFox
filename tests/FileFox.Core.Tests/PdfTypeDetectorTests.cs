using FileFox.Core.Models;
using FileFox.Core.Services;

namespace FileFox.Core.Tests;

public class PdfTypeDetectorTests
{
    private readonly PdfTypeDetector _detector = new(TestData.Options());

    [Fact]
    [Trait("Spec", "F1-24")]
    public void Digital_pdf_with_text_and_no_images()
    {
        var pdf = TestData.Pdf(TestData.Page(TestData.InvoiceText));

        Assert.Equal(PdfType.Digital, _detector.Detect(pdf));
    }

    [Fact]
    [Trait("Spec", "F1-21")]
    [Trait("Spec", "F1-24")]
    public void Digital_pdf_with_small_logo_is_still_digital()
    {
        var pdf = TestData.Pdf(TestData.Page(TestData.InvoiceText, imageCoverage: 0.05));

        Assert.Equal(PdfType.Digital, _detector.Detect(pdf));
    }

    [Fact]
    [Trait("Spec", "F1-22")]
    public void Page_image_without_text_is_a_scan()
    {
        var pdf = TestData.Pdf(TestData.Page("", imageCoverage: 1.0));

        Assert.Equal(PdfType.Scan, _detector.Detect(pdf));
    }

    [Fact]
    [Trait("Spec", "F1-22")]
    public void Empty_pdf_without_images_is_treated_as_scan()
    {
        var pdf = TestData.Pdf(TestData.Page("   \n  "));

        Assert.Equal(PdfType.Scan, _detector.Detect(pdf));
    }

    [Fact]
    [Trait("Spec", "F1-23")]
    public void Page_image_with_text_layer_is_scan_with_text_layer()
    {
        var pdf = TestData.Pdf(TestData.Page(TestData.InvoiceText, imageCoverage: 0.95));

        Assert.Equal(PdfType.ScanWithTextLayer, _detector.Detect(pdf));
    }

    [Fact]
    [Trait("Spec", "F1-20")]
    public void Too_little_text_counts_as_no_text()
    {
        var pdf = TestData.Pdf(TestData.Page("Page 1", imageCoverage: 1.0));

        Assert.Equal(PdfType.Scan, _detector.Detect(pdf));
    }

    [Fact]
    [Trait("Spec", "F1-20")]
    public void Garbage_text_counts_as_no_text()
    {
        var pdf = TestData.Pdf(TestData.Page("§$%&/()=?{[]}§$%&/()=?{[]}§$%&/()", imageCoverage: 1.0));

        Assert.Equal(PdfType.Scan, _detector.Detect(pdf));
    }

    [Fact]
    [Trait("Spec", "F1-22")]
    [Trait("Spec", "F1-24")]
    public void One_readable_page_is_enough()
    {
        var pdf = TestData.Pdf(TestData.Page(""), TestData.Page(TestData.InvoiceText));

        Assert.Equal(PdfType.Digital, _detector.Detect(pdf));
    }

    [Fact]
    [Trait("Spec", "F1-20")]
    [Trait("Spec", "F1-83")]
    public void Thresholds_come_from_options()
    {
        var detector = new PdfTypeDetector(TestData.Options(o => o.ScanDetection.MinCharactersPerPage = 500));
        var pdf = TestData.Pdf(TestData.Page(TestData.InvoiceText));

        Assert.Equal(PdfType.Scan, detector.Detect(pdf));
    }

    // --- Boundaries from the specification ---

    [Theory]
    [Trait("Spec", "F1-20")]
    [InlineData(19, PdfType.Scan)]
    [InlineData(20, PdfType.Digital)]
    public void Readable_text_needs_at_least_20_non_whitespace_characters(int characters, PdfType expected)
    {
        // Spaces and line breaks must not count.
        var text = string.Join(" \n", Enumerable.Repeat("a", characters));

        Assert.Equal(expected, _detector.Detect(TestData.Pdf(TestData.Page(text))));
    }

    [Theory]
    [Trait("Spec", "F1-20")]
    [InlineData(10, 10, PdfType.Digital)] // exactly 50 % letters/digits
    [InlineData(9, 11, PdfType.Scan)]     // 45 % letters/digits
    public void Readable_text_needs_at_least_half_letters_or_digits(int letters, int symbols, PdfType expected)
    {
        var text = new string('a', letters) + new string('#', symbols);

        Assert.Equal(expected, _detector.Detect(TestData.Pdf(TestData.Page(text))));
    }

    [Theory]
    [Trait("Spec", "F1-21")]
    [Trait("Spec", "F1-23")]
    [Trait("Spec", "F1-24")]
    [InlineData(0.5, PdfType.ScanWithTextLayer)]
    [InlineData(0.49, PdfType.Digital)]
    public void Image_page_needs_at_least_half_of_the_page_covered(double coverage, PdfType expected)
    {
        var pdf = TestData.Pdf(TestData.Page(TestData.InvoiceText, imageCoverage: coverage));

        Assert.Equal(expected, _detector.Detect(pdf));
    }

    [Fact]
    [Trait("Spec", "F1-23")]
    public void Image_page_on_another_page_makes_it_a_scan_with_text_layer()
    {
        var pdf = TestData.Pdf(TestData.Page(TestData.InvoiceText), TestData.Page("", imageCoverage: 1.0));

        Assert.Equal(PdfType.ScanWithTextLayer, _detector.Detect(pdf));
    }
}
