namespace Shiny.Controls.Office;

/// <summary>
/// The measurement units OOXML uses, converted to pixels at 96 dpi.
/// </summary>
/// <remarks>
/// Every format in the family invents its own unit, and mixing them up produces layouts that are wrong
/// by an exact ratio — content 1/20th the intended size, or 12,700 times too large — which reads as a
/// mysterious rendering bug rather than a unit error.
/// </remarks>
public static class OoxmlUnits
{
    /// <summary>English Metric Units per inch. DrawingML positions and sizes are in these.</summary>
    public const double EmuPerInch = 914400;

    public const double PixelsPerInch = 96;
    public const double PointsPerInch = 72;

    /// <summary>DrawingML geometry: EMU to pixels.</summary>
    public static double EmuToPixels(long emu) => emu / EmuPerInch * PixelsPerInch;

    public static long PixelsToEmu(double pixels) => (long)Math.Round(pixels / PixelsPerInch * EmuPerInch);

    /// <summary>WordprocessingML sizes are in twentieths of a point.</summary>
    public static double TwipsToPixels(double twips) => twips / 20 / PointsPerInch * PixelsPerInch;

    /// <summary>Run font size in WordprocessingML is in half-points.</summary>
    public static double HalfPointsToPixels(double halfPoints) => halfPoints / 2 / PointsPerInch * PixelsPerInch;

    /// <summary>DrawingML font size is in hundredths of a point.</summary>
    public static double HundredthPointsToPixels(double hundredths) => hundredths / 100 / PointsPerInch * PixelsPerInch;

    public static double PointsToPixels(double points) => points / PointsPerInch * PixelsPerInch;

    /// <summary>Pixels back to points, for showing a size in the units a picker uses.</summary>
    public static double PixelsToPointsApprox(double pixels) => Math.Round(pixels / PixelsPerInch * PointsPerInch, 1);

    /// <summary>DrawingML angles are in 60,000ths of a degree.</summary>
    public static double AngleToDegrees(int angle) => angle / 60000d;

    /// <summary>
    /// The XML value of an enum-typed attribute, e.g. <c>"roundRect"</c> for a preset geometry.
    /// </summary>
    /// <remarks>
    /// OpenXml 3.x models these as record structs whose <c>ToString()</c> returns
    /// <c>"ShapeTypeValues { }"</c> — a compiling, non-throwing, entirely useless string that silently
    /// matches nothing. The only reliable source of the spec's own token is the serialised attribute.
    /// </remarks>
    public static string? EnumAttribute(DocumentFormat.OpenXml.OpenXmlElement? element, string localName)
    {
        if (element is null)
            return null;

        var value = element.GetAttribute(localName, string.Empty).Value;
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
