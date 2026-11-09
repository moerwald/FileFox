using System.Net.Sockets;
using FileFox.Core.Abstractions;
using FileFox.Core.Models;
using Microsoft.Extensions.Options;

namespace FileFox.Infrastructure.Ollama;

/// <summary>Checks that Ollama is running and that the configured model is installed.</summary>
public sealed class OllamaModelStatusProvider(OllamaClient client, IOptions<OllamaOptions> options) : ILanguageModelStatusProvider
{
    private readonly OllamaOptions _options = options.Value;

    public async Task<ModelStatus> CheckAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> installed;
        try
        {
            installed = await client.ListModelsAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex) when (ex.InnerException is SocketException || ex.StatusCode is null)
        {
            return new ModelStatus(false, _options.Model,
                $"Ollama is not reachable at {_options.BaseUrl}. Start Ollama and try again.");
        }
        catch (HttpRequestException ex)
        {
            return new ModelStatus(false, _options.Model, $"Ollama returned an error: {ex.Message}");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ModelStatus(false, _options.Model, $"Ollama at {_options.BaseUrl} did not answer in time.");
        }

        if (installed.Any(name => IsSameModel(name, _options.Model)))
        {
            return new ModelStatus(true, _options.Model, $"Model {_options.Model} is ready.");
        }

        var available = installed.Count > 0 ? string.Join(", ", installed) : "none";
        return new ModelStatus(false, _options.Model,
            $"Model {_options.Model} is not installed. Run: ollama pull {_options.Model}  (installed: {available})");
    }

    /// <summary>"qwen2.5" and "qwen2.5:latest" are the same model in Ollama.</summary>
    internal static bool IsSameModel(string installed, string configured) =>
        string.Equals(WithTag(installed), WithTag(configured), StringComparison.OrdinalIgnoreCase);

    private static string WithTag(string name) => name.Contains(':') ? name.Trim() : name.Trim() + ":latest";
}
