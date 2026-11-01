using FileFox.Core.Models;
using FileFox.Core.Services;

namespace FileFox.Core.Tests;

public class PdfTypeDetectorTests
{
    private readonly PdfTypeDetector _detector = new(TestData.Options());

    [Fact]
    public void Digital_pdf_with_text_and_no_images()
    {
        var pdf = TestData.Pdf(TestData.Page(TestData.InvoiceText));

        Assert.Equal(PdfType.Digital, _detector.Detect(pdf));
    }

    [Fact]
    public void Digital_pdf_with_small_logo_is_still_digital()
    {
        var pdf = TestData.Pdf(TestData.Page(TestData.InvoiceText, imageCoverage: 0.05));

        Assert.Equal(PdfType.Digital, _detector.Detect(pdf));
    }

    [Fact]
    public void Page_image_without_text_is_a_scan()
    {
        var pdf = TestData.Pdf(TestData.Page("", imageCoverage: 1.0));

        Assert.Equal(PdfType.Scan, _detector.Detect(pdf));
    }

    [Fact]
    public void Empty_pdf_without_images_is_treated_as_scan()
    {
        var pdf = TestData.Pdf(TestData.Page("   \n  "));

        Assert.Equal(PdfType.Scan, _detector.Detect(pdf));
    }

    [Fact]
    public void Page_image_with_text_layer_is_scan_with_text_layer()
    {
        var pdf = TestData.Pdf(TestData.Page(TestData.InvoiceText, imageCoverage: 0.95));

        Assert.Equal(PdfType.ScanWithTextLayer, _detector.Detect(pdf));
    }

    [Fact]
    public void Too_little_text_counts_as_no_text()
    {
        var pdf = TestData.Pdf(TestData.Page("Page 1", imageCoverage: 1.0));

        Assert.Equal(PdfType.Scan, _detector.Detect(pdf));
    }

    [Fact]
    public void Garbage_text_counts_as_no_text()
    {
        var pdf = TestData.Pdf(TestData.Page("§$%&/()=?{[]}§$%&/()=?{[]}§$%&/()", imageCoverage: 1.0));

        Assert.Equal(PdfType.Scan, _detector.Detect(pdf));
    }

    [Fact]
    public void One_readable_page_is_enough()
    {
        var pdf = TestData.Pdf(TestData.Page(""), TestData.Page(TestData.InvoiceText));

        Assert.Equal(PdfType.Digital, _detector.Detect(pdf));
    }

    [Fact]
    public void Thresholds_come_from_options()
    {
        var detector = new PdfTypeDetector(TestData.Options(o => o.ScanDetection.MinCharactersPerPage = 500));
        var pdf = TestData.Pdf(TestData.Page(TestData.InvoiceText));

        Assert.Equal(PdfType.Scan, detector.Detect(pdf));
    }
}
