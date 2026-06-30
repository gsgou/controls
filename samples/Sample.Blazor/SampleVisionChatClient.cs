using Microsoft.Extensions.AI;

namespace Sample.Blazor;

/// <summary>
/// A stand-in <see cref="IChatClient"/> so the camera "AI Document" demo runs offline in WASM, without an API
/// key. It ignores the image and returns a canned structured-output JSON payload to exercise the end-to-end
/// flow. In a real app, register a genuine vision client instead (Azure OpenAI / OpenAI / Ollama) — the
/// <c>AiDocumentScanner</c> code is identical.
/// </summary>
public sealed class SampleVisionChatClient : IChatClient
{
    const string CannedJson =
        """
        {
          "documentType": "Receipt",
          "summary": "Blue Bottle Coffee — 2 items, total $8.50",
          "fields": [
            { "label": "Merchant", "value": "Blue Bottle Coffee" },
            { "label": "Date", "value": "2026-06-30" },
            { "label": "Latte", "value": "$5.00" },
            { "label": "Croissant", "value": "$3.50" },
            { "label": "Total", "value": "$8.50" }
          ]
        }
        """;

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, CannedJson)));

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new ChatResponseUpdate(ChatRole.Assistant, CannedJson);
        await Task.CompletedTask;
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceType?.IsInstanceOfType(this) == true ? this : null;

    public void Dispose() { }
}
