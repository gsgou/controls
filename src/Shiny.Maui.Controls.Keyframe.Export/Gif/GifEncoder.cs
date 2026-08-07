using System.Text;

namespace Shiny.Maui.Controls.Keyframe.Export;

/// <summary>Settings for GIF encoding.</summary>
public sealed class GifOptions
{
    int maxColors = 256;
    int loopCount;

    /// <summary>Palette size per frame, 2 to 256.</summary>
    public int MaxColors
    {
        get => maxColors;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 2);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 256);
            maxColors = value;
        }
    }

    /// <summary>How many times to repeat. Zero, the default, loops forever.</summary>
    public int LoopCount
    {
        get => loopCount;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, ushort.MaxValue);
            loopCount = value;
        }
    }
}

/// <summary>
/// Writes an animated GIF, with no external dependency.
/// </summary>
/// <remarks>
/// <para><b>The frame-rate caveat.</b> GIF stores frame delays in hundredths of a second, so the
/// only frame rates it can represent exactly are the divisors of 100 — 50, 25, 20, 10fps and so on.
/// Ask for 30fps and every frame is written as 3cs, which actually plays at 33.3fps. The encoder
/// rounds and reports nothing, because there is no way to do better within the format. Export at
/// 25 or 50fps if exact timing matters, or use an image sequence and encode to a real video format.</para>
/// <para>Most browsers also silently promote a delay of 0 or 1cs to 10cs, so anything above 50fps
/// will play far slower than requested no matter what is written to the file.</para>
/// </remarks>
public static class GifEncoder
{
    /// <summary>Writes frames as an animated GIF.</summary>
    /// <param name="output">Destination stream. Left open.</param>
    /// <param name="frames">Frames in order. Enumerated lazily, one at a time.</param>
    /// <param name="fps">Playback rate, used to compute the per-frame delay.</param>
    /// <param name="options">Encoding settings.</param>
    /// <param name="cancellationToken">Stops encoding between frames.</param>
    public static void Encode(
        Stream output,
        IEnumerable<ExportedFrame> frames,
        int fps,
        GifOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(frames);
        ArgumentOutOfRangeException.ThrowIfLessThan(fps, 1);

        options ??= new GifOptions();

        // Delay is in hundredths of a second, and zero makes most viewers substitute their own
        // default. Clamp to 1 so fast animations stay fast rather than dropping to 10fps.
        var delay = Math.Max(1, (int)Math.Round(100d / fps));

        var started = false;
        var frameCount = 0;

        foreach (var frame in frames)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!started)
            {
                WriteHeader(output, frame.Width, frame.Height);
                WriteLoopExtension(output, options.LoopCount);
                started = true;
            }

            WriteFrame(output, frame, delay, options.MaxColors);
            frameCount++;
        }

        if (!started)
            throw new ArgumentException("No frames were produced, so there is nothing to encode.", nameof(frames));

        output.WriteByte(0x3B); // Trailer.
        output.Flush();
    }

    /// <summary>Writes frames as an animated GIF to a file.</summary>
    public static void EncodeToFile(
        string path,
        IEnumerable<ExportedFrame> frames,
        int fps,
        GifOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using var file = File.Create(path);
        Encode(file, frames, fps, options, cancellationToken);
    }

    static void WriteHeader(Stream output, int width, int height)
    {
        if (width > ushort.MaxValue || height > ushort.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(width),
                $"GIF dimensions are limited to {ushort.MaxValue} pixels; this frame is {width}×{height}.");

        output.Write(Encoding.ASCII.GetBytes("GIF89a"));

        WriteUInt16(output, width);
        WriteUInt16(output, height);

        // No global colour table — every frame carries its own. Bits 4-6 report the source colour
        // resolution, which decoders ignore but which is conventionally set to 7.
        output.WriteByte(0x70);
        output.WriteByte(0);    // Background colour index.
        output.WriteByte(0);    // Pixel aspect ratio: unspecified.
    }

    static void WriteLoopExtension(Stream output, int loopCount)
    {
        // The NETSCAPE2.0 application extension is how looping is expressed. It is a de facto
        // standard rather than part of the GIF spec, but universally understood.
        output.WriteByte(0x21);
        output.WriteByte(0xFF);
        output.WriteByte(0x0B);
        output.Write(Encoding.ASCII.GetBytes("NETSCAPE2.0"));
        output.WriteByte(0x03);
        output.WriteByte(0x01);
        WriteUInt16(output, loopCount);
        output.WriteByte(0x00);
    }

    static void WriteFrame(Stream output, ExportedFrame frame, int delay, int maxColors)
    {
        var quantized = ColorQuantizer.Quantize(frame.Pixels, maxColors);

        // GIF colour tables must be a power of two, at least 2 entries.
        var tableBits = Math.Max(1, BitsFor(quantized.Palette.Length));
        var tableSize = 1 << tableBits;

        WriteGraphicControl(output, delay, quantized.TransparentIndex);
        WriteImageDescriptor(output, frame.Width, frame.Height, tableBits);
        WriteColorTable(output, quantized.Palette, tableSize);

        LzwEncoder.Encode(output, quantized.Indices, Math.Max(2, tableBits));
    }

    static void WriteGraphicControl(Stream output, int delay, int transparentIndex)
    {
        output.WriteByte(0x21);
        output.WriteByte(0xF9);
        output.WriteByte(0x04);

        // Disposal method 1 ("do not dispose") leaves the previous frame in place. Every frame here
        // is full-size and opaque-or-keyed, so nothing needs restoring between them.
        var packed = 1 << 2;
        if (transparentIndex >= 0)
            packed |= 1;

        output.WriteByte((byte)packed);
        WriteUInt16(output, delay);
        output.WriteByte((byte)Math.Max(0, transparentIndex));
        output.WriteByte(0x00);
    }

    static void WriteImageDescriptor(Stream output, int width, int height, int tableBits)
    {
        output.WriteByte(0x2C);

        WriteUInt16(output, 0); // Left.
        WriteUInt16(output, 0); // Top.
        WriteUInt16(output, width);
        WriteUInt16(output, height);

        // Local colour table present, not interlaced, not sorted, size in the low three bits.
        output.WriteByte((byte)(0x80 | (tableBits - 1)));
    }

    static void WriteColorTable(Stream output, int[] palette, int tableSize)
    {
        var table = new byte[tableSize * 3];

        for (var i = 0; i < palette.Length && i < tableSize; i++)
        {
            table[i * 3] = (byte)(palette[i] >> 16);
            table[i * 3 + 1] = (byte)(palette[i] >> 8);
            table[i * 3 + 2] = (byte)palette[i];
        }

        output.Write(table);
    }

    static int BitsFor(int count)
    {
        var bits = 1;
        while (1 << bits < count)
            bits++;

        return Math.Min(bits, 8);
    }

    static void WriteUInt16(Stream output, int value)
    {
        output.WriteByte((byte)(value & 0xFF));
        output.WriteByte((byte)((value >> 8) & 0xFF));
    }
}
