namespace Shiny.Controls.Office.Theming;

/// <summary>How a watermark is sized against the surface it sits on.</summary>
public enum OfficeWatermarkFit
{
    /// <summary>Scaled to <see cref="OfficeWatermark.Scale"/> of the surface, keeping its shape.</summary>
    Contain,

    /// <summary>Drawn at its own pixel size, centred.</summary>
    Native,

    /// <summary>Repeated across the surface. For a texture rather than a mark.</summary>
    Tile
}


/// <summary>
/// A picture drawn behind a document, deck or sheet — a logo, a DRAFT stamp, a company mark.
/// </summary>
/// <remarks>
/// <para>
/// This is a <b>display</b> watermark: the surfaces draw it, the viewers draw it, and it is not
/// written into the file. That is a deliberate limit rather than an omission, and worth knowing before
/// reaching for it. The three formats have no common notion of one — Word keeps a VML shape in the
/// header part, Excel has no watermark at all and fakes it with a header-and-footer image, and
/// PowerPoint has none either and expects a picture on the slide master. Persisting to all three means
/// three unrelated mechanisms; drawing on all three means one, and it is the same one every viewer
/// already has.
/// </para>
/// <para>
/// So it is right for stamping a preview, marking a draft, or badging an export, and wrong as the way
/// to put a permanent watermark into a document someone else will open in Word.
/// </para>
/// </remarks>
public sealed record OfficeWatermark
{
    /// <summary>The encoded picture — PNG, JPEG or anything the platform decodes.</summary>
    public required byte[] Image { get; init; }

    /// <summary>
    /// How much of it shows through, 0 to 1.
    /// </summary>
    /// <remarks>
    /// Low by default and it has to be: a watermark is behind text that still has to be read, and the
    /// failure people actually hit is one drawn at full strength that makes the page unusable. Word's
    /// own washout is in this region.
    /// </remarks>
    public double Opacity { get; init; } = 0.15;

    /// <summary>For <see cref="OfficeWatermarkFit.Contain"/>, the fraction of the surface it spans.</summary>
    public double Scale { get; init; } = 0.6;

    /// <summary>Turned about its own centre. 315 is the diagonal a DRAFT stamp is usually set on.</summary>
    public double RotationDegrees { get; init; }

    public OfficeWatermarkFit Fit { get; init; } = OfficeWatermarkFit.Contain;

    /// <summary>True when there is actually something to draw.</summary>
    public bool IsEmpty => this.Image is not { Length: > 0 } || this.Opacity <= 0 || this.Scale <= 0;
}
