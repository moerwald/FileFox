# Specification – Feature 1: Invoice Analysis (CLI)

| | |
|---|---|
| Status | Accepted for implementation |
| Command | `filefox analyze [folder]` |
| Sources | User requirements from the planning conversation, [01-overview.md](../plan/01-overview.md), [03-scan-detection.md](../plan/03-scan-detection.md), [05-architecture.md](../plan/05-architecture.md) |

## Goal
The user has a folder with invoice PDFs. FileFox reads every invoice with a local language model and shows on the command line:
the invoice data, every line item, and the category the model suggests for each item.
Example from the user: a USB stick and a USB power adapter bought at Amazon are both **IT hardware**.

Feature 1 only *shows* information. It never changes files.

## Conventions
- Each requirement has an ID (`F1-xx`). Tests reference it with `[Trait("Spec", "F1-xx")]`.
- "Default" means the value in the shipped `appsettings.json`.
- Requirements marked *(manual)* cannot be covered by automated tests and are checked by hand.

---

## 1 Command and input

| ID | Requirement |
|----|-------------|
| F1-01 | The command is `filefox analyze [folder]` with at most one folder argument. The argument is optional (see F1-08). |
| F1-02 | If the folder (from the argument or from the setting, see F1-08) does not exist, FileFox shows an error that names the folder and exits with a non-zero exit code. No invoice is analyzed. |
| F1-03 | Only PDF files directly inside the folder are considered. Subfolders are ignored. The `.pdf` extension is matched case-insensitively. |
| F1-04 | Files whose name matches one of `FileFox:StatementFileNamePatterns` (wildcards `*` and `?`, case-insensitive) are bank statements and are **not** analyzed. Default patterns: `*Kontoauszug*`, `*statement*`. |
| F1-05 | Invoices are processed in alphabetical order of their file name (case-insensitive). |
| F1-06 | If the folder contains no invoice, FileFox says so and exits with code 0. |
| F1-07 | FileFox does not create, change, rename or delete any file in the folder. |
| F1-08 | If no folder argument is given, FileFox uses the folder in `FileFox:InvoiceFolder`. A folder argument always wins over the setting. Environment variables written as `%NAME%` in the setting are expanded (e.g. `%USERPROFILE%\Documents\Invoices`). Default: empty. |
| F1-09 | If no folder argument is given and `FileFox:InvoiceFolder` is empty, FileFox shows an error that explains both ways to set the folder and exits with a non-zero exit code. No invoice is analyzed. |

## 2 Language model check

| ID | Requirement |
|----|-------------|
| F1-10 | Before any invoice is analyzed, FileFox checks that Ollama is reachable at `Ollama:BaseUrl` and that the model `Ollama:Model` is installed. |
| F1-11 | If Ollama is not reachable, FileFox shows a red message that tells the user to start Ollama, analyzes no invoice and exits with code 1. |
| F1-12 | If the model is not installed, FileFox shows a red message with the fix (`ollama pull <model>`) and the list of installed models, analyzes no invoice and exits with code 1. |
| F1-13 | Model names are compared like Ollama does: case-insensitive, and a name without a tag means `:latest` (`qwen2.5` = `qwen2.5:latest`, but `qwen2.5` ≠ `qwen2.5:7b`). |

## 3 PDF type (scan detection)

| ID | Requirement |
|----|-------------|
| F1-20 | A page has **readable text** if it has at least `MinCharactersPerPage` (default 20) non-whitespace characters **and** at least `MinTextQuality` (default 0.5) of these characters are letters or digits. |
| F1-21 | A page is an **image page** if images cover at least `LargeImageCoverage` (default 0.5) of its area. |
| F1-22 | A PDF without any page with readable text is a **scan**. It is skipped, is **not** sent to the language model, and is shown in red as "Skipped". |
| F1-23 | A PDF with readable text and at least one image page is a **scan with text layer**. It is analyzed, but marked for review. |
| F1-24 | A PDF with readable text and no image page is a **digital PDF**. |
| F1-25 | A file that cannot be read as PDF (corrupt, encrypted, not a PDF) is reported as failed. The other invoices are still analyzed. |

