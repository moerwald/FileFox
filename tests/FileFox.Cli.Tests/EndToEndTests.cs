using System.Security.Cryptography;
using FileFox.Core.Abstractions;
using FileFox.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FileFox.Cli.Tests;

/// <summary>
/// Runs the complete application – real DI wiring, real file system and real PdfPig – with only
/// the language model replaced by a fake.
/// </summary>
public sealed class EndToEndTests : IDisposable
{
    private readonly TempFolder _folder = new();
    private readonly FakeModel _model = new();

    public void Dispose() => _folder.Dispose();

    private sealed class FakeModel : IInvoiceAnalyzer, ILanguageModelStatusProvider
    {
        public List<string> ReceivedTexts { get; } = [];

        public Task<ModelStatus> CheckAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new ModelStatus(true, "fake", "Model fake is ready."));

        public Task<InvoiceData> AnalyzeAsync(string invoiceText, CancellationToken cancellationToken = default)
        {
            ReceivedTexts.Add(invoiceText);
            return Task.FromResult(new InvoiceData(
                "Amazon", "INV-2026-0042", new DateOnly(2026, 11, 8), "EUR", 32.98m,
                [
                    new LineItem("SanDisk USB stick 64 GB", 1, 12.99m, "it-hardware"),
                    new LineItem("Anker USB power adapter", 1, 19.99m, "IT-Hardware"),
                ]));
        }
    }

    private Task<(int ExitCode, string Output)> RunAnalyze() =>
        CliTestHost.RunAsync(
            services =>
            {
                services.AddSingleton<IInvoiceAnalyzer>(_model);
                services.AddSingleton<ILanguageModelStatusProvider>(_model);
            },
            "analyze", _folder.Path);

    private Dictionary<string, string> Snapshot() =>
        Directory.EnumerateFiles(_folder.Path, "*", SearchOption.AllDirectories)
            .ToDictionary(f => Path.GetRelativePath(_folder.Path, f), f => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(f))));

    private void CreateSampleFolder()
    {
        SamplePdf.Write(_folder.File("Amazon.pdf"), "Amazon EU Invoice number INV-2026-0042 SanDisk USB stick 64 GB 12.99 EUR");
        SamplePdf.Write(_folder.File("Kontoauszug_2026_11.pdf"), "Kontoauszug Girokonto 08.11.2026 Amazon Payments -32,98 EUR");
        SamplePdf.Write(_folder.File("scan.pdf"), "");
        File.WriteAllText(_folder.File("broken.pdf"), "this is not a pdf");
        File.WriteAllText(_folder.File("notes.txt"), "not a pdf at all");
    }

    [Fact]
    [Trait("Spec", "F1-03")]
    [Trait("Spec", "F1-04")]
    [Trait("Spec", "F1-22")]
    [Trait("Spec", "F1-82")]
    public async Task Only_real_invoices_reach_the_language_model()
    {
        CreateSampleFolder();

        await RunAnalyze();

        var text = Assert.Single(_model.ReceivedTexts);
        Assert.Contains("INV-2026-0042", text);
    }

    [Fact]
    [Trait("Spec", "F1-04")]
    [Trait("Spec", "F1-05")]
    [Trait("Spec", "F1-22")]
    [Trait("Spec", "F1-25")]
    [Trait("Spec", "F1-71")]
    public async Task Every_invoice_gets_a_result_in_alphabetical_order()
    {
        CreateSampleFolder();

        var (exitCode, output) = await RunAnalyze();

        Assert.Equal(1, exitCode); // broken.pdf failed
        Assert.DoesNotContain("Kontoauszug", output);
        Assert.DoesNotContain("notes.txt", output);

        var amazon = output.IndexOf("Amazon.pdf", StringComparison.Ordinal);
        var broken = output.IndexOf("broken.pdf", StringComparison.Ordinal);
        var scan = output.IndexOf("scan.pdf", StringComparison.Ordinal);
        Assert.True(amazon >= 0 && amazon < broken && broken < scan, "Expected order: Amazon.pdf, broken.pdf, scan.pdf");

        Assert.Contains("Scan (no text)", output);
        Assert.Contains("Failed", output);
    }

    [Fact]
    [Trait("Spec", "F1-31")]
    [Trait("Spec", "F1-41")]
    [Trait("Spec", "F1-42")]
    public async Task Usb_stick_and_usb_power_adapter_are_shown_as_it_hardware()
    {
        SamplePdf.Write(_folder.File("Amazon.pdf"), "Amazon EU Invoice number INV-2026-0042 SanDisk USB stick 64 GB 12.99 EUR");

        var (exitCode, output) = await RunAnalyze();

        Assert.Equal(0, exitCode);
        Assert.Contains("SanDisk USB stick 64 GB", output);
        Assert.Contains("Anker USB power adapter", output);
        Assert.DoesNotContain("it-hardware", output); // normalized to the configured spelling
        Assert.Contains("IT-Hardware", output);
        Assert.DoesNotContain("mixed", output);
    }

    [Fact]
    [Trait("Spec", "F1-08")]
    [Trait("Spec", "F1-82")]
    public async Task Configured_invoice_folder_is_analyzed_without_an_argument()
    {
        SamplePdf.Write(_folder.File("Amazon.pdf"), "Amazon EU Invoice number INV-2026-0042 SanDisk USB stick 64 GB 12.99 EUR");

        var (exitCode, output) = await CliTestHost.RunAsync(
            services =>
            {
                services.AddSingleton<IInvoiceAnalyzer>(_model);
                services.AddSingleton<ILanguageModelStatusProvider>(_model);
            },
            new Dictionary<string, string?> { ["FileFox:InvoiceFolder"] = _folder.Path },
            "analyze");

        Assert.Equal(0, exitCode);
        Assert.Contains("Amazon.pdf", output);
        Assert.Single(_model.ReceivedTexts);
    }

    [Fact]
    [Trait("Spec", "F1-07")]
    public async Task No_file_in_the_folder_is_changed()
    {
        CreateSampleFolder();
        var before = Snapshot();

        await RunAnalyze();

        Assert.Equal(before, Snapshot());
    }
}
