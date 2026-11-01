using System.Text;
using FileFox.Cli.Composition;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

// Umlauts and the € sign in invoice texts need UTF-8 on the Windows console.
Console.OutputEncoding = Encoding.UTF8;

var console = AnsiConsole.Console;
var configuration = CliComposition.BuildConfiguration(AppContext.BaseDirectory);

var services = new ServiceCollection().AddFileFoxCli(configuration, console);

return await CliComposition.CreateApp(services, console).RunAsync(args);
