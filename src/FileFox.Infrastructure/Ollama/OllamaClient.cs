using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace FileFox.Infrastructure.Ollama;

/// <summary>Thin HTTP client for the Ollama REST API.</summary>
public sealed class OllamaClient(HttpClient httpClient)
{
    /// <summary>Sends one chat request and returns the text of the model's answer.</summary>
    internal async Task<string> ChatAsync(
        string model,
        IReadOnlyList<OllamaMessage> messages,
        JsonNode? format,
        OllamaRequestOptions options,
        CancellationToken cancellationToken)
    {
        var request = new OllamaChatRequest(model, messages, format, options);

        using var response = await httpClient.PostAsJsonAsync("api/chat", request, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        var body = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken).ConfigureAwait(false);
        return body?.Message?.Content
            ?? throw new InvalidOperationException("Ollama returned an empty answer.");
    }

    /// <summary>Returns the names of all locally installed models.</summary>
    internal async Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync("api/tags", cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        var body = await response.Content.ReadFromJsonAsync<OllamaTagsResponse>(cancellationToken).ConfigureAwait(false);
        return body?.Models?
            .Select(m => m.Name ?? m.Model)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .ToList() ?? [];
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var details = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        throw new HttpRequestException(
            $"Ollama answered with {(int)response.StatusCode} {response.ReasonPhrase}: {details}",
            inner: null,
            statusCode: response.StatusCode);
    }
}
