namespace FileFox.Core.Options;

/// <summary>Business settings of FileFox, bound from the "FileFox" section of appsettings.json.</summary>
public sealed class FileFoxOptions
{
    public const string SectionName = "FileFox";

    /// <summary>
    /// Folder with the invoice PDFs. Used when no folder is given on the command line.
    /// Environment variables like <c>%USERPROFILE%</c> are expanded. Empty means "not set".
    /// </summary>
    public string InvoiceFolder { get; set; } = "";

    /// <summary>Categories the language model may choose from.</summary>
    public List<CategoryOption> Categories { get; set; } = [];

    /// <summary>Category used when the model returns a category that is not in <see cref="Categories"/>.</summary>
    public string FallbackCategory { get; set; } = "Other";

    /// <summary>File name patterns (wildcards * and ?) that identify bank statements. These files are not treated as invoices.</summary>
    public List<string> StatementFileNamePatterns { get; set; } = [];

    public ScanDetectionOptions ScanDetection { get; set; } = new();
}

/// <summary>A category with a description that helps the language model choose it.</summary>
public sealed class CategoryOption
{
    public string Name { get; set; } = "";

    /// <summary>Examples of what belongs to this category, e.g. "USB sticks, power adapters, cables".</summary>
    public string Description { get; set; } = "";
}

/// <summary>Thresholds for telling digital PDFs and scans apart.</summary>
public sealed class ScanDetectionOptions
{
    /// <summary>A page needs at least this many non-whitespace characters to count as having text.</summary>
    public int MinCharactersPerPage { get; set; } = 20;

    /// <summary>Minimum share (0.0–1.0) of letters and digits among the non-whitespace characters. Filters out garbage text.</summary>
    public double MinTextQuality { get; set; } = 0.5;

    /// <summary>A page counts as an image page if images cover at least this share (0.0–1.0) of its area.</summary>
    public double LargeImageCoverage { get; set; } = 0.5;
}
