namespace FileFox.Core.Models;

/// <summary>Text and image information of a single PDF page.</summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="Text">Extracted text of the page (may be empty).</param>
/// <param name="ImageCoverage">Share of the page area covered by images, from 0.0 to 1.0.</param>
public sealed record PdfPageContent(int PageNumber, string Text, double ImageCoverage);

/// <summary>Extracted content of a whole PDF file.</summary>
public sealed record PdfContent(string FilePath, IReadOnlyList<PdfPageContent> Pages)
{
    /// <summary>Text of all pages, separated by blank lines.</summary>
    public string FullText => string.Join("\n\n", Pages.Select(p => p.Text));
}
