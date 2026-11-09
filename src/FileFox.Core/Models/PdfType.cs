namespace FileFox.Core.Models;

/// <summary>How a PDF was created, as far as FileFox can tell.</summary>
public enum PdfType
{
    /// <summary>Created digitally; contains real, readable text.</summary>
    Digital,

    /// <summary>Scanned page image with an (often invisible) OCR text layer. Readable, but should be reviewed.</summary>
    ScanWithTextLayer,

    /// <summary>Scanned page image without readable text. Cannot be analyzed without OCR.</summary>
    Scan,
}
