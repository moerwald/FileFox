namespace FileFox.Core.Models;

/// <summary>Overall outcome of analyzing one invoice file.</summary>
public enum AnalysisStatus
{
    /// <summary>Analyzed without any findings.</summary>
    Success,

    /// <summary>Analyzed, but the user should check the result (see warnings).</summary>
    NeedsReview,

    /// <summary>Not analyzed on purpose, e.g. because the file is a scan without text.</summary>
    Skipped,

    /// <summary>Analysis failed because of an error.</summary>
    Failed,
}

/// <summary>Result of analyzing one invoice file.</summary>
public sealed record InvoiceAnalysisResult
{
    public required string FilePath { get; init; }

    public required AnalysisStatus Status { get; init; }

    /// <summary>Detected PDF type; <c>null</c> if the file could not be read at all.</summary>
    public PdfType? PdfType { get; init; }

    /// <summary>Extracted invoice data; <c>null</c> if the file was skipped or failed.</summary>
    public InvoiceData? Invoice { get; init; }

    /// <summary>Category of the whole invoice, derived from its line items.</summary>
    public string? InvoiceCategory { get; init; }

    /// <summary><c>true</c> if the line items belong to more than one category.</summary>
    public bool HasMixedCategories { get; init; }

    /// <summary>Things the user should know about this result.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];

    /// <summary>Error or skip reason, if <see cref="Status"/> is <see cref="AnalysisStatus.Failed"/> or <see cref="AnalysisStatus.Skipped"/>.</summary>
    public string? Message { get; init; }

    /// <summary>How long the analysis took.</summary>
    public TimeSpan Duration { get; init; }

    public string FileName => Path.GetFileName(FilePath);
}
