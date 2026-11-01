# 04 – Backlog

## Open questions
- [ ] Which bank issues the statement? What does a booking line look like? (An anonymized sample line is needed for reliable matching.)
- [x] Initial categories: IT-Hardware, Software, Household, Office, Other (configurable, each with a description for the model).
- [x] Invoices with items from several categories: the category with the highest amount wins, and the invoice is marked for review.
- [x] Model: `qwen2.5:7b` (configurable).
- [ ] How is the bank statement named, so FileFox can find it reliably?
- [ ] Which model gives the best balance of speed and accuracy on the target machine?

## Features

| # | Feature | Status |
|---|---------|--------|
| 1 | `filefox analyze <folder>`: show invoice data, line items and suggested categories | Implemented (branch `feature/invoice-analysis`) |
| 2 | Match invoices to bank statement bookings | Planned |
| 3 | Rename invoices (`rename`, `undo`) | Planned |
| 4 | WPF GUI | Planned |

## Future feature ideas
- [ ] OCR for scanned PDFs (e.g. Tesseract, offline).
- [ ] *(add new features here)*
