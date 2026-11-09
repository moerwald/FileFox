using System.Net.Sockets;
using FileFox.Infrastructure.Ollama;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace FileFox.Infrastructure.Tests;

public class OllamaModelStatusProviderTests
{
    private static OllamaModelStatusProvider CreateProvider(FakeHttpHandler handler, string model = "qwen2.5:7b") =>
        new(new OllamaClient(handler.CreateClient()), MsOptions.Create(new OllamaOptions { Model = model }));

    [Fact]
    [Trait("Spec", "F1-10")]
    public async Task Installed_model_is_available()
    {
        var handler = FakeHttpHandler.Json("""{ "models": [ { "name": "llama3.2:3b" }, { "name": "qwen2.5:7b" } ] }""");

        var status = await CreateProvider(handler).CheckAsync();

        Assert.True(status.IsAvailable);
        Assert.EndsWith("api/tags", handler.Requests[0].Request.RequestUri!.ToString());
    }

    [Fact]
    [Trait("Spec", "F1-12")]
    public async Task Missing_model_explains_how_to_install_it()
    {
        var handler = FakeHttpHandler.Json("""{ "models": [ { "name": "llama3.2:3b" } ] }""");

        var status = await CreateProvider(handler).CheckAsync();

        Assert.False(status.IsAvailable);
        Assert.Contains("ollama pull qwen2.5:7b", status.Message);
        Assert.Contains("llama3.2:3b", status.Message);
    }

    [Fact]
    [Trait("Spec", "F1-11")]
    public async Task Unreachable_server_is_reported()
    {
        var handler = FakeHttpHandler.Throws(new HttpRequestException("Connection refused", new SocketException()));

        var status = await CreateProvider(handler).CheckAsync();

        Assert.False(status.IsAvailable);
        Assert.Contains("not reachable", status.Message);
    }

    [Theory]
    [Trait("Spec", "F1-13")]
    [InlineData("qwen2.5:latest", "qwen2.5", true)]
    [InlineData("qwen2.5:7b", "QWEN2.5:7B", true)]
    [InlineData("qwen2.5:3b", "qwen2.5:7b", false)]
    [InlineData("qwen2.5:7b", "qwen2.5", false)]
    public void Model_names_are_compared_like_ollama_does(string installed, string configured, bool expected)
    {
        Assert.Equal(expected, OllamaModelStatusProvider.IsSameModel(installed, configured));
    }
}
