# FileFox

FileFox is an offline Windows tool that brings order to invoice PDFs.
It reads invoices with a local language model (via [Ollama](https://ollama.com)), shows the invoice data and line items, and suggests a category for every item.

Later versions will match invoices to a bank statement and rename them to `MM-DD_InvoiceNumber_Company-Category.pdf` (see [docs/plan](docs/plan/README.md)).

## Status

**Feature 1 – invoice analysis (CLI):** `filefox analyze <folder>` shows, for every invoice PDF in a folder:

- company, invoice number, invoice date, total
- every line item with quantity, amount and the category suggested by the model
- the category of the whole invoice (the category with the highest amount)
- whether the PDF is a digital PDF or a scan

Nothing is renamed or changed yet.

## Requirements

- Windows (Linux and macOS work too)
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Ollama](https://ollama.com) **0.5 or newer** (structured JSON output), running locally
- The model from `appsettings.json`, by default:

  ```
  ollama pull qwen2.5:7b
  ```

No GPU needed. A 7B model needs about 5 GB RAM and takes roughly 5–30 seconds per invoice on a CPU.
For more speed use `qwen2.5:3b`.

## Build and run

```
dotnet build
dotnet run --project src/FileFox.Cli -- analyze "C:\Invoices\2026-11"
```

Or publish a single executable:

```
dotnet publish src/FileFox.Cli -c Release -r win-x64 --self-contained false -o publish
publish\filefox.exe analyze "C:\Invoices\2026-11"
```

Files whose name matches `*Kontoauszug*` or `*statement*` are treated as bank statements and skipped.

### Colors

| Color | Meaning |
|-------|---------|
| Green | OK |
| Yellow | Please review (mixed categories, scan with text layer, missing invoice number, …) |
| Red | Scan without text (skipped) or error |

Set the environment variable `NO_COLOR=1` to turn colors off.

### Exit codes

| Code | Meaning |
|------|---------|
| 0 | All invoices analyzed (some may need review) |
| 1 | Ollama/model not available, or at least one invoice failed |
| 130 | Cancelled with Ctrl+C |

## Configuration

All settings live in `src/FileFox.Cli/appsettings.json` (copied next to the executable).

| Setting | Meaning |
|---------|---------|
| `Ollama:BaseUrl` | Ollama address, default `http://localhost:11434` |
| `Ollama:Model` | Model name as shown by `ollama list` |
| `Ollama:Temperature` | 0 = stable, repeatable answers |
| `Ollama:ContextLength` | Context window (`num_ctx`); increase for very long invoices |
| `Ollama:RequestTimeoutSeconds` | Max. time per invoice |
| `Ollama:MaxInvoiceCharacters` | Longer invoice texts are truncated |
| `FileFox:Categories` | Category names plus a description with examples – the description is what the model uses to decide |
| `FileFox:FallbackCategory` | Used when the model returns an unknown category |
| `FileFox:StatementFileNamePatterns` | File names that are bank statements, not invoices |
| `FileFox:ScanDetection` | Thresholds for telling digital PDFs and scans apart |

Every setting can be overridden with an environment variable, e.g.
`set FILEFOX_Ollama__Model=qwen2.5:3b`.

## Tests

[![Build and test](https://github.com/moerwald/FileFox/actions/workflows/build.yml/badge.svg)](https://github.com/moerwald/FileFox/actions/workflows/build.yml)

```
dotnet test
```

The tests use fake implementations and need neither Ollama nor real invoices.

Tests are derived from the specification in [docs/specs](docs/specs/feature-1-invoice-analysis.md).
Every test is tagged with the requirement it verifies, so you can run the tests for one requirement:

```
dotnet test --filter "Spec=F1-42"
```

## Architecture

```
src/
  FileFox.Core/            Models, interfaces, business logic (no UI, no PDF library, no HTTP)
  FileFox.Infrastructure/  PdfPig text extraction, Ollama client, file system
  FileFox.Cli/             Command-line front end (Spectre.Console)
tests/
  FileFox.Core.Tests/
  FileFox.Infrastructure.Tests/
  FileFox.Cli.Tests/       CLI behavior, settings, wiring and end-to-end tests with real PDFs
```

The CLI only talks to `IInvoiceAnalysisService`. Everything is wired with dependency injection, so the planned WPF front end can reuse the same core. Details: [docs/plan/05-architecture.md](docs/plan/05-architecture.md).
