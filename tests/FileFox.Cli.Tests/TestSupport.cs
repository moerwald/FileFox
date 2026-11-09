using FileFox.Cli.Composition;
using FileFox.Core.Abstractions;
using FileFox.Core.Models;
using FileFox.Core.Options;
using FileFox.Core.Services;
using FileFox.Infrastructure.FileSystem;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Testing;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace FileFox.Cli.Tests;

/// <summary>Locates files in the repository, independent of the test output folder.</summary>
internal static class RepoPaths
{
    public static string Root { get; } = FindRoot();

    public static string CliProjectDirectory => Path.Combine(Root, "src", "FileFox.Cli");

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "FileFox.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Repository root (FileFox.slnx) not found.");
    }
}

/// <summary>Runs the real CLI (same composition as Program) against a test console.</summary>
internal static class CliTestHost
{
    public static Task<(int ExitCode, string Output)> RunAsync(Action<IServiceCollection> overrides, params string[] args) =>
        RunAsync(overrides, new Dictionary<string, string?>(), args);

    /// <summary>Runs the CLI with <paramref name="settings"/> layered over the shipped appsettings.json.</summary>
    public static async Task<(int ExitCode, string Output)> RunAsync(
        Action<IServiceCollection> overrides, IDictionary<string, string?> settings, params string[] args)
    {
        var console = new TestConsole();
        console.Profile.Width = 250;

        var configuration = new ConfigurationBuilder()
            .AddConfiguration(CliComposition.BuildConfiguration(RepoPaths.CliProjectDirectory))
            .AddInMemoryCollection(settings)
            .Build();
        var services = new ServiceCollection().AddFileFoxCli(configuration, console);
        overrides(services);

        var exitCode = await CliComposition.CreateApp(services, console).RunAsync(args);
        return (exitCode, console.Output);
    }
}

/// <summary>Temporary folder that is deleted after the test.</summary>
internal sealed class TempFolder : IDisposable
{
    public string Path { get; } = Directory.CreateTempSubdirectory("filefox-cli-tests-").FullName;

    public string File(string name) => System.IO.Path.Combine(Path, name);

    public void Dispose() => Directory.Delete(Path, recursive: true);
}

internal static class SamplePdf
{
    /// <summary>Writes a PDF with one text line per page; an empty string creates a page without text.</summary>
    public static void Write(string path, params string[] pageTexts)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        foreach (var text in pageTexts)
        {
            var page = builder.AddPage(PageSize.A4);
            if (text.Length > 0)
            {
                page.AddText(text, 11, new PdfPoint(50, 750), font);
            }
        }

        File.WriteAllBytes(path, builder.Build());
    }
}

/// <summary>Configurable stand-in for the core service. Records how the CLI uses it.</summary>
internal sealed class FakeAnalysisService : IInvoiceAnalysisService
{
    public ModelStatus ModelStatus { get; set; } = new(true, "qwen2.5:7b", "Model qwen2.5:7b is ready.");

    public List<string> Files { get; set; } = [];

    public Func<string, InvoiceAnalysisResult> Analyze { get; set; } = Results.Ok;

    public List<string> Calls { get; } = [];

    /// <summary>Value of <c>FileFox:InvoiceFolder</c> seen by the real folder resolver.</summary>
    public string ConfiguredFolder { get; set; } = "";

    public FolderResolution ResolveFolder(string? folderArgument)
    {
        Calls.Add("resolve");
        var resolver = new InvoiceFolderResolver(
            new FileSystemFolderReader(),
            Microsoft.Extensions.Options.Options.Create(new FileFoxOptions { InvoiceFolder = ConfiguredFolder }));
        return resolver.Resolve(folderArgument);
    }

    public Task<ModelStatus> CheckModelAsync(CancellationToken cancellationToken = default)
    {
        Calls.Add("check");
        return Task.FromResult(ModelStatus);
    }

    public IReadOnlyList<string> FindInvoiceFiles(string folder)
    {
        Calls.Add("find");
        return Files.Select(f => System.IO.Path.Combine(folder, f)).ToList();
    }

    public Task<InvoiceAnalysisResult> AnalyzeAsync(string filePath, CancellationToken cancellationToken = default)
    {
        Calls.Add("analyze:" + System.IO.Path.GetFileName(filePath));
        return Task.FromResult(Analyze(filePath));
    }
}

internal static class Results
{
    public static InvoiceData AmazonInvoice() => new(
        "Amazon", "DE5ABC123", new DateOnly(2026, 11, 8), "EUR", 32.98m,
        [
            new LineItem("SanDisk USB stick 64 GB", 1, 12.99m, "IT-Hardware"),
            new LineItem("Anker USB power adapter", 1, 19.99m, "IT-Hardware"),
        ]);

    public static InvoiceAnalysisResult Ok(string path) => new()
    {
        FilePath = path,
        Status = AnalysisStatus.Success,
        PdfType = PdfType.Digital,
        Invoice = AmazonInvoice(),
        InvoiceCategory = "IT-Hardware",
        Duration = TimeSpan.FromSeconds(1.5),
    };

    public static InvoiceAnalysisResult Review(string path) => Ok(path) with
    {
        Status = AnalysisStatus.NeedsReview,
        HasMixedCategories = true,
        Warnings = ["Items belong to several categories – 'IT-Hardware' was chosen by the highest amount."],
    };

    public static InvoiceAnalysisResult Skipped(string path) => new()
    {
        FilePath = path,
        Status = AnalysisStatus.Skipped,
        PdfType = PdfType.Scan,
        Message = "Scan without a text layer – cannot be read without OCR.",
    };

    public static InvoiceAnalysisResult Failed(string path) => new()
    {
        FilePath = path,
        Status = AnalysisStatus.Failed,
        Message = "Ollama did not answer in time.",
    };
}
