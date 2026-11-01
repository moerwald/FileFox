using System.IO.Enumeration;
using FileFox.Core.Abstractions;
using FileFox.Core.Options;
using Microsoft.Extensions.Options;

namespace FileFox.Core.Services;

/// <summary>Returns all PDFs in a folder except files that look like bank statements.</summary>
public sealed class InvoiceFileLocator(IFolderReader folderReader, IOptions<FileFoxOptions> options) : IInvoiceFileLocator
{
    private readonly IReadOnlyList<string> _statementPatterns = options.Value.StatementFileNamePatterns;

    public IReadOnlyList<string> FindInvoiceFiles(string folder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);

        return folderReader.GetPdfFiles(folder)
            .Where(path => !IsStatement(Path.GetFileName(path)))
            .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    internal bool IsStatement(string fileName) =>
        _statementPatterns.Any(pattern => FileSystemName.MatchesSimpleExpression(pattern, fileName, ignoreCase: true));
}
