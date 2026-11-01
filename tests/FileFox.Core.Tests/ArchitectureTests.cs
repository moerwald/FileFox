using FileFox.Core.Abstractions;

namespace FileFox.Core.Tests;

public class ArchitectureTests
{
    [Theory]
    [Trait("Spec", "F1-80")]
    [InlineData("Spectre.Console")]
    [InlineData("UglyToad.PdfPig")]
    [InlineData("System.Net.Http")]
    [InlineData("Microsoft.Extensions.Http")]
    [InlineData("FileFox.Infrastructure")]
    [InlineData("FileFox.Cli")]
    public void Core_does_not_depend_on_ui_pdf_or_http_libraries(string forbiddenPrefix)
    {
        var references = typeof(IInvoiceAnalysisService).Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(references, r => r.Name!.StartsWith(forbiddenPrefix, StringComparison.Ordinal));
    }
}
