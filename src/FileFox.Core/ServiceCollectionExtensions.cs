using FileFox.Core.Abstractions;
using FileFox.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FileFox.Core;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the FileFox business logic. Options (<see cref="Options.FileFoxOptions"/>) and the
    /// infrastructure implementations (PDF, language model, file system) must be registered separately.
    /// </summary>
    public static IServiceCollection AddFileFoxCore(this IServiceCollection services)
    {
        services.AddSingleton<IPdfTypeDetector, PdfTypeDetector>();
        services.AddSingleton<IInvoiceFileLocator, InvoiceFileLocator>();
        services.AddSingleton<IInvoiceFolderResolver, InvoiceFolderResolver>();
        services.AddSingleton<IInvoiceCategoryResolver, InvoiceCategoryResolver>();
        services.AddTransient<IInvoiceAnalysisService, InvoiceAnalysisService>();
        return services;
    }
}
