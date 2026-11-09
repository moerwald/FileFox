using FileFox.Core.Abstractions;
using FileFox.Core.Models;
using FileFox.Core.Options;
using Microsoft.Extensions.Options;

namespace FileFox.Infrastructure.Ollama;

/// <summary>Extracts invoice data and line item categories with a local Ollama model.</summary>
public sealed class OllamaInvoiceAnalyzer(
    OllamaClient client,
    IOptions<OllamaOptions> ollamaOptions,
    IOptions<FileFoxOptions> fileFoxOptions) : IInvoiceAnalyzer
{
    private readonly OllamaOptions _ollama = ollamaOptions.Value;
    private readonly FileFoxOptions _fileFox = fileFoxOptions.Value;

    public async Task<InvoiceData> AnalyzeAsync(string invoiceText, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(invoiceText))
        {
            throw new ArgumentException("The invoice text is empty.", nameof(invoiceText));
        }

        OllamaMessage[] messages =
        [
            new("system", InvoicePromptBuilder.BuildSystemPrompt(_fileFox)),
            new("user", InvoicePromptBuilder.BuildUserPrompt(invoiceText, _ollama.MaxInvoiceCharacters)),
        ];

        var answer = await client.ChatAsync(
            _ollama.Model,
            messages,
            InvoicePromptBuilder.BuildSchema(_fileFox),
            new OllamaRequestOptions(_ollama.Temperature, _ollama.ContextLength),
            cancellationToken).ConfigureAwait(false);

        return InvoiceResponseParser.Parse(answer);
    }
}
