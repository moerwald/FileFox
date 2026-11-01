using FileFox.Cli.Composition;
using FileFox.Core.Options;
using FileFox.Infrastructure.Ollama;
using Microsoft.Extensions.Configuration;

namespace FileFox.Cli.Tests;

/// <summary>Checks the shipped appsettings.json against the specification.</summary>
public class DefaultSettingsTests
{
    private static readonly IConfiguration Configuration = CliComposition.BuildConfiguration(RepoPaths.CliProjectDirectory);

    private static FileFoxOptions FileFox => Configuration.GetSection(FileFoxOptions.SectionName).Get<FileFoxOptions>()!;

    private static OllamaOptions Ollama => Configuration.GetSection(OllamaOptions.SectionName).Get<OllamaOptions>()!;

    [Fact]
    [Trait("Spec", "F1-42")]
    public void Usb_sticks_and_usb_power_adapters_belong_to_it_hardware()
    {
        var itHardware = Assert.Single(FileFox.Categories, c => c.Name == "IT-Hardware");

        Assert.Contains("USB stick", itHardware.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("USB power adapter", itHardware.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Spec", "F1-42")]
    public void Every_category_has_a_description()
    {
        Assert.NotEmpty(FileFox.Categories);
        Assert.All(FileFox.Categories, c => Assert.False(string.IsNullOrWhiteSpace(c.Description), $"{c.Name} has no description"));
    }

    [Fact]
    [Trait("Spec", "F1-40")]
    [Trait("Spec", "F1-41")]
    public void Fallback_category_is_one_of_the_categories()
    {
        Assert.Contains(FileFox.Categories, c => c.Name == FileFox.FallbackCategory);
    }

    [Fact]
    [Trait("Spec", "F1-04")]
    public void Default_statement_patterns()
    {
        Assert.Equal(new[] { "*Kontoauszug*", "*statement*" }, FileFox.StatementFileNamePatterns);
    }

    [Fact]
    [Trait("Spec", "F1-20")]
    [Trait("Spec", "F1-21")]
    public void Default_scan_detection_thresholds()
    {
        Assert.Equal(20, FileFox.ScanDetection.MinCharactersPerPage);
        Assert.Equal(0.5, FileFox.ScanDetection.MinTextQuality);
        Assert.Equal(0.5, FileFox.ScanDetection.LargeImageCoverage);
    }

    [Fact]
    [Trait("Spec", "F1-10")]
    [Trait("Spec", "F1-84")]
    public void Default_model_runs_locally()
    {
        Assert.Equal("qwen2.5:7b", Ollama.Model);
        Assert.Equal("localhost", new Uri(Ollama.BaseUrl).Host);
        Assert.Equal(0, Ollama.Temperature);
    }

    [Fact]
    [Trait("Spec", "F1-83")]
    public void Settings_can_be_overridden_with_environment_variables()
    {
        const string name = "FILEFOX_Ollama__ContextLength";
        Environment.SetEnvironmentVariable(name, "16384");
        try
        {
            var configuration = CliComposition.BuildConfiguration(RepoPaths.CliProjectDirectory);

            Assert.Equal("16384", configuration["Ollama:ContextLength"]);
        }
        finally
        {
            Environment.SetEnvironmentVariable(name, null);
        }
    }
}
