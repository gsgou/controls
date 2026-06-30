using Microsoft.Extensions.AI;

namespace Sample.Features.Camera;

/// <summary>
/// A stand-in <see cref="IChatClient"/> so the "AI Document" detector demo runs offline, without an API key.
/// It ignores the image and returns a canned structured-output JSON payload, just to exercise the end-to-end
/// flow (present a document → frame is shipped here → a typed result comes back). In a real app you'd register
/// a genuine vision client instead, e.g. an Azure OpenAI / OpenAI <c>IChatClient</c>, or Ollama — the
/// <c>AiDocumentAnalyzer</c> code is identical.
/// </summary>
public sealed class SampleVisionChatClient : IChatClient
{
    // valid JSON for the built-in AiDocument structured-output schema (camelCase; case-insensitive anyway)
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
    {
        // structured output reads ChatResponse.Text and deserializes it into the requested type
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, CannedJson));
        return Task.FromResult(response);
    }

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
