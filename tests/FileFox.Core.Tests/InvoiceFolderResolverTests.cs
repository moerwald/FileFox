using FileFox.Core.Abstractions;
using FileFox.Core.Services;

namespace FileFox.Core.Tests;

public class InvoiceFolderResolverTests
{
    private static readonly string ArgumentFolder = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "filefox-argument"));
    private static readonly string ConfiguredFolder = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "filefox-configured"));

    /// <summary>Folder reader that only knows the folders passed to it.</summary>
    private sealed class FakeFolderReader(params string[] existingFolders) : IFolderReader
    {
        public IEnumerable<string> GetPdfFiles(string folder) => [];

        public bool FolderExists(string folder) => existingFolders.Contains(folder, StringComparer.Ordinal);
    }

    private static InvoiceFolderResolver CreateResolver(string configuredFolder, params string[] existingFolders) =>
        new(new FakeFolderReader(existingFolders), TestData.Options(o => o.InvoiceFolder = configuredFolder));

    [Fact]
    [Trait("Spec", "F1-01")]
    public void Folder_argument_is_used()
    {
        var resolver = CreateResolver("", ArgumentFolder);

        var result = resolver.Resolve(ArgumentFolder);

        Assert.True(result.IsValid);
        Assert.Equal(ArgumentFolder, result.Folder);
        Assert.Null(result.Error);
    }

    [Fact]
    [Trait("Spec", "F1-08")]
    public void Configured_folder_is_used_without_an_argument()
    {
        var resolver = CreateResolver(ConfiguredFolder, ConfiguredFolder);

        var result = resolver.Resolve(null);

        Assert.Equal(ConfiguredFolder, result.Folder);
    }

    [Theory]
    [Trait("Spec", "F1-08")]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_argument_counts_as_no_argument(string argument)
    {
        var resolver = CreateResolver(ConfiguredFolder, ConfiguredFolder);

        Assert.Equal(ConfiguredFolder, resolver.Resolve(argument).Folder);
    }

    [Fact]
    [Trait("Spec", "F1-08")]
    public void Folder_argument_wins_over_the_configured_folder()
    {
        var resolver = CreateResolver(ConfiguredFolder, ArgumentFolder, ConfiguredFolder);

        Assert.Equal(ArgumentFolder, resolver.Resolve(ArgumentFolder).Folder);
    }

    [Fact]
    [Trait("Spec", "F1-08")]
    public void Environment_variables_in_the_configured_folder_are_expanded()
    {
        var name = "FILEFOX_TEST_" + Guid.NewGuid().ToString("N");
        Environment.SetEnvironmentVariable(name, Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar));
        try
        {
            var expected = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "invoices"));
            var resolver = CreateResolver(Path.Combine($"%{name}%", "invoices"), expected);

            Assert.Equal(expected, resolver.Resolve(null).Folder);
        }
        finally
        {
            Environment.SetEnvironmentVariable(name, null);
        }
    }

    [Theory]
    [Trait("Spec", "F1-09")]
    [InlineData("")]
    [InlineData("  ")]
    public void Without_argument_and_setting_the_error_explains_both_ways(string configuredFolder)
    {
        var resolver = CreateResolver(configuredFolder);

        var result = resolver.Resolve(null);

        Assert.False(result.IsValid);
        Assert.Null(result.Folder);
        Assert.Contains("No folder given", result.Error);
        Assert.Contains("filefox analyze <folder>", result.Error);
        Assert.Contains("InvoiceFolder", result.Error);
        Assert.Contains("appsettings.json", result.Error);
    }

    [Fact]
    [Trait("Spec", "F1-02")]
    public void Missing_argument_folder_is_an_error_that_names_the_folder()
    {
        var resolver = CreateResolver(ConfiguredFolder, ConfiguredFolder);

        var result = resolver.Resolve(ArgumentFolder);

        Assert.False(result.IsValid);
        Assert.Equal($"Folder not found: {ArgumentFolder}", result.Error);
    }

    [Fact]
    [Trait("Spec", "F1-02")]
    [Trait("Spec", "F1-08")]
    public void Missing_configured_folder_is_an_error_that_names_the_folder()
    {
        var resolver = CreateResolver(ConfiguredFolder);

        var result = resolver.Resolve(null);

        Assert.False(result.IsValid);
        Assert.Equal($"Folder not found: {ConfiguredFolder}", result.Error);
    }
}
