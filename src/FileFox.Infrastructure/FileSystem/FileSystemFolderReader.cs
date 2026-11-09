using FileFox.Core.Abstractions;

namespace FileFox.Infrastructure.FileSystem;

/// <summary>Reads PDF files from the local file system (top folder only, no subfolders).</summary>
public sealed class FileSystemFolderReader : IFolderReader
{
    private static readonly EnumerationOptions Options = new()
    {
        MatchCasing = MatchCasing.CaseInsensitive,
        RecurseSubdirectories = false,
        IgnoreInaccessible = true,
    };

    public IEnumerable<string> GetPdfFiles(string folder)
    {
        if (!Directory.Exists(folder))
        {
            throw new DirectoryNotFoundException($"Folder not found: {folder}");
        }

        return Directory.EnumerateFiles(folder, "*.pdf", Options);
    }

    public bool FolderExists(string folder) => Directory.Exists(folder);
}
