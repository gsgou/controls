using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Shiny.Controls.Camera;

namespace Shiny.Maui.Controls.Camera.Ocr;

/// <summary>
/// Recognizes text in each frame (native Vision / MLKit / Windows.Media.Ocr) and raises
/// <see cref="TextRecognized"/> with the recognized blocks. Draws a box around each block (captioned with
/// its text) unless <see cref="IncludeTextBlocks"/> is disabled. For structured extraction (invoices, IDs,
/// …) use the document analyzers in <c>Shiny.Maui.Controls.Camera.Documents</c>, which reuse the same
/// <see cref="TextRecognizer"/>.
/// </summary>
public class OcrAnalyzer : FrameAnalyzer
{
    readonly TextRecognizer recognizer = new();

    /// <inheritdoc/>
    public override string Id => "shiny.camera.ocr";

    /// <summary>Draw a box around each recognized text block. Default <c>true</c> (the event fires either way).</summary>
    public bool IncludeTextBlocks { get; set; } = true;

    /// <summary>Box outline + caption color. Default a violet accent.</summary>
    public Color BoxColor { get; set; } = Color.FromArgb("#A78BFA");

    /// <summary>Command invoked (with the <see cref="TextRecognizedEventArgs"/>) when text is recognized.</summary>
    public static readonly BindableProperty TextRecognizedCommandProperty = BindableProperty.Create(
        nameof(TextRecognizedCommand), typeof(ICommand), typeof(OcrAnalyzer));

    /// <inheritdoc cref="TextRecognizedCommandProperty"/>
    public ICommand? TextRecognizedCommand
    {
        get => (ICommand?)this.GetValue(TextRecognizedCommandProperty);
        set => this.SetValue(TextRecognizedCommandProperty, value);
    }

    /// <summary>
    /// Optional selector deciding the boxes to draw for the recognized text; return <c>null</c> for no
    /// overlay. When unset the analyzer draws a box per text block (subject to <see cref="IncludeTextBlocks"/>).
    /// </summary>
    public Func<TextRecognizedEventArgs, IReadOnlyList<OverlayBox>?>? OverlayProvider { get; set; }

    /// <summary>Raised on the UI thread when text is recognized in a frame.</summary>
    public event EventHandler<TextRecognizedEventArgs>? TextRecognized;

    /// <inheritdoc/>
    public override async ValueTask<IReadOnlyList<OverlayBox>?> AnalyzeAsync(CameraFrame frame, CancellationToken ct)
    {
        var text = await this.recognizer.RecognizeAsync(frame, ct).ConfigureAwait(false);
        if (text.Count == 0)
            return null;

        var args = new TextRecognizedEventArgs(text);
        this.Emit(() => this.TextRecognized?.Invoke(this, args), this.TextRecognizedCommand, args);

        return this.ResolveOverlay(args, this.OverlayProvider, () =>
        {
            if (!this.IncludeTextBlocks)
                return null;

            var boxes = new OverlayBox[text.Count];
            for (var i = 0; i < text.Count; i++)
                boxes[i] = new OverlayBox(text[i].BoundingBox, this.BoxColor, text[i].Text, this.BoxColor);
            return boxes;
        });
    }
}
