using System.Diagnostics;
using FileFox.Core.Abstractions;
using FileFox.Core.Models;

namespace FileFox.Core.Services;

/// <summary>Orchestrates the analysis of a single invoice: read PDF → detect scan → ask model → resolve categories.</summary>
public sealed class InvoiceAnalysisService(
    IInvoiceFileLocator fileLocator,
    IPdfTextExtractor textExtractor,
    IPdfTypeDetector typeDetector,
    IInvoiceAnalyzer analyzer,
    IInvoiceCategoryResolver categoryResolver,
    ILanguageModelStatusProvider modelStatusProvider) : IInvoiceAnalysisService
{
    public Task<ModelStatus> CheckModelAsync(CancellationToken cancellationToken = default) =>
        modelStatusProvider.CheckAsync(cancellationToken);

    public IReadOnlyList<string> FindInvoiceFiles(string folder) => fileLocator.FindInvoiceFiles(folder);

    public async Task<InvoiceAnalysisResult> AnalyzeAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var stopwatch = Stopwatch.StartNew();
        PdfType? pdfType = null;

        try
        {
            var content = await textExtractor.ExtractAsync(filePath, cancellationToken).ConfigureAwait(false);
            pdfType = typeDetector.Detect(content);

            if (pdfType == PdfType.Scan)
            {
                return new InvoiceAnalysisResult
                {
                    FilePath = filePath,
                    Status = AnalysisStatus.Skipped,
                    PdfType = pdfType,
                    Message = "Scan without a text layer – cannot be read without OCR.",
                    Duration = stopwatch.Elapsed,
                };
            }

            var invoice = await analyzer.AnalyzeAsync(content.FullText, cancellationToken).ConfigureAwait(false);
            var resolution = categoryResolver.Resolve(invoice);
            var warnings = CollectWarnings(pdfType.Value, resolution);

            return new InvoiceAnalysisResult
            {
                FilePath = filePath,
                Status = warnings.Count > 0 ? AnalysisStatus.NeedsReview : AnalysisStatus.Success,
                PdfType = pdfType,
                Invoice = resolution.Invoice,
                InvoiceCategory = resolution.InvoiceCategory,
                HasMixedCategories = resolution.HasMixedCategories,
                Warnings = warnings,
                Duration = stopwatch.Elapsed,
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new InvoiceAnalysisResult
            {
                FilePath = filePath,
                Status = AnalysisStatus.Failed,
                PdfType = pdfType,
                Message = ex.Message,
                Duration = stopwatch.Elapsed,
            };
        }
    }

    private static List<string> CollectWarnings(PdfType pdfType, CategoryResolution resolution)
    {
        var warnings = new List<string>();
        var invoice = resolution.Invoice;

        if (pdfType == PdfType.ScanWithTextLayer)
        {
            warnings.Add("Scanned PDF with a text layer – the text may contain OCR errors.");
        }

        if (resolution.HasMixedCategories)
        {
            warnings.Add($"Items belong to several categories – '{resolution.InvoiceCategory}' was chosen by the highest amount.");
        }

        if (string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
        {
            warnings.Add("No invoice number found.");
        }

        if (string.IsNullOrWhiteSpace(invoice.Company))
        {
            warnings.Add("No company found.");
        }

        if (invoice.LineItems.Count == 0)
        {
            warnings.Add("No line items found.");
        }

        return warnings;
    }
}
