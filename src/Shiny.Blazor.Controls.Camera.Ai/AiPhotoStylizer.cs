using Microsoft.Extensions.AI;
using Shiny.Controls.Camera;

namespace Shiny.Blazor.Controls.Camera.Ai;

/// <summary>
/// Redraws a captured photo through an image-generation model — the "turn my selfie into a comic" flow. The
/// mirror of the MAUI <c>AiPhotoStylizer</c>, and an <see cref="ICaptureEffect"/> for the same reason: a model
/// round-trip is seconds of latency and a per-image cost, so it runs on the shutter, never on a frame.
/// </summary>
/// <remarks>
/// <para>
/// Blazor's <c>CameraView</c> applies CSS effects itself but does not run capture effects automatically — the
/// browser component has no async post-capture stage — so call <see cref="StylizeAsync"/> with the bytes from
/// <c>CapturePhotoAsync</c>:
/// </para>
/// <code>
/// var jpeg = await camera.CapturePhotoAsync();
/// var comic = await stylizer.StylizeAsync(jpeg);
/// </code>
/// <para>
/// For a live comic <i>viewfinder</i> alongside it, add <see cref="CameraEffects.Comic"/> to the camera's
/// <c>Effects</c> — procedural, offline and free.
/// </para>
/// <para>
/// <b>MEAI001:</b> <c>IImageGenerator</c> is still marked evaluation-only by Microsoft.Extensions.AI, so
/// constructing one in your own project raises <c>MEAI001</c>. Add
/// <c>&lt;NoWarn&gt;$(NoWarn);MEAI001&lt;/NoWarn&gt;</c> to suppress it until that API ships stable.
/// </para>
/// </remarks>
public class AiPhotoStylizer : ICaptureEffect
{
    readonly IImageGenerator generator;

    /// <param name="generator">The MEAI image generator the photo is sent to. Must support image-to-image editing.</param>
    public AiPhotoStylizer(IImageGenerator generator)
        => this.generator = generator ?? throw new ArgumentNullException(nameof(generator));

    /// <inheritdoc/>
    public string Id { get; set; } = "shiny.camera.ai.stylize";

    /// <inheritdoc/>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// The instruction describing the style to redraw the photo in. The default asks for inked comic-book art.
    /// </summary>
    public string Prompt { get; set; } =
        "Redraw this photo as a comic-book illustration: bold black ink outlines, flat cel shading, " +
        "halftone-style shadows and punchy saturated colour. Keep the subject, composition and pose exactly " +
        "as they are — restyle the image, do not reimagine the scene.";

    /// <summary>Optional MEAI <see cref="ImageGenerationOptions"/> (model id, size, response format, …).</summary>
    public ImageGenerationOptions? Options { get; set; }

    /// <summary>How long to wait before giving up and returning the original photo. Default 60 seconds.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Raised when the model call fails or times out. The original photo is returned, so this means "the style
    /// didn't apply", not "the photo was lost".
    /// </summary>
    public event EventHandler<Exception>? Error;

    /// <summary>
    /// Redraw <paramref name="jpeg"/> in the configured style, or return it unchanged if the model fails.
    /// </summary>
    public Task<byte[]> StylizeAsync(byte[] jpeg, CancellationToken ct = default)
        => this.ApplyAsync(jpeg, ct).AsTask();

    /// <inheritdoc/>
    public async ValueTask<byte[]> ApplyAsync(byte[] jpeg, CancellationToken ct)
    {
        if (jpeg is not { Length: > 0 })
            return jpeg;

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        if (this.Timeout > TimeSpan.Zero)
            timeout.CancelAfter(this.Timeout);

        try
        {
            var response = await this.generator
                .EditImageAsync(new DataContent(jpeg, "image/jpeg"), this.Prompt, this.Options, timeout.Token)
                .ConfigureAwait(false);

            foreach (var content in response.Contents)
            {
                if (content is DataContent data && data.Data.Length > 0)
                    return data.Data.ToArray();
            }

            this.Error?.Invoke(this, new InvalidOperationException(
                "The image generator returned no inline image data. Providers that reply with a URL instead of " +
                "bytes need ImageGenerationOptions.ResponseFormat set to request the data directly."));
            return jpeg;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw; // the caller cancelled — let that propagate
        }
        catch (OperationCanceledException)
        {
            this.Error?.Invoke(this, new TimeoutException($"Image stylization timed out after {this.Timeout}."));
            return jpeg;
        }
        catch (Exception ex)
        {
            this.Error?.Invoke(this, ex);
            return jpeg;
        }
    }
}
