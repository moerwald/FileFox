# 02 – Implementation Plan

## Phase 1 – Preparation
- [ ] Install the .NET 10 SDK.
- [ ] Install Ollama for Windows.
- [ ] Pull a small model, e.g. `qwen2.5:7b` (or `qwen2.5:3b` for more speed).
- [ ] Test by hand: give the model the text of one invoice and check that company and invoice number are correct.

## Phase 2 – Project and UI
- [ ] Create a WPF project targeting .NET 10.
- [ ] Add the NuGet package **PdfPig**.
- [ ] Build the main window:
  - folder picker (`OpenFolderDialog`)
  - "Analyze" button
  - "Rename" button
  - status line
  - editable table with the proposals
- [ ] Table columns: old file name, PDF type, company, invoice number, amount, category, booking date, new file name.
- [ ] Move categories and model name into a settings file (e.g. `appsettings.json`) so they can change without a rebuild.

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
- [ ] Mark uncertain rows, e.g. "no booking found" or "scan".
- [ ] Let the user edit every value in the table.
- [ ] Rename only after the user clicks "Rename".
- [ ] Never overwrite an existing file.
- [ ] Replace characters that are not allowed in Windows file names.
- [ ] Write a log file (`old name → new name`) so every rename can be undone.

## Phase 7 – Testing
- [ ] Test with **copies** of 5–10 real invoices.
- [ ] Add one scanned PDF on purpose to test the scan detection.
- [ ] Compare 3B and 7B models; use 3B if it is accurate enough (faster).
- [ ] Add unit tests for: date parsing, file name building, scan detection, booking matching.
