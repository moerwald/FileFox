using FileFox.Core.Abstractions;
using FileFox.Core.Options;
using FileFox.Infrastructure.FileSystem;
using FileFox.Infrastructure.Ollama;
using FileFox.Infrastructure.Pdf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FileFox.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Binds the options from <paramref name="configuration"/> and registers the infrastructure
    /// implementations: file system, PdfPig and Ollama.
    /// </summary>
    public static IServiceCollection AddFileFoxInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FileFoxOptions>(configuration.GetSection(FileFoxOptions.SectionName));
        services.Configure<OllamaOptions>(configuration.GetSection(OllamaOptions.SectionName));

        services.AddSingleton<IFolderReader, FileSystemFolderReader>();
        services.AddSingleton<IPdfTextExtractor, PdfPigTextExtractor>();

        services.AddHttpClient<OllamaClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<OllamaOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(Math.Max(10, options.RequestTimeoutSeconds));
        });

        services.AddTransient<IInvoiceAnalyzer, OllamaInvoiceAnalyzer>();
        services.AddTransient<ILanguageModelStatusProvider, OllamaModelStatusProvider>();

        return services;
    }
}
