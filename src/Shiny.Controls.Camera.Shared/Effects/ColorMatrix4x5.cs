namespace Shiny.Controls.Camera;

/// <summary>
/// An immutable 4x5 row-major colour matrix — the one colour representation every platform can honour.
/// Each row is <c>[r, g, b, a, offset]</c> for the output R, G, B and A channel respectively:
/// <c>outR = r*R + g*G + b*B + a*A + offset</c>, with all channels and the offset in <b>normalized 0..1
/// space</b>.
/// </summary>
/// <remarks>
/// <para>
/// Normalized offsets are the SVG <c>feColorMatrix</c> / Core Image convention. Android's
/// <c>android.graphics.ColorMatrix</c> instead expects the offset column in 0..255 space, which is what
/// <see cref="ToAndroidArray"/> produces — use it (not <see cref="Values"/>) when handing the matrix to
/// Android, or every offset lands 255x too small.
/// </para>
/// <para>
/// Instances are immutable and cheap to share; the built-in effects allocate theirs once at startup.
/// </para>
/// </remarks>
public sealed class ColorMatrix4x5
{
    readonly float[] values;

    /// <param name="values">Exactly 20 floats, row-major, offsets in 0..1 space.</param>
    /// <exception cref="ArgumentException"><paramref name="values"/> is not exactly 20 elements.</exception>
    public ColorMatrix4x5(ReadOnlySpan<float> values)
    {
        if (values.Length != 20)
            throw new ArgumentException("A colour matrix requires exactly 20 values (4 rows x 5 columns).", nameof(values));

        this.values = values.ToArray();
    }

    /// <summary>The 20 coefficients, row-major, offsets in 0..1 space.</summary>
    public ReadOnlySpan<float> Values => this.values;

    /// <summary>Read a single coefficient.</summary>
    /// <param name="row">Output channel: 0=R, 1=G, 2=B, 3=A.</param>
    /// <param name="column">Input channel 0..3, or 4 for the offset.</param>
    public float this[int row, int column] => this.values[(row * 5) + column];

    /// <summary>The identity matrix (passthrough).</summary>
    public static ColorMatrix4x5 Identity { get; } = new([
        1, 0, 0, 0, 0,
        0, 1, 0, 0, 0,
        0, 0, 1, 0, 0,
        0, 0, 0, 1, 0
    ]);

    /// <summary><c>true</c> when this matrix leaves every pixel unchanged, so a backend can skip it entirely.</summary>
    public bool IsIdentity
    {
        get
        {
            var identity = Identity.values;
            for (var i = 0; i < 20; i++)
            {
                if (Math.Abs(this.values[i] - identity[i]) > 1e-6f)
                    return false;
            }
            return true;
        }
    }

    /// <summary>
    /// A saturation adjustment, using the same luminance weights as
    /// <c>android.graphics.ColorMatrix.setSaturation</c> (0.213 / 0.715 / 0.072) so a matrix built here and
    /// one built natively on Android produce identical pixels.
    /// </summary>
    /// <param name="saturation">0 = fully desaturated, 1 = unchanged, &gt;1 = boosted.</param>
    public static ColorMatrix4x5 Saturation(float saturation)
    {
        var inv = 1f - saturation;
        var r = 0.213f * inv;
        var g = 0.715f * inv;
        var b = 0.072f * inv;

        return new([
            r + saturation, g,              b,              0, 0,
            r,              g + saturation, b,              0, 0,
            r,              g,              b + saturation, 0, 0,
            0,              0,              0,              1, 0
        ]);
    }

    /// <summary>
    /// A uniform scale + offset applied to R, G and B (alpha untouched) — the "contrast and lift" building
    /// block the built-in looks are assembled from.
    /// </summary>
    /// <param name="scale">Multiplier applied to each colour channel.</param>
    /// <param name="offset">Constant added to each colour channel, in 0..1 space.</param>
    public static ColorMatrix4x5 ScaleOffset(float scale, float offset) => new([
        scale, 0,     0,     0, offset,
        0,     scale, 0,     0, offset,
        0,     0,     scale, 0, offset,
        0,     0,     0,     1, 0
    ]);

    /// <summary>
    /// Compose: apply <c>this</c> first, then <paramref name="next"/>. Equivalent to Android's
    /// <c>PostConcat</c>, and to chaining two effects in <c>CameraView.Effects</c> order.
    /// </summary>
    public ColorMatrix4x5 Then(ColorMatrix4x5 next)
    {
        ArgumentNullException.ThrowIfNull(next);

        var a = this.values;
        var b = next.values;
        var result = new float[20];

        for (var row = 0; row < 4; row++)
        {
            for (var col = 0; col < 4; col++)
            {
                var sum = 0f;
                for (var k = 0; k < 4; k++)
                    sum += b[(row * 5) + k] * a[(k * 5) + col];

                result[(row * 5) + col] = sum;
            }

            // offset column: b's weighted sum of a's offsets, plus b's own offset
            var offset = b[(row * 5) + 4];
            for (var k = 0; k < 4; k++)
                offset += b[(row * 5) + k] * a[(k * 5) + 4];

            result[(row * 5) + 4] = offset;
        }

        return new ColorMatrix4x5(result);
    }

    /// <summary>
    /// The same coefficients with the offset column rescaled to 0..255, ready for
    /// <c>new android.graphics.ColorMatrix(float[])</c>.
    /// </summary>
    public float[] ToAndroidArray()
    {
        var result = this.values.AsSpan().ToArray();
        for (var row = 0; row < 4; row++)
            result[(row * 5) + 4] *= 255f;

        return result;
    }

    /// <summary>
    /// The coefficients as an SVG <c>feColorMatrix</c> <c>values</c> attribute (space-separated, offsets in
    /// 0..1 space — the convention SVG already uses, so no rescaling).
    /// </summary>
    public string ToSvgValues() => string.Join(" ", this.values.Select(v => v.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture)));

    /// <summary>Apply this matrix to a single premultiplied-free BGRA pixel, clamping to 0..255.</summary>
    internal void ApplyBgra(ref byte blue, ref byte green, ref byte red, ref byte alpha)
    {
        var r = red / 255f;
        var g = green / 255f;
        var b = blue / 255f;
        var a = alpha / 255f;

        var nr = (this.values[0] * r) + (this.values[1] * g) + (this.values[2] * b) + (this.values[3] * a) + this.values[4];
        var ng = (this.values[5] * r) + (this.values[6] * g) + (this.values[7] * b) + (this.values[8] * a) + this.values[9];
        var nb = (this.values[10] * r) + (this.values[11] * g) + (this.values[12] * b) + (this.values[13] * a) + this.values[14];
        var na = (this.values[15] * r) + (this.values[16] * g) + (this.values[17] * b) + (this.values[18] * a) + this.values[19];

        red = Clamp(nr);
        green = Clamp(ng);
        blue = Clamp(nb);
        alpha = Clamp(na);
    }

    static byte Clamp(float v) => (byte)Math.Clamp((int)MathF.Round(v * 255f), 0, 255);
}
