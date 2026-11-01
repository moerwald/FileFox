# 01 – Overview

## Goal
FileFox is a Windows desktop app that brings order to invoice PDFs.

The user has one folder that contains:
- one **bank statement** PDF
- several **invoice** PDFs

FileFox reads the bank statement, finds the matching invoice for each booking and renames the invoice file.

## Naming scheme

```
MM-DD_InvoiceNumber_Company-Category.pdf
```

| Part | Source | Example |
|------|--------|---------|
| `MM-DD` | Date the amount was debited from the account (bank statement) | `11-10` |
| `InvoiceNumber` | Invoice number printed on the invoice | `RE12345` |
| `Company` | Company that issued the invoice | `Amazon` |
| `Category` | Category from a fixed list | `IT` |

Example: Amazon debited money on 10 November 2026 → `11-10_RE12345_Amazon-IT.pdf`

Example categories: `IT`, `Home`, `Car`, `Groceries`, `Other`. The list must be configurable.

## Constraints
- Runs **fully offline** – no cloud services.
- Platform: **Windows**.
- Technology: **C#, WPF, .NET 10**.
- Hardware: **no GPU / no VRAM**, max. **32 GB RAM**.
- Invoices are normally real (digitally created) PDFs, not scans.

## Key decisions

| Topic | Decision | Reason |
|-------|----------|--------|
| UI | WPF | Native Windows desktop app; review table before renaming |
| PDF text extraction | PdfPig (NuGet) | Free, managed, works offline |
| Intelligence | Small language model via **Ollama** on `localhost:11434` | Handles invoices with different layouts; runs offline on CPU |
| Model | Qwen 2.5 7B (Q4), fall back to 3B if too slow | Good quality, ~3–6 GB RAM |
| Category assignment | Model picks from the fixed list; user can correct in the table | Automatic, but user stays in control |
| Renaming | Only after the user clicks "Rename" | No surprises; mistakes can be fixed first |
| Scans | Detected automatically and skipped | No OCR needed for now (see [03-scan-detection.md](03-scan-detection.md)) |

## Hardware assessment
32 GB RAM without a GPU is **enough**.

- A 3B–8B model in Q4 quantization needs about **3–6 GB RAM** and runs on the CPU.
- Expected speed: about **5–30 seconds per invoice**, depending on CPU and model size.
- A folder with ~20 invoices is fine. Large batches will take a few minutes.
