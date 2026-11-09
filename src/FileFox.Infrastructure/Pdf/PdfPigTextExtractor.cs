using FileFox.Core.Abstractions;
using FileFox.Core.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace FileFox.Infrastructure.Pdf;

/// <summary>Extracts text and image coverage per page with PdfPig.</summary>
public sealed class PdfPigTextExtractor : IPdfTextExtractor
{
    public Task<PdfContent> ExtractAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        // PdfPig is synchronous and CPU bound – run it off the caller's thread.
        return Task.Run(() => Extract(filePath, cancellationToken), cancellationToken);
    }

    internal static PdfContent Extract(string filePath, CancellationToken cancellationToken)
    {
        using var document = PdfDocument.Open(filePath);
        var pages = new List<PdfPageContent>(document.NumberOfPages);

        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            pages.Add(new PdfPageContent(page.Number, ExtractText(page), ImageCoverage(page)));
        }

        return new PdfContent(filePath, pages);
    }

    private static string ExtractText(Page page)
    {
        try
        {
            // Keeps the reading order, which helps the language model with tables.
            return ContentOrderTextExtractor.GetText(page);
        }
        catch (Exception)
        {
            // Fall back to the raw text if layout analysis fails on an unusual page.
            return page.Text;
        }
    }

    internal static double ImageCoverage(Page page)
    {
        var pageArea = page.Width * page.Height;
        if (pageArea <= 0)
        {
            return 0;
        }

        double imageArea = 0;
        foreach (var image in page.GetImages())
        {
            imageArea += Math.Abs(image.Bounds.Width * image.Bounds.Height);
        }

        // Overlapping images can add up to more than the page – cap at 100 %.
        return Math.Min(1.0, imageArea / pageArea);
    }
}
