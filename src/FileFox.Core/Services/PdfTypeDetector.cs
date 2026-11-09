using FileFox.Core.Abstractions;
using FileFox.Core.Models;
using FileFox.Core.Options;
using Microsoft.Extensions.Options;

namespace FileFox.Core.Services;

/// <summary>
/// Classifies a PDF from its extracted text and image coverage.
/// <list type="bullet">
/// <item>No page with readable text → <see cref="PdfType.Scan"/>.</item>
/// <item>Readable text and at least one page mostly covered by an image → <see cref="PdfType.ScanWithTextLayer"/>.</item>
/// <item>Readable text, no large images → <see cref="PdfType.Digital"/>.</item>
/// </list>
/// </summary>
public sealed class PdfTypeDetector(IOptions<FileFoxOptions> options) : IPdfTypeDetector
{
    private readonly ScanDetectionOptions _settings = options.Value.ScanDetection;

    public PdfType Detect(PdfContent content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var hasReadableText = content.Pages.Any(HasReadableText);
        if (!hasReadableText)
        {
            return PdfType.Scan;
        }

        var hasLargeImage = content.Pages.Any(p => p.ImageCoverage >= _settings.LargeImageCoverage);
        return hasLargeImage ? PdfType.ScanWithTextLayer : PdfType.Digital;
    }

    internal bool HasReadableText(PdfPageContent page)
    {
        var visible = page.Text.Where(c => !char.IsWhiteSpace(c)).ToArray();
        if (visible.Length < _settings.MinCharactersPerPage)
        {
            return false;
        }

        var lettersAndDigits = visible.Count(char.IsLetterOrDigit);
        return (double)lettersAndDigits / visible.Length >= _settings.MinTextQuality;
    }
}
