# 03 – Scan Detection

FileFox must decide on its own whether a PDF is a **real PDF** or a **scan**.

- **Real PDF:** created digitally, e.g. an invoice downloaded from Amazon. It contains real, selectable text.
- **Scan:** a photo or scanned image of a paper page. It contains only an image, no text.

PdfPig can only read real text. A scan without a text layer would give the language model empty input and lead to wrong results.

## Checks per page
1. **Text amount:** How many characters can be extracted?
2. **Images:** Does the page contain a large image (e.g. covering most of the page)?
3. **Text quality:** Is the text meaningful, or mostly special characters / garbage (e.g. broken font encoding)?

## Classification

| Result | Rule | Action |
|--------|------|--------|
| **Real PDF** | Enough readable text (e.g. > 20 characters per page) | Process normally |
| **Scan with text layer** | Large page image **and** readable text (some scanners add invisible OCR text) | Process, but mark the row for review |
| **Scan** | Little or no text and a large page image | Mark as "Scan – not readable" and skip; do not send to the model |

The threshold (20 characters) must be configurable.

## Bank statement
The bank statement is checked the same way. If it is a scan without text, FileFox stops and shows a clear error message.

## Out of scope (for now)
- OCR for scans (e.g. with Tesseract). Can be added later; see [04-backlog.md](04-backlog.md).
