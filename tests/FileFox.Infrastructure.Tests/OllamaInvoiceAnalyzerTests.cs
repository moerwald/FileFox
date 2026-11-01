using System.Text.Json;
using FileFox.Core.Options;
using FileFox.Infrastructure.Ollama;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace FileFox.Infrastructure.Tests;

public class OllamaInvoiceAnalyzerTests
{
    private static string ChatResponse(string content) =>
        JsonSerializer.Serialize(new { model = "qwen2.5:7b", message = new { role = "assistant", content }, done = true });

    private static OllamaInvoiceAnalyzer CreateAnalyzer(FakeHttpHandler handler) => new(
        new OllamaClient(handler.CreateClient()),
        MsOptions.Create(new OllamaOptions { Model = "qwen2.5:7b", ContextLength = 4096, Temperature = 0 }),
        MsOptions.Create(new FileFoxOptions
        {
            Categories = [new() { Name = "IT-Hardware", Description = "USB sticks" }],
            FallbackCategory = "Other",
        }));

    [Fact]
    public async Task Sends_a_structured_chat_request_and_parses_the_answer()
    {
        var handler = FakeHttpHandler.Json(ChatResponse("""
            { "company": "Amazon", "invoiceNumber": "DE5ABC123", "invoiceDate": "2026-11-08", "currency": "EUR",
              "totalAmount": 12.99,
              "lineItems": [ { "description": "USB stick", "quantity": 1, "totalPrice": 12.99, "category": "IT-Hardware" } ] }
            """));

        var invoice = await CreateAnalyzer(handler).AnalyzeAsync("Rechnung Amazon USB-Stick 12,99 EUR");

        Assert.Equal("Amazon", invoice.Company);
        Assert.Equal("IT-Hardware", invoice.LineItems[0].Category);

        var (request, body) = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("http://localhost:11434/api/chat", request.RequestUri!.ToString());

        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;
        Assert.Equal("qwen2.5:7b", root.GetProperty("model").GetString());
        Assert.False(root.GetProperty("stream").GetBoolean());
        Assert.Equal(4096, root.GetProperty("options").GetProperty("num_ctx").GetInt32());
        Assert.Equal(0, root.GetProperty("options").GetProperty("temperature").GetDouble());
        Assert.Equal("object", root.GetProperty("format").GetProperty("type").GetString());

        var messages = root.GetProperty("messages");
        Assert.Equal("system", messages[0].GetProperty("role").GetString());
        Assert.Contains("IT-Hardware", messages[0].GetProperty("content").GetString());
        Assert.Contains("USB-Stick 12,99 EUR", messages[1].GetProperty("content").GetString());
    }

    [Fact]
    public async Task Http_error_is_reported_with_details()
    {
        var handler = FakeHttpHandler.Json("""{ "error": "model 'qwen2.5:7b' not found" }""", System.Net.HttpStatusCode.NotFound);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => CreateAnalyzer(handler).AnalyzeAsync("some text"));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task Empty_text_is_rejected_without_calling_the_model()
    {
        var handler = FakeHttpHandler.Json("{}");

        await Assert.ThrowsAsync<ArgumentException>(() => CreateAnalyzer(handler).AnalyzeAsync("  "));
        Assert.Empty(handler.Requests);
    }
}
