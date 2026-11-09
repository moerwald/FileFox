namespace FileFox.Infrastructure.Ollama;

/// <summary>Settings for the local Ollama server, bound from the "Ollama" section of appsettings.json.</summary>
public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";

    public string BaseUrl { get; set; } = "http://localhost:11434";

    /// <summary>Model name as shown by <c>ollama list</c>, e.g. "qwen2.5:7b".</summary>
    public string Model { get; set; } = "qwen2.5:7b";

    /// <summary>0 = deterministic answers. Keep it low for data extraction.</summary>
    public double Temperature { get; set; } = 0;

    /// <summary>Context window in tokens (Ollama option <c>num_ctx</c>). Too small and long invoices get cut off.</summary>
    public int ContextLength { get; set; } = 8192;

    /// <summary>Maximum time for one request. CPU-only machines can be slow.</summary>
    public int RequestTimeoutSeconds { get; set; } = 300;

    /// <summary>Invoice text longer than this is truncated before it is sent to the model.</summary>
    public int MaxInvoiceCharacters { get; set; } = 12000;
}
