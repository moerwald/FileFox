using FileFox.Infrastructure.FileSystem;

namespace FileFox.Infrastructure.Tests;

public sealed class FileSystemFolderReaderTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("filefox-tests-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private void Touch(string relativePath)
    {
        var path = Path.Combine(_folder, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "x");
    }

    [Fact]
    [Trait("Spec", "F1-03")]
    public void Returns_only_pdf_files_directly_in_the_folder()
    {
        Touch("amazon.pdf");
        Touch("notes.txt");
        Touch("invoice.pdf.bak");
        Touch(Path.Combine("archive", "old.pdf"));

        var files = new FileSystemFolderReader().GetPdfFiles(_folder).Select(f => Path.GetFileName(f)).ToList();

        Assert.Equal(new[] { "amazon.pdf" }, files);
    }

    [Fact]
    [Trait("Spec", "F1-03")]
    public void Extension_is_matched_case_insensitively()
    {
        Touch("IKEA.PDF");
        Touch("amazon.Pdf");

        var files = new FileSystemFolderReader().GetPdfFiles(_folder).Select(f => Path.GetFileName(f)).Order(StringComparer.Ordinal).ToList();

        Assert.Equal(new[] { "IKEA.PDF", "amazon.Pdf" }, files);
    }

    [Fact]
    [Trait("Spec", "F1-02")]
    public void Missing_folder_throws()
    {
        var missing = Path.Combine(_folder, "does-not-exist");

        Assert.Throws<DirectoryNotFoundException>(() => new FileSystemFolderReader().GetPdfFiles(missing).ToList());
    }
}
