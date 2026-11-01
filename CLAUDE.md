# FileFox – Project Instructions

## Language
- English is the default language for everything written to this repository.
- This includes all source code, identifiers, comments, commit messages, plans, documentation and the README.
- Chat replies to the user may stay in German.

## Architecture rules
- Keep the presentation layer thin. No business logic in the CLI or the (later) WPF (Windows Presentation Foundation) UI.
- All business logic lives in a separate core library that knows nothing about any UI.
- Work through interfaces (e.g. `IPdfTextExtractor`, `IInvoiceAnalyzer`, `IBookingMatcher`, `IFileRenamer`).
- Use dependency injection (`Microsoft.Extensions.DependencyInjection`) to wire everything together.
- Version 1 is a command-line app with colored output. The WPF GUI comes later and must reuse the same core library.
- See `docs/plan/05-architecture.md`.

## Specifications and tests
- Every feature has a specification in `docs/specs/` with numbered requirements (e.g. `F1-20`), written before or together with the code.
- Tests are derived from the specification, not from the implementation. Each test names the requirement(s) it verifies with `[Trait("Spec", "F1-20")]`.
- If code and specification disagree, the specification wins – or the specification is changed first, with the user's agreement.
- Requirements that cannot be tested automatically are marked *(manual)* in the specification.

## Git workflow
- Implement every feature on its own branch (e.g. `feature/<short-name>`), never directly on `main`.
- When the feature is done, push the branch and open a pull request against `main` for the user to review.
- Do not merge pull requests yourself.

## Project
FileFox is an offline Windows app (.NET 10, CLI first, WPF later) that matches invoice PDFs to entries in a bank statement PDF and renames them to:
`MM-DD_InvoiceNumber_Company-Category.pdf`

- PDF text extraction: PdfPig
- Local language model: Ollama (CPU only, no GPU, max. 32 GB RAM)
- The app detects scanned PDFs without a text layer and skips them.