## 4 Invoice data extraction

| ID | Requirement |
|----|-------------|
| F1-30 | For each analyzed invoice FileFox shows: company (the seller, not the customer), invoice number, invoice date, total amount with currency, PDF type and the time the analysis took. |
| F1-31 | For each invoice FileFox shows every line item with description, quantity, amount and category. Subtotals, tax lines, payment information and free shipping are not line items. |
| F1-32 | The invoice text sent to the model is limited to `Ollama:MaxInvoiceCharacters`. |
| F1-33 | The request to Ollama is not streamed and uses `Ollama:Model`, `Ollama:Temperature`, `Ollama:ContextLength` and a JSON schema that describes the expected answer. |
| F1-34 | Amounts are understood in German and English notation, with or without currency symbol (`12,34`, `12.34`, `1.234,56 €`, `1,234.56`, `-5,00`). Dates are understood as `YYYY-MM-DD`, `DD.MM.YYYY` and `DD/MM/YYYY`. |
| F1-35 | Missing values are shown as "–"; they do not cause an error. Line items without a description are ignored. |
| F1-36 | If the model answer is not a JSON object, the invoice is reported as failed with a message that mentions the language model. The other invoices are still analyzed. |
| F1-37 | If Ollama returns an error or does not answer within `Ollama:RequestTimeoutSeconds`, the invoice is reported as failed with the reason. The other invoices are still analyzed. |

## 5 Categories

| ID | Requirement |
|----|-------------|
| F1-40 | Every line item gets exactly one category. The model may only choose from `FileFox:Categories` plus `FileFox:FallbackCategory`. |
| F1-41 | Categories are matched case-insensitively and shown with the configured spelling. An unknown or empty category becomes `FallbackCategory`. |
| F1-42 | Each category has a description with examples, which is given to the model. In the default settings, USB sticks and USB power adapters belong to `IT-Hardware`. |
| F1-43 | The category of the whole invoice is the category with the highest sum of line item amounts. If no item has an amount, the category with the most items wins. A tie is decided by the order in the settings. An invoice without items gets `FallbackCategory`. |
| F1-44 | If the items belong to more than one category, the invoice is marked as **mixed** and needs review. |

## 6 Review

| ID | Requirement |
|----|-------------|
| F1-50 | An analyzed invoice **needs review** (with a warning that says why) if any of these is true: scan with text layer, mixed categories, no invoice number, no company, no line items. Otherwise it is **OK**. |

## 7 Output

| ID | Requirement |
|----|-------------|
| F1-60 | Colors: green = OK, yellow = needs review, red = skipped or failed. |
| F1-61 | Text from invoices and from the model is shown literally. Characters like `[` and `]` must not break the output. |
| F1-62 | At the end FileFox shows a summary with the number of invoices per result (OK, needs review, skipped, failed) and the total time. |
| F1-63 | Colors can be turned off with the environment variable `NO_COLOR`. *(manual)* |

## 8 Exit codes

| ID | Requirement |
|----|-------------|
| F1-70 | Exit code 0: all invoices were analyzed, skipped or need review, and none failed. |
| F1-71 | Exit code 1: no usable folder (F1-02, F1-09), the model check failed (F1-11, F1-12) or at least one invoice failed. |
| F1-72 | Exit code 130: the user cancelled with Ctrl+C. *(manual)* |

## 9 Architecture and configuration

| ID | Requirement |
|----|-------------|
| F1-80 | The core library (`FileFox.Core`) does not depend on the CLI, Spectre.Console, PdfPig or HTTP libraries. |
| F1-81 | The CLI command only uses the core service `IInvoiceAnalysisService` for business functions; it contains no business logic. |
| F1-82 | All components are wired with `Microsoft.Extensions.DependencyInjection`. |
| F1-83 | All settings above are read from `appsettings.json` next to the executable and can be overridden with environment variables prefixed `FILEFOX_` (e.g. `FILEFOX_Ollama__Model`). |
| F1-84 | FileFox works fully offline; the only network target is the configured Ollama server. *(manual / by design)* |
