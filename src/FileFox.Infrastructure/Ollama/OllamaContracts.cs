using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace FileFox.Infrastructure.Ollama;

// Minimal request/response shapes of the Ollama REST API (https://github.com/ollama/ollama/blob/main/docs/api.md).

internal sealed record OllamaChatRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("messages")] IReadOnlyList<OllamaMessage> Messages,
    [property: JsonPropertyName("format")] JsonNode? Format,
    [property: JsonPropertyName("options")] OllamaRequestOptions Options,
    [property: JsonPropertyName("stream")] bool Stream = false);

internal sealed record OllamaMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content);

internal sealed record OllamaRequestOptions(
    [property: JsonPropertyName("temperature")] double Temperature,
    [property: JsonPropertyName("num_ctx")] int ContextLength);

internal sealed record OllamaChatResponse(
    [property: JsonPropertyName("message")] OllamaMessage? Message,
    [property: JsonPropertyName("done")] bool Done);

internal sealed record OllamaTagsResponse(
    [property: JsonPropertyName("models")] IReadOnlyList<OllamaModelInfo>? Models);

internal sealed record OllamaModelInfo(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("model")] string? Model);
