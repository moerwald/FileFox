using System.ComponentModel;
using FileFox.Cli.Rendering;
using FileFox.Core.Abstractions;
using FileFox.Core.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace FileFox.Cli.Commands;

/// <summary>
/// <c>filefox analyze [folder]</c> – shows invoice data, line items and suggested categories.
/// Presentation only: all logic lives in <see cref="IInvoiceAnalysisService"/>.
/// </summary>
internal sealed class AnalyzeCommand(
    IInvoiceAnalysisService analysisService,
    InvoiceRenderer renderer,
    IAnsiConsole console) : AsyncCommand<AnalyzeCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "[folder]")]
        [Description("Folder that contains the invoice PDFs. If left out, FileFox:InvoiceFolder from appsettings.json is used.")]
        public string? Folder { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var folder = analysisService.ResolveFolder(settings.Folder);
        if (!folder.IsValid)
        {
            renderer.RenderError(folder.Error!);
            return ExitCodes.Error;
        }

        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler onCancel = (_, e) =>
        {
            e.Cancel = true;
            cancellation.Cancel();
        };
        Console.CancelKeyPress += onCancel;

        try
        {
            return await RunAsync(folder.Folder!, cancellation.Token);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            renderer.RenderCancelled();
            return ExitCodes.Cancelled;
        }
        finally
        {
            Console.CancelKeyPress -= onCancel;
        }
    }

    private async Task<int> RunAsync(string folder, CancellationToken cancellationToken)
    {
        renderer.RenderHeader(folder);

        var modelStatus = await console.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Checking Ollama …", _ => analysisService.CheckModelAsync(cancellationToken));

        renderer.RenderModelStatus(modelStatus);
        if (!modelStatus.IsAvailable)
        {
            return ExitCodes.Error;
        }

        var files = analysisService.FindInvoiceFiles(folder);
        if (files.Count == 0)
        {
            renderer.RenderNoInvoicesFound();
            return ExitCodes.Success;
        }

        var results = new List<InvoiceAnalysisResult>(files.Count);
        for (var i = 0; i < files.Count; i++)
        {
            var file = files[i];
            var statusText = $"Analyzing [bold]{Markup.Escape(Path.GetFileName(file))}[/] ({i + 1}/{files.Count}) …";

            var result = await console.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync(statusText, _ => analysisService.AnalyzeAsync(file, cancellationToken));

            renderer.RenderInvoice(result);
            results.Add(result);
        }

        renderer.RenderSummary(results);
        return results.Any(r => r.Status == AnalysisStatus.Failed) ? ExitCodes.Error : ExitCodes.Success;
    }
}

internal static class ExitCodes
{
    public const int Success = 0;
    public const int Error = 1;
    public const int Cancelled = 130;
}
