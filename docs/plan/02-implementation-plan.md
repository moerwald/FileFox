# 02 – Implementation Plan

## Phase 1 – Preparation
- [ ] Install the .NET 10 SDK.
- [ ] Install Ollama for Windows.
- [ ] Pull a small model, e.g. `qwen2.5:7b` (or `qwen2.5:3b` for more speed).
- [ ] Test by hand: give the model the text of one invoice and check that company and invoice number are correct.

## Phase 2 – Solution structure and CLI
See [05-architecture.md](05-architecture.md) for the layering rules.
- [ ] Create the solution with `FileFox.Core`, `FileFox.Infrastructure`, `FileFox.Cli` and test projects, all targeting .NET 10.
- [ ] Define the core interfaces and models in `FileFox.Core`.
- [ ] Add **PdfPig** to `FileFox.Infrastructure` only.
- [ ] Set up dependency injection and configuration (`Microsoft.Extensions.Hosting`, `appsettings.json`).
- [ ] Build the CLI with **Spectre.Console** (colored output):
  - `analyze <folder>` – show proposals, change nothing
  - `rename <folder>` – show proposals, confirm, rename
  - `undo <folder>` – undo the last rename
- [ ] Proposal table columns: old file name, PDF type, company, invoice number, amount, category, booking date, new file name.
- [ ] Categories, model name and thresholds come from `appsettings.json`, not from code.

## Phase 3 – Read PDFs and detect scans
- [ ] Find the bank statement by its file name (e.g. contains "Kontoauszug" / "statement"); all other PDFs are invoices.
- [ ] Extract the text of every page with PdfPig.
- [ ] Classify each PDF as *real PDF*, *scan with text layer* or *scan* (details in [03-scan-detection.md](03-scan-detection.md)).
- [ ] Mark scans without text as "Scan – not readable" and skip them.
- [ ] If the bank statement is a scan, show a clear error message.

## Phase 4 – Ask the language model
- [ ] Send the invoice text over HTTP to Ollama (`/api/generate` or `/api/chat`).
- [ ] Request a JSON answer:
  ```json
  { "company": "...", "invoiceNumber": "...", "amount": "...", "category": "one of the list" }
  ```
- [ ] Use `format: "json"` and `temperature: 0` for stable answers.
- [ ] Increase the context length (`num_ctx`) so long invoices are not cut off.
- [ ] Run the analysis asynchronously so the UI does not freeze; show progress.
- [ ] Handle errors: Ollama not running, model missing, invalid JSON.

## Phase 5 – Match the booking in the bank statement
- [ ] Search the bank statement lines in this order:
  1. invoice number
  2. amount
  3. company name
- [ ] Take the booking date from the matching line (or a line just above it).
- [ ] Convert the date to `MM-DD`.
- [ ] Note: the line format depends on the bank. An anonymized sample line will make this step much more reliable. Optionally the model can parse the booking lines too.

## Phase 6 – Review and rename
- [ ] Show the proposed new name per invoice: `MM-DD_InvoiceNumber_Company-Category.pdf`.
- [ ] Mark uncertain rows with colors, e.g. yellow for "needs review", red for "no booking found" or "scan".
- [ ] CLI: let the user confirm or skip each uncertain row (interactive prompt).
- [ ] Rename only after the user confirms.
- [ ] Never overwrite an existing file.
- [ ] Replace characters that are not allowed in Windows file names.
- [ ] Write a log file (`old name → new name`) so every rename can be undone.

## Phase 7 – Testing
- [ ] Test with **copies** of 5–10 real invoices.
- [ ] Add one scanned PDF on purpose to test the scan detection.
- [ ] Compare 3B and 7B models; use 3B if it is accurate enough (faster).
- [ ] Add unit tests for: date parsing, file name building, scan detection, booking matching.

## Phase 8 – WPF GUI (later)
- [ ] Add `FileFox.Wpf` on top of the existing Core and Infrastructure – no new business logic.
- [ ] Reuse the same DI registration and `appsettings.json`.
- [ ] Folder picker, "Analyze" and "Rename" buttons, status line, editable proposal table.
