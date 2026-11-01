# 05 – Architecture

## Principles
1. **Thin presentation layer.** The CLI and the later WPF UI only collect input, call the core and display results. They contain no business logic.
2. **Business logic in a core library** that has no reference to any UI framework.
3. **Interfaces everywhere** between components, so implementations can be swapped and tested.
4. **Dependency injection** with `Microsoft.Extensions.DependencyInjection` (and `Microsoft.Extensions.Hosting` for configuration and logging).
5. **CLI first, WPF later.** Both front ends use the same core and the same DI registration.

## Solution layout

```
FileFox.sln
src/
  FileFox.Core/            Domain models, interfaces, business logic (no UI, no PdfPig, no HTTP)
  FileFox.Infrastructure/  Implementations: PdfPig text extraction, Ollama client, file system
  FileFox.Cli/             Version 1: command-line front end (colored output)
  FileFox.Wpf/             Later: WPF front end
tests/
  FileFox.Core.Tests/
  FileFox.Infrastructure.Tests/
```

Dependency direction:

```
FileFox.Cli ─┐
             ├─► FileFox.Core ◄── FileFox.Infrastructure
FileFox.Wpf ─┘
```

The front ends reference Core and Infrastructure only to register services. Core references nothing else in the solution.

## Core interfaces (first draft)

| Interface | Responsibility |
|-----------|----------------|
| `IFolderReader` | List the PDF files in a folder (file system access) |
| `IInvoiceFileLocator` | Pick the invoice files, skip bank statements |
| `IPdfTextExtractor` | Extract text per page from a PDF |
| `IPdfTypeDetector` | Classify a PDF as real PDF, scan with text layer, or scan |
| `IStatementParser` | Turn the bank statement text into booking lines |
| `IInvoiceAnalyzer` | Extract company, invoice number, amount and line items with categories (Ollama implementation) |
| `IInvoiceCategoryResolver` | Map item categories to the configured list and pick the invoice category |
| `ILanguageModelStatusProvider` | Check that Ollama runs and the model is installed |
| `IBookingMatcher` | Find the booking that belongs to an invoice |
| `IFileNameBuilder` | Build `MM-DD_InvoiceNumber_Company-Category.pdf` and clean invalid characters |
| `IFileRenamer` | Rename files safely and write the undo log |
| `IInvoiceAnalysisService` | Orchestrates the analysis; the only service the front ends call |
| `IProgress<T>` | Progress reporting from Core to the front end (standard .NET) |

Models (examples): `InvoiceInfo`, `Booking`, `RenameProposal`, `PdfType`, `FileFoxOptions`.

## Command-line front end (version 1)
- Colored output with **Spectre.Console** (tables, colors, progress bars, prompts).
- Suggested commands:
  - `filefox analyze <folder>` – show the rename proposals as a table, change nothing
  - `filefox rename <folder>` – show proposals, ask for confirmation, then rename
  - `filefox undo <folder>` – undo the last rename using the log file
- Colors: green = matched, yellow = needs review, red = scan / no booking / error.
- Command parsing with `Spectre.Console.Cli` (works with DI).

## Configuration
`appsettings.json` (bound to `FileFoxOptions`):
- Ollama URL and model name
- category list
- scan detection threshold
- bank statement file name pattern

## Testing
- Core logic is tested with fake implementations of the interfaces (no Ollama, no real PDFs needed).
- Infrastructure is tested with a few sample PDFs.
