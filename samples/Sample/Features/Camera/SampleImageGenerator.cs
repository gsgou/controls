using Microsoft.Extensions.AI;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;

namespace Sample.Features.Camera;

/// <summary>
/// A stand-in <see cref="IImageGenerator"/> so the "AI stylize" demo runs offline, without an API key or a
/// per-image bill. It does <b>not</b> generate anything — it redraws the captured photo with an obvious
/// treatment (a warm tint, an ink border and a label) purely to prove the capture-effect round trip: shutter →
/// bytes handed to the generator → new bytes come back → that's what <c>CapturePhotoAsync</c> returns.
/// </summary>
/// <remarks>
/// In a real app you register a genuine image-to-image generator instead — Azure OpenAI, OpenAI, or anything
/// else implementing <see cref="IImageGenerator"/> — and <c>AiPhotoStylizer</c> is unchanged.
/// </remarks>
public sealed class SampleImageGenerator : IImageGenerator
{
    public Task<ImageGenerationResponse> GenerateAsync(
        ImageGenerationRequest request,
        ImageGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var source = request.OriginalImages?
            .OfType<DataContent>()
            .FirstOrDefault(d => d.Data.Length > 0);

        if (source is null)
            return Task.FromResult(new ImageGenerationResponse());

        var styled = Restyle(source.Data.ToArray());
        return Task.FromResult(new ImageGenerationResponse([new DataContent(styled, "image/jpeg")]));
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose() { }

    static byte[] Restyle(byte[] jpeg)
    {
        try
        {
            using var input = new MemoryStream(jpeg, false);
            var image = PlatformImage.FromStream(input);
            if (image is null)
                return jpeg;

            var width = (int)image.Width;
            var height = (int)image.Height;

            using var context = new PlatformBitmapExportService().CreateContext(width, height);
            var canvas = context.Canvas;
            canvas.DrawImage(image, 0, 0, width, height);

            // a warm wash + heavy ink border reads instantly as "something processed this"
            canvas.FillColor = Color.FromRgba(255, 170, 60, 60);
            canvas.FillRectangle(0, 0, width, height);

            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = Math.Max(6f, width * 0.012f);
            canvas.DrawRectangle(0, 0, width, height);

            var bannerHeight = Math.Max(28f, height * 0.06f);
            canvas.FillColor = Color.FromRgba(0, 0, 0, 180);
            canvas.FillRectangle(0, height - bannerHeight, width, bannerHeight);

            canvas.FontColor = Colors.White;
            canvas.FontSize = bannerHeight * 0.5f;
            canvas.DrawString(
                "SAMPLE STYLIZER — no model was called",
                0, height - bannerHeight, width, bannerHeight,
                HorizontalAlignment.Center, VerticalAlignment.Center);

            using var output = new MemoryStream();
            context.Image.Save(output, ImageFormat.Jpeg, 0.9f);
            var bytes = output.ToArray();
            return bytes.Length == 0 ? jpeg : bytes;
        }
        catch (Exception)
        {
            return jpeg; // the stylizer treats an unchanged result as "no style applied", which is correct here
        }
    }
}
