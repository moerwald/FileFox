using FileFox.Cli.Commands;
using FileFox.Cli.Composition;
using FileFox.Cli.Rendering;
using FileFox.Core.Abstractions;
using FileFox.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Testing;

namespace FileFox.Cli.Tests;

public class ArchitectureTests
{
    [Fact]
    [Trait("Spec", "F1-81")]
    public void Analyze_command_only_uses_the_core_service_for_business_functions()
    {
        var allowed = new[] { typeof(IInvoiceAnalysisService), typeof(InvoiceRenderer), typeof(IAnsiConsole) };

        var parameters = typeof(AnalyzeCommand).GetConstructors().Single().GetParameters().Select(p => p.ParameterType);

        Assert.All(parameters, type => Assert.Contains(type, allowed));
    }

    [Fact]
    [Trait("Spec", "F1-81")]
    public void Renderer_only_depends_on_the_console()
    {
        var parameters = typeof(InvoiceRenderer).GetConstructors().Single().GetParameters().Select(p => p.ParameterType);

        Assert.Equal(new[] { typeof(IAnsiConsole) }, parameters);
    }

    [Fact]
    [Trait("Spec", "F1-82")]
    public void All_services_can_be_created_from_the_container()
    {
        var configuration = CliComposition.BuildConfiguration(RepoPaths.CliProjectDirectory);
        var services = new ServiceCollection().AddFileFoxCli(configuration, new TestConsole());

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        Assert.NotNull(provider.GetRequiredService<IInvoiceAnalysisService>());
        Assert.NotNull(provider.GetRequiredService<InvoiceRenderer>());
    }

    [Theory]
    [Trait("Spec", "F1-60")]
    [InlineData(AnalysisStatus.Success, "green")]
    [InlineData(AnalysisStatus.NeedsReview, "yellow")]
    [InlineData(AnalysisStatus.Skipped, "red")]
    [InlineData(AnalysisStatus.Failed, "red")]
    public void Result_colors(AnalysisStatus status, string color)
    {
        Assert.Equal(color, InvoiceRenderer.StatusColor(status));
    }
}
