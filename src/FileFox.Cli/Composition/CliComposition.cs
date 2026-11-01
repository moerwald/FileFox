using FileFox.Cli.Commands;
using FileFox.Cli.Rendering;
using FileFox.Core;
using FileFox.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;

namespace FileFox.Cli.Composition;

/// <summary>Composition root of the CLI. Shared by <c>Program</c> and the tests.</summary>
internal static class CliComposition
{
    public const string EnvironmentPrefix = "FILEFOX_";

    public static IConfiguration BuildConfiguration(string basePath) =>
        new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddEnvironmentVariables(EnvironmentPrefix)
            .Build();

    public static IServiceCollection AddFileFoxCli(this IServiceCollection services, IConfiguration configuration, IAnsiConsole console)
    {
        services.AddSingleton(configuration);
        services.AddSingleton(console);
        services.AddFileFoxCore();
        services.AddFileFoxInfrastructure(configuration);
        services.AddSingleton<InvoiceRenderer>();
        return services;
    }

    public static CommandApp CreateApp(IServiceCollection services, IAnsiConsole console)
    {
        var app = new CommandApp(new TypeRegistrar(services));
        app.Configure(config =>
        {
            config.SetApplicationName("filefox");
            config.ConfigureConsole(console);

            config.AddCommand<AnalyzeCommand>("analyze")
                .WithDescription("Reads all invoice PDFs in a folder and shows the invoice data, the line items and the category suggested by the language model. Nothing is changed.")
                .WithExample("analyze", @"C:\Invoices\2026-11");
        });
        return app;
    }
}
