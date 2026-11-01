using System.Globalization;
using FileFox.Core.Models;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace FileFox.Cli.Rendering;

/// <summary>Writes analysis results to the console with colors. Display only – no business logic.</summary>
internal sealed class InvoiceRenderer(IAnsiConsole console)
{
    // Colors: green = OK, yellow = please review, red = skipped or error.
    private const string Ok = "green";
    private const string Review = "yellow";
    private const string Problem = "red";
    private const string Label = "grey";
    private const string CategoryColor = "deepskyblue1";

    public void RenderHeader(string folder)
    {
        console.Write(new Rule("[bold orange1]FileFox[/] – invoice analysis") { Justification = Justify.Left });
        console.MarkupLine($"[{Label}]Folder:[/] {Markup.Escape(folder)}");
    }

    public void RenderModelStatus(ModelStatus status)
    {
        var color = status.IsAvailable ? Ok : Problem;
        var icon = status.IsAvailable ? "✓" : "✗";
        console.MarkupLine($"[{color}]{icon} {Markup.Escape(status.Message)}[/]");
        console.WriteLine();
    }

    public void RenderNoInvoicesFound() =>
        console.MarkupLine($"[{Review}]No invoice PDFs found in this folder.[/]");

    public void RenderCancelled() =>
        console.MarkupLine($"[{Review}]Cancelled.[/]");

    public void RenderInvoice(InvoiceAnalysisResult result)
    {
        var statusColor = StatusColor(result.Status);
        console.Write(new Rule($"[bold {statusColor}]{Markup.Escape(result.FileName)}[/]") { Justification = Justify.Left });

        if (result.Invoice is null)
        {
            var label = result.Status == AnalysisStatus.Skipped ? "Skipped" : "Failed";
            console.MarkupLine($"[{statusColor}]{label}:[/] {Markup.Escape(result.Message ?? "Unknown error.")}");
            RenderPdfType(result);
            console.WriteLine();
            return;
        }

        var invoice = result.Invoice;
        var grid = new Grid().AddColumn(new GridColumn().NoWrap()).AddColumn();
        AddRow(grid, "Company", Plain(invoice.Company));
        AddRow(grid, "Invoice number", Plain(invoice.InvoiceNumber));
        AddRow(grid, "Invoice date", Plain(invoice.InvoiceDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
        AddRow(grid, "Total", Plain(FormatAmount(invoice.TotalAmount, invoice.Currency)));
        AddRow(grid, "Category", CategoryMarkup(result));
        AddRow(grid, "PDF type", new Markup(PdfTypeMarkup(result.PdfType)));
        AddRow(grid, "Duration", Plain($"{result.Duration.TotalSeconds:0.0} s"));
        console.Write(grid);

        if (invoice.LineItems.Count > 0)
        {
            console.Write(BuildLineItemTable(invoice));
        }

        foreach (var warning in result.Warnings)
        {
            console.MarkupLine($"[{Review}]! {Markup.Escape(warning)}[/]");
        }

        console.WriteLine();
    }

    public void RenderSummary(IReadOnlyCollection<InvoiceAnalysisResult> results)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold]Summary[/]")
            .AddColumn("Result")
            .AddColumn(new TableColumn("Files").RightAligned());

        AddSummaryRow(table, "OK", Ok, results.Count(r => r.Status == AnalysisStatus.Success));
        AddSummaryRow(table, "Please review", Review, results.Count(r => r.Status == AnalysisStatus.NeedsReview));
        AddSummaryRow(table, "Skipped (scan)", Problem, results.Count(r => r.Status == AnalysisStatus.Skipped));
        AddSummaryRow(table, "Failed", Problem, results.Count(r => r.Status == AnalysisStatus.Failed));

        var totalSeconds = results.Sum(r => r.Duration.TotalSeconds);
        table.Caption($"[{Label}]Total time: {totalSeconds:0.0} s[/]");

        console.Write(table);
    }

    private static Table BuildLineItemTable(InvoiceData invoice)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("#").RightAligned())
            .AddColumn("Item")
            .AddColumn(new TableColumn("Qty").RightAligned())
            .AddColumn(new TableColumn("Amount").RightAligned())
            .AddColumn("Category");

        var index = 1;
        foreach (var item in invoice.LineItems)
        {
            table.AddRow(
                Plain(index++.ToString(CultureInfo.CurrentCulture)),
                Plain(item.Description),
                Plain(item.Quantity?.ToString("0.##", CultureInfo.CurrentCulture)),
                Plain(FormatAmount(item.TotalPrice, invoice.Currency)),
                new Markup($"[{CategoryColor}]{Markup.Escape(item.Category)}[/]"));
        }

        return table;
    }

    private static void AddSummaryRow(Table table, string label, string color, int count)
    {
        // Rows with zero files are grey so the eye goes to what matters.
        var effective = count == 0 ? Label : color;
        table.AddRow(new Markup($"[{effective}]{label}[/]"), new Markup($"[{effective}]{count}[/]"));
    }

    private void RenderPdfType(InvoiceAnalysisResult result)
    {
        if (result.PdfType is not null)
        {
            console.MarkupLine($"[{Label}]PDF type:[/] {PdfTypeMarkup(result.PdfType)}");
        }
    }

    private static IRenderable CategoryMarkup(InvoiceAnalysisResult result)
    {
        var category = Markup.Escape(result.InvoiceCategory ?? "–");
        return result.HasMixedCategories
            ? new Markup($"[bold {Review}]{category}[/] [{Review}](mixed – please review)[/]")
            : new Markup($"[bold {Ok}]{category}[/]");
    }

    private static string PdfTypeMarkup(PdfType? type) => type switch
    {
        PdfType.Digital => $"[{Ok}]Digital PDF[/]",
        PdfType.ScanWithTextLayer => $"[{Review}]Scan with text layer[/]",
        PdfType.Scan => $"[{Problem}]Scan (no text)[/]",
        _ => $"[{Label}]unknown[/]",
    };

    internal static string StatusColor(AnalysisStatus status) => status switch
    {
        AnalysisStatus.Success => Ok,
        AnalysisStatus.NeedsReview => Review,
        _ => Problem,
    };

    private static void AddRow(Grid grid, string label, IRenderable value) =>
        grid.AddRow(new Markup($"[{Label}]{label}:[/]"), value);

    /// <summary>Plain text – never interpreted as markup, so brackets in invoice texts are safe.</summary>
    private static IRenderable Plain(string? value) =>
        new Text(string.IsNullOrWhiteSpace(value) ? "–" : value);

    private static string FormatAmount(decimal? amount, string? currency) =>
        amount is null
            ? "–"
            : $"{amount.Value.ToString("N2", CultureInfo.CurrentCulture)} {currency}".TrimEnd();
}
