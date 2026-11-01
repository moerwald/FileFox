# FileFox – Project Instructions

## Language
- English is the default language for everything written to this repository.
- This includes all source code, identifiers, comments, commit messages, plans, documentation and the README.
- Chat replies to the user may stay in German.

## Project
FileFox is an offline Windows WPF app (.NET 10) that matches invoice PDFs to entries in a bank statement PDF and renames them to:
`MM-DD_InvoiceNumber_Company-Category.pdf`

- PDF text extraction: PdfPig
- Local language model: Ollama (CPU only, no GPU, max. 32 GB RAM)
- The app detects scanned PDFs without a text layer and skips them.
