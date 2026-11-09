using FileFox.Core.Abstractions;
using FileFox.Core.Options;
using Microsoft.Extensions.Options;

namespace FileFox.Core.Services;

/// <summary>
/// Picks the folder to analyze. A folder given by the user always wins; otherwise
/// <see cref="FileFoxOptions.InvoiceFolder"/> is used.
/// </summary>
public sealed class InvoiceFolderResolver(IFolderReader folderReader, IOptions<FileFoxOptions> options) : IInvoiceFolderResolver
{
    public const string NoFolderMessage =
        "No folder given. Pass a folder (filefox analyze <folder>) or set \"InvoiceFolder\" in the \"FileFox\" section of appsettings.json.";

    private readonly string _configuredFolder = options.Value.InvoiceFolder ?? "";

    public FolderResolution Resolve(string? folderArgument)
    {
        var folder = !string.IsNullOrWhiteSpace(folderArgument)
            ? folderArgument.Trim()
            : Environment.ExpandEnvironmentVariables(_configuredFolder.Trim());

        if (string.IsNullOrWhiteSpace(folder))
        {
            return FolderResolution.Failed(NoFolderMessage);
        }

        var fullPath = Path.GetFullPath(folder);
        return folderReader.FolderExists(fullPath)
            ? FolderResolution.Found(fullPath)
            : FolderResolution.Failed($"Folder not found: {fullPath}");
    }
}
