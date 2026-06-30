using System.Text.Json;
using Microsoft.Extensions.AI;
using Shiny.Blazor.Controls.Camera;

namespace Shiny.Blazor.Controls.Camera.Ai;

/// <summary>
/// Drives the Blazor <see cref="CameraView"/>'s document presence detection into a Microsoft.Extensions.AI
/// <see cref="IChatClient"/>: it waits (cheaply, in-browser) for a document to be steadily present, grabs that
/// one cropped frame, and sends it to the model to extract a strongly-typed <typeparamref name="TDocument"/>.
/// The model only runs once a real document is in view — not on every preview frame — which is the speed/cost
/// win. The mirror of the MAUI <c>AiDocumentAnalyzer&lt;TDocument&gt;</c>.
/// </summary>
/// <remarks>
/// Assign a <see cref="DocumentAnalyzer"/> to the camera's <c>Analyzer</c> first, then call
/// <see cref="ScanAsync"/> (e.g. from a "Scan" button, or in a loop to keep scanning). Parsing uses MEAI
/// structured output, so supply <see cref="SerializerOptions"/> built from a
/// <see cref="System.Text.Json.Serialization.JsonSerializerContext"/> for trim/AOT-safe WASM (the built-in
/// <see cref="AiDocumentScanner"/> does this for you).
/// </remarks>
/// <typeparam name="TDocument">The strongly-typed payload the model fills in (e.g. <c>Invoice</c>, or the built-in <c>AiDocument</c>).</typeparam>
public class AiDocumentScanner<TDocument>
{
    readonly IChatClient chatClient;

    /// <param name="chatClient">The MEAI chat client the frame is sent to. Must support image input (a vision model).</param>
    public AiDocumentScanner(IChatClient chatClient)
        => this.chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));

    /// <summary>The instruction sent with the image; the JSON shape is supplied automatically by structured output.</summary>
    public string Prompt { get; set; } =
        "You are a document scanner. Extract the data from this document image accurately and completely. " +
        "Transcribe values exactly as printed; do not invent fields that aren't present.";

    /// <summary>Optional MEAI <see cref="ChatOptions"/> (model id, temperature, …) passed to the chat client.</summary>
    public ChatOptions? Options { get; set; }

    /// <summary>
    /// <see cref="JsonSerializerOptions"/> used to build the structured-output schema and deserialize the result.
    /// Supply context-backed options for trim/AOT-safe WASM; when null the MEAI reflection defaults are used.
    /// </summary>
    public JsonSerializerOptions? SerializerOptions { get; set; }

    /// <summary>
    /// Wait for the next steadily-present document on <paramref name="camera"/>, send its cropped image to the
    /// model, and return the parsed document (or <c>null</c> if the model couldn't produce one).
    /// </summary>
    /// <param name="camera">The started camera whose <c>Analyzer</c> is a <see cref="DocumentAnalyzer"/>.</param>
    /// <param name="ct">Cancels the wait for a document and/or the model call.</param>
    public async Task<TDocument?> ScanAsync(CameraView camera, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(camera);

        var image = await camera.RequestDocumentImageAsync(ct).ConfigureAwait(false);
        return await this.ExtractAsync(image.Jpeg, ct).ConfigureAwait(false);
    }

    /// <summary>Send an already-captured document JPEG to the model and parse it (e.g. from <c>CapturePhotoAsync</c>).</summary>
    public async Task<TDocument?> ExtractAsync(byte[] jpeg, CancellationToken ct = default)
    {
        var message = new ChatMessage(ChatRole.User,
        [
            new TextContent(this.Prompt),
            new DataContent(jpeg, "image/jpeg")
        ]);

        var response = this.SerializerOptions is { } opts
            ? await this.chatClient.GetResponseAsync<TDocument>([message], opts, this.Options, cancellationToken: ct).ConfigureAwait(false)
            : await this.chatClient.GetResponseAsync<TDocument>([message], this.Options, cancellationToken: ct).ConfigureAwait(false);

        return response.TryGetResult(out var doc) ? doc : default;
    }
}
