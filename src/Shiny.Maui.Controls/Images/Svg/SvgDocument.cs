using System.IO.Compression;
using System.Text;

namespace Shiny.Maui.Controls.Images.Svg;

/// <summary>
/// A parsed SVG, ready to draw onto any <see cref="ICanvas"/>.
/// </summary>
/// <remarks>
/// <para>Parsing is where the cost is - XML, path data, colours, transforms and gradient ramps all
/// resolve here, once. A document is immutable afterwards and holds no canvas or platform state, so
/// one instance can be drawn by any number of controls, at any number of sizes, on any thread that
/// owns a canvas. That is what <see cref="SvgCache"/> exploits.</para>
///
/// <para><b>What is drawn:</b> <c>path</c>, <c>rect</c>, <c>circle</c>, <c>ellipse</c>, <c>line</c>,
/// <c>polyline</c>, <c>polygon</c>, <c>text</c>, <c>g</c>, <c>use</c>, <c>symbol</c>, <c>defs</c>,
/// <c>switch</c>, <c>clipPath</c>, linear and radial gradients, the presentation attributes, the
/// <c>style</c> attribute, and the type/class/id rules inside a <c>&lt;style&gt;</c> element.</para>
///
/// <para><b>What is not:</b> filters, masks, patterns, markers, embedded raster <c>&lt;image&gt;</c>,
/// CSS animation and SMIL, and external references of any kind. An element this renderer does not
/// know is skipped rather than approximated, so an unsupported feature costs that element and
/// nothing else.</para>
/// </remarks>
public sealed class SvgDocument
{
    // Enough to see past a BOM, an XML declaration and a DOCTYPE before giving up on the sniff.
    const int SniffLength = 1024;

    readonly SvgGroup root;
    int? nodeCount;

    internal SvgDocument(SizeF size, RectF viewBox, PointF alignment, SvgGroup root)
    {
        this.Size = size;
        this.ViewBox = viewBox;
        this.Alignment = alignment;
        this.root = root;
    }


    /// <summary>
    /// The drawing's intrinsic size, from its <c>width</c>/<c>height</c> or, failing those, its
    /// <c>viewBox</c>. Use it to give an SVG a natural size when the layout does not impose one.
    /// </summary>
    public SizeF Size { get; }

    /// <summary>The user-space rectangle that maps onto whatever bounds the document is drawn into.</summary>
    public RectF ViewBox { get; }

    /// <summary>How leftover space is distributed, from <c>preserveAspectRatio</c>. Centre by default.</summary>
    internal PointF Alignment { get; }

    /// <summary>How many drawable nodes the document holds - the cache's measure of what it costs.</summary>
    public int NodeCount => this.nodeCount ??= this.root.Weight;

    /// <summary>The parsed tree.</summary>
    internal SvgGroup Root => this.root;


    /// <summary>
    /// Parses SVG markup.
    /// </summary>
    /// <exception cref="FormatException">The content is not well-formed SVG.</exception>
    public static SvgDocument Parse(string markup) => SvgParser.Parse(markup);


    /// <summary>
    /// Parses SVG bytes, transparently decompressing gzipped <c>.svgz</c> content.
    /// </summary>
    /// <exception cref="FormatException">The content is not well-formed SVG.</exception>
    public static SvgDocument Parse(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        return Parse(Decode(bytes));
    }


    /// <summary>
    /// Whether a payload looks like SVG rather than a raster image.
    /// </summary>
    /// <remarks>
    /// A URL's extension is a hint and nothing more - plenty of CDNs serve <c>/avatar/1234</c> and
    /// decide the format by content negotiation - so the bytes get the final say. The check is a
    /// sniff of the first kilobyte, not a parse, so it costs nothing on the raster path.
    /// </remarks>
    public static bool LooksLikeSvg(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 4)
            return false;

        if (IsGzip(bytes))
            return true;

        var window = bytes[..Math.Min(SniffLength, bytes.Length)];
        var text = DecodeText(window);

        // The root may be preceded by a declaration, a DOCTYPE and comments, so this looks for the
        // tag anywhere in the window rather than only at the start.
        return text.Contains("<svg", StringComparison.OrdinalIgnoreCase);
    }


    /// <summary>
    /// Draws the document into a rectangle.
    /// </summary>
    /// <param name="canvas">The surface to draw to.</param>
    /// <param name="bounds">Where the drawing goes.</param>
    /// <param name="aspect">How the <see cref="ViewBox"/> is scaled into <paramref name="bounds"/>.</param>
    /// <param name="currentColor">What <c>currentColor</c> resolves to.</param>
    public void Draw(ICanvas canvas, RectF bounds, Aspect aspect, Color currentColor)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        if (bounds.Width <= 0f || bounds.Height <= 0f || this.ViewBox.Width <= 0f || this.ViewBox.Height <= 0f)
            return;

        var scaleX = bounds.Width / this.ViewBox.Width;
        var scaleY = bounds.Height / this.ViewBox.Height;

        (scaleX, scaleY) = aspect switch
        {
            Aspect.AspectFill => (Math.Max(scaleX, scaleY), Math.Max(scaleX, scaleY)),
            Aspect.Fill => (scaleX, scaleY),
            Aspect.Center => (1f, 1f),
            _ => (Math.Min(scaleX, scaleY), Math.Min(scaleX, scaleY))
        };

        canvas.SaveState();

        try
        {
            // AspectFill and Center both let the drawing exceed its bounds, and a vector has no
            // frame of its own to stop at - so the clip is unconditional rather than conditional on
            // arithmetic that has to be right every time.
            canvas.ClipRectangle(bounds);

            canvas.Translate(
                bounds.X + ((bounds.Width - (this.ViewBox.Width * scaleX)) * this.Alignment.X),
                bounds.Y + ((bounds.Height - (this.ViewBox.Height * scaleY)) * this.Alignment.Y)
            );

            canvas.Scale(scaleX, scaleY);
            canvas.Translate(-this.ViewBox.X, -this.ViewBox.Y);

            this.root.Draw(canvas, new SvgDrawContext(currentColor), 1f);
        }
        finally
        {
            canvas.RestoreState();
        }
    }


    static string Decode(byte[] bytes)
    {
        if (!IsGzip(bytes))
            return DecodeText(bytes);

        // .svgz is just gzipped .svg, and servers hand it over with the extension intact rather
        // than as Content-Encoding, so nothing upstream will have unwrapped it.
        using var compressed = new MemoryStream(bytes, false);
        using var gzip = new GZipStream(compressed, CompressionMode.Decompress);
        using var expanded = new MemoryStream();

        gzip.CopyTo(expanded);

        return DecodeText(expanded.GetBuffer().AsSpan(0, (int)expanded.Length));
    }


    static bool IsGzip(ReadOnlySpan<byte> bytes) => bytes.Length >= 2 && bytes[0] == 0x1F && bytes[1] == 0x8B;


    static string DecodeText(ReadOnlySpan<byte> bytes)
    {
        // UTF-8 is what SVG is in practice, but a Windows-authored file can be UTF-16 and would
        // otherwise decode into interleaved nulls that match nothing.
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return Encoding.Unicode.GetString(bytes[2..]);

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            return Encoding.BigEndianUnicode.GetString(bytes[2..]);

        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return Encoding.UTF8.GetString(bytes[3..]);

        return Encoding.UTF8.GetString(bytes);
    }
}
