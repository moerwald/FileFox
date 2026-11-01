using System.Text;
using FileFox.Cli.Commands;
using FileFox.Cli.Composition;
using FileFox.Cli.Rendering;
using FileFox.Core;
using FileFox.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;

// Umlauts and the € sign in invoice texts need UTF-8 on the Windows console.
Console.OutputEncoding = Encoding.UTF8;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddEnvironmentVariables(prefix: "FILEFOX_")
    .Build();

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);
services.AddSingleton<IAnsiConsole>(AnsiConsole.Console);
services.AddFileFoxCore();
services.AddFileFoxInfrastructure(configuration);
services.AddSingleton<InvoiceRenderer>();

var app = new CommandApp(new TypeRegistrar(services));
app.Configure(config =>
{
    config.SetApplicationName("filefox");

    config.AddCommand<AnalyzeCommand>("analyze")
        .WithDescription("Reads all invoice PDFs in a folder and shows the invoice data, the line items and the category suggested by the language model. Nothing is changed.")
        .WithExample("analyze", @"C:\Invoices\2026-11");
});

return await app.RunAsync(args);
