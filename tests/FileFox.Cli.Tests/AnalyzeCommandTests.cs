using FileFox.Core.Abstractions;
using FileFox.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FileFox.Cli.Tests;

/// <summary>Behavior of <c>filefox analyze</c> with a fake core service.</summary>
public sealed class AnalyzeCommandTests : IDisposable
{
    private readonly TempFolder _folder = new();
    private readonly FakeAnalysisService _service = new();

    public void Dispose() => _folder.Dispose();

    private Task<(int ExitCode, string Output)> RunAnalyze(string? folder = null) =>
        CliTestHost.RunAsync(s => s.AddSingleton<IInvoiceAnalysisService>(_service), "analyze", folder ?? _folder.Path);

    private Task<(int ExitCode, string Output)> RunAnalyzeWithoutArgument() =>
        CliTestHost.RunAsync(s => s.AddSingleton<IInvoiceAnalysisService>(_service), "analyze");

    [Fact]
    [Trait("Spec", "F1-02")]
    [Trait("Spec", "F1-71")]
    public async Task Missing_folder_is_an_error_and_nothing_is_analyzed()
    {
        var missing = Path.Combine(_folder.Path, "does-not-exist");

        var (exitCode, output) = await RunAnalyze(missing);

        Assert.Equal(1, exitCode);
        Assert.Contains("Folder not found", output);
        Assert.Contains("does-not-exist", output);
        Assert.Equal(new[] { "resolve" }, _service.Calls);
    }

    [Fact]
    [Trait("Spec", "F1-01")]
    [Trait("Spec", "F1-08")]
    public async Task Without_argument_the_configured_folder_is_analyzed()
    {
        _service.ConfiguredFolder = _folder.Path;
        _service.Files = ["a.pdf"];

        var (exitCode, output) = await RunAnalyzeWithoutArgument();

        Assert.Equal(0, exitCode);
        Assert.Equal(new[] { "resolve", "check", "find", "analyze:a.pdf" }, _service.Calls);
        Assert.Contains(Path.GetFileName(_folder.Path), output);
    }

    [Fact]
    [Trait("Spec", "F1-08")]
    public async Task Folder_argument_wins_over_the_configured_folder()
    {
        using var other = new TempFolder();
        _service.ConfiguredFolder = other.Path;

        var (exitCode, output) = await RunAnalyze();

        Assert.Equal(0, exitCode);
        Assert.Contains(Path.GetFileName(_folder.Path), output);
        Assert.DoesNotContain(Path.GetFileName(other.Path), output);
    }

    [Fact]
    [Trait("Spec", "F1-09")]
    [Trait("Spec", "F1-71")]
    public async Task Without_argument_and_setting_an_error_explains_both_ways()
    {
        var (exitCode, output) = await RunAnalyzeWithoutArgument();

        Assert.Equal(1, exitCode);
        Assert.Contains("No folder given", output);
        Assert.Contains("filefox analyze <folder>", output);
        Assert.Contains("InvoiceFolder", output);
        Assert.Equal(new[] { "resolve" }, _service.Calls);
    }

    [Fact]
    [Trait("Spec", "F1-10")]
    public async Task Model_is_checked_before_any_invoice_is_analyzed()
    {
        _service.Files = ["a.pdf", "b.pdf"];

        await RunAnalyze();

        Assert.Equal(new[] { "resolve", "check", "find", "analyze:a.pdf", "analyze:b.pdf" }, _service.Calls);
    }

    [Fact]
    [Trait("Spec", "F1-11")]
    [Trait("Spec", "F1-12")]
    [Trait("Spec", "F1-71")]
    public async Task Unavailable_model_stops_with_exit_code_1()
    {
        _service.Files = ["a.pdf"];
        _service.ModelStatus = new ModelStatus(false, "qwen2.5:7b", "Model qwen2.5:7b is not installed. Run: ollama pull qwen2.5:7b");

        var (exitCode, output) = await RunAnalyze();

        Assert.Equal(1, exitCode);
        Assert.Contains("ollama pull qwen2.5:7b", output);
        Assert.Equal(new[] { "resolve", "check" }, _service.Calls);
    }

    [Fact]
    [Trait("Spec", "F1-06")]
    public async Task Folder_without_invoices_is_reported_with_exit_code_0()
    {
        var (exitCode, output) = await RunAnalyze();

        Assert.Equal(0, exitCode);
        Assert.Contains("No invoice PDFs found", output);
    }

