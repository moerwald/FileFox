using FileFox.Infrastructure.Pdf;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace FileFox.Infrastructure.Tests;

public sealed class PdfPigTextExtractorTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("filefox-tests-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    /// <summary>Creates a real PDF file with one text line per page (empty string = page without text).</summary>
    private string CreatePdf(params string[] pageTexts)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);

        foreach (var text in pageTexts)
        {
            var page = builder.AddPage(PageSize.A4);
            if (text.Length > 0)
            {
                page.AddText(text, 12, new PdfPoint(50, 750), font);
            }
        }

        var path = Path.Combine(_folder, $"{Guid.NewGuid():N}.pdf");
        File.WriteAllBytes(path, builder.Build());
        return path;
    }

    [Fact]
    [Trait("Spec", "F1-20")]
    [Trait("Spec", "F1-30")]
    public async Task Extracts_text_of_every_page()
    {
        var path = CreatePdf("Invoice number INV-2026-0042", "Total 12.99 EUR");

        var content = await new PdfPigTextExtractor().ExtractAsync(path);

        Assert.Equal(2, content.Pages.Count);
        Assert.Contains("INV-2026-0042", content.Pages[0].Text);
        Assert.Contains("12.99", content.Pages[1].Text);
        Assert.Contains("INV-2026-0042", content.FullText);
        Assert.Equal(path, content.FilePath);
    }

    [Fact]
    [Trait("Spec", "F1-21")]
    [Trait("Spec", "F1-22")]
    public async Task Page_without_text_and_images_has_empty_text_and_no_coverage()
    {
        var path = CreatePdf("");

        var content = await new PdfPigTextExtractor().ExtractAsync(path);

        var page = Assert.Single(content.Pages);
        Assert.True(string.IsNullOrWhiteSpace(page.Text));
        Assert.Equal(0, page.ImageCoverage);
    }

    [Fact]
    [Trait("Spec", "F1-25")]
    public async Task Broken_file_throws()
    {
        var path = Path.Combine(_folder, "broken.pdf");
        await File.WriteAllTextAsync(path, "this is not a pdf");

        await Assert.ThrowsAnyAsync<Exception>(() => new PdfPigTextExtractor().ExtractAsync(path));
    }
}
