using FileFox.Core.Models;

namespace FileFox.Core.Abstractions;

/// <summary>Lists PDF files in a folder. Implemented by the infrastructure layer.</summary>
public interface IFolderReader
{
    /// <summary>Returns the full paths of all PDF files directly inside <paramref name="folder"/>.</summary>
    IEnumerable<string> GetPdfFiles(string folder);

    /// <summary>Returns <c>true</c> if <paramref name="folder"/> exists.</summary>
    bool FolderExists(string folder);
}

/// <summary>Decides which folder to analyze: the folder given by the user or the configured default folder.</summary>
public interface IInvoiceFolderResolver
{
    FolderResolution Resolve(string? folderArgument);
}

/// <summary>Result of <see cref="IInvoiceFolderResolver.Resolve"/>. Either <see cref="Folder"/> or <see cref="Error"/> is set.</summary>
/// <param name="Folder">Full path of an existing folder, or <c>null</c> if there is no usable folder.</param>
/// <param name="Error">Message for the user if there is no usable folder.</param>
public sealed record FolderResolution(string? Folder, string? Error)
{
    public bool IsValid => Folder is not null;

    public static FolderResolution Found(string folder) => new(folder, null);

    public static FolderResolution Failed(string error) => new(null, error);
}

/// <summary>Finds the invoice files in a folder (all PDFs except bank statements).</summary>
public interface IInvoiceFileLocator
{
    IReadOnlyList<string> FindInvoiceFiles(string folder);
}

/// <summary>Extracts text and image information from a PDF file.</summary>
public interface IPdfTextExtractor
{
    Task<PdfContent> ExtractAsync(string filePath, CancellationToken cancellationToken = default);
}

/// <summary>Decides whether a PDF is a digital PDF or a scan.</summary>
public interface IPdfTypeDetector
{
    PdfType Detect(PdfContent content);
}

/// <summary>Extracts structured invoice data from invoice text, e.g. with a language model.</summary>
public interface IInvoiceAnalyzer
{
    Task<InvoiceData> AnalyzeAsync(string invoiceText, CancellationToken cancellationToken = default);
}

/// <summary>Checks whether the language model is ready to use.</summary>
public interface ILanguageModelStatusProvider
{
    Task<ModelStatus> CheckAsync(CancellationToken cancellationToken = default);
}

/// <summary>Normalizes line item categories and derives the category of the whole invoice.</summary>
public interface IInvoiceCategoryResolver
{
    CategoryResolution Resolve(InvoiceData invoice);
}

/// <summary>Result of <see cref="IInvoiceCategoryResolver.Resolve"/>.</summary>
/// <param name="Invoice">Invoice with every line item category mapped to a configured category.</param>
/// <param name="InvoiceCategory">Category of the whole invoice.</param>
/// <param name="HasMixedCategories"><c>true</c> if the line items belong to more than one category.</param>
public sealed record CategoryResolution(InvoiceData Invoice, string InvoiceCategory, bool HasMixedCategories);

/// <summary>
/// Entry point for front ends (CLI, later WPF). Orchestrates the whole invoice analysis.
/// Front ends should only talk to this service.
/// </summary>
public interface IInvoiceAnalysisService
{
    /// <summary>Returns the folder to analyze: <paramref name="folderArgument"/> if given, otherwise the configured folder.</summary>
    FolderResolution ResolveFolder(string? folderArgument);

    Task<ModelStatus> CheckModelAsync(CancellationToken cancellationToken = default);

    IReadOnlyList<string> FindInvoiceFiles(string folder);

    Task<InvoiceAnalysisResult> AnalyzeAsync(string filePath, CancellationToken cancellationToken = default);
}