    [Fact]
    [Trait("Spec", "F1-30")]
    [Trait("Spec", "F1-31")]
    [Trait("Spec", "F1-70")]
    public async Task Invoice_data_and_every_line_item_with_category_are_shown()
    {
        _service.Files = ["amazon.pdf"];

        var (exitCode, output) = await RunAnalyze();

        Assert.Equal(0, exitCode);
        Assert.Contains("amazon.pdf", output);
        Assert.Contains("Amazon", output);
        Assert.Contains("DE5ABC123", output);
        Assert.Contains("2026-11-08", output);
        Assert.Contains("EUR", output);
        Assert.Contains("Digital PDF", output);
        Assert.Contains("SanDisk USB stick 64 GB", output);
        Assert.Contains("Anker USB power adapter", output);
        Assert.Contains("IT-Hardware", output);
    }

    [Fact]
    [Trait("Spec", "F1-62")]
    public async Task Summary_counts_every_result()
    {
        _service.Files = ["ok.pdf", "review.pdf", "scan.pdf"];
        _service.Analyze = path => Path.GetFileName(path) switch
        {
            "review.pdf" => Results.Review(path),
            "scan.pdf" => Results.Skipped(path),
            _ => Results.Ok(path),
        };

        var (_, output) = await RunAnalyze();

        Assert.Contains("Summary", output);
        Assert.Matches(@"OK\s*[│|]\s*1", output);
        Assert.Matches(@"Please review\s*[│|]\s*1", output);
        Assert.Matches(@"Skipped \(scan\)\s*[│|]\s*1", output);
        Assert.Matches(@"Failed\s*[│|]\s*0", output);
        Assert.Contains("Total time", output);
    }

    [Fact]
    [Trait("Spec", "F1-50")]
    [Trait("Spec", "F1-70")]
    public async Task Review_and_skipped_invoices_still_exit_with_0_and_show_why()
    {
        _service.Files = ["review.pdf", "scan.pdf"];
        _service.Analyze = path => path.EndsWith("scan.pdf") ? Results.Skipped(path) : Results.Review(path);

        var (exitCode, output) = await RunAnalyze();

        Assert.Equal(0, exitCode);
        Assert.Contains("several categories", output);
        Assert.Contains("mixed – please review", output);
        Assert.Contains("Skipped", output);
        Assert.Contains("Scan without a text layer", output);
    }

    [Fact]
    [Trait("Spec", "F1-37")]
    [Trait("Spec", "F1-71")]
    public async Task One_failed_invoice_gives_exit_code_1_but_the_others_are_still_analyzed()
    {
        _service.Files = ["a.pdf", "b.pdf", "c.pdf"];
        _service.Analyze = path => path.EndsWith("b.pdf") ? Results.Failed(path) : Results.Ok(path);

        var (exitCode, output) = await RunAnalyze();

        Assert.Equal(1, exitCode);
        Assert.Contains("analyze:c.pdf", _service.Calls);
        Assert.Contains("Failed", output);
        Assert.Contains("did not answer in time", output);
    }

    [Fact]
    [Trait("Spec", "F1-35")]
    public async Task Missing_values_are_shown_as_dash()
    {
        _service.Files = ["x.pdf"];
        _service.Analyze = path => Results.Ok(path) with
        {
            Invoice = new InvoiceData("", "", null, null, null, [new LineItem("Cable", null, null, "Other")]),
        };

        var (exitCode, output) = await RunAnalyze();

        Assert.Equal(0, exitCode);
        Assert.Matches(@"Invoice number:\s+–", output);
        Assert.Matches(@"Invoice date:\s+–", output);
        Assert.Matches(@"Total:\s+–", output);
    }

    [Fact]
    [Trait("Spec", "F1-61")]
    public async Task Brackets_in_invoice_texts_are_shown_literally()
    {
        _service.Files = ["[draft] invoice.pdf"];
        _service.Analyze = path => Results.Ok(path) with
        {
            Invoice = Results.AmazonInvoice() with
            {
                Company = "[ACME] GmbH",
                LineItems = [new LineItem("HDMI cable [2 m]", 1, 9.99m, "IT-Hardware")],
            },
            Warnings = ["Check [this]"],
        };

        var (exitCode, output) = await RunAnalyze();

        Assert.Equal(0, exitCode);
        Assert.Contains("[draft] invoice.pdf", output);
        Assert.Contains("[ACME] GmbH", output);
        Assert.Contains("HDMI cable [2 m]", output);
        Assert.Contains("Check [this]", output);
    }
}
