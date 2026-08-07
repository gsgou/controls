namespace Shiny.Maui.Controls.Keyframe.Export;

/// <summary>
/// The variable-width LZW compressor GIF image data uses.
/// </summary>
/// <remarks>
/// <para>Codes start one bit wider than the palette needs and grow as the dictionary fills, up to
/// 12 bits. When the dictionary is full the encoder emits a clear code and starts over — GIF
/// decoders rely on that rather than on the encoder finding a smarter continuation.</para>
/// <para>Output is packed least-significant-bit first, then split into sub-blocks of at most 255
/// bytes, each prefixed with its length. That framing is part of the GIF container, not the
/// compression, but the two are interleaved so they live together here.</para>
/// </remarks>
public static class LzwEncoder
{
    const int MaxCodeWidth = 12;
    const int MaxDictionarySize = 1 << MaxCodeWidth;

    /// <summary>Compresses palette indices and writes the framed result.</summary>
    /// <param name="output">Destination stream.</param>
    /// <param name="indices">One palette index per pixel.</param>
    /// <param name="minimumCodeWidth">Bits needed for the palette, at least 2.</param>
    public static void Encode(Stream output, byte[] indices, int minimumCodeWidth)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(indices);
        ArgumentOutOfRangeException.ThrowIfLessThan(minimumCodeWidth, 2);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minimumCodeWidth, 8);

        output.WriteByte((byte)minimumCodeWidth);

        var clearCode = 1 << minimumCodeWidth;
        var endCode = clearCode + 1;

        using var writer = new BlockWriter(output);

        var codeWidth = minimumCodeWidth + 1;
        var nextCode = endCode + 1;

        // Key is (prefix code << 8) | next index, which is unique because indices are bytes.
        var dictionary = new Dictionary<int, int>();

        writer.Write(clearCode, codeWidth);

        if (indices.Length == 0)
        {
            writer.Write(endCode, codeWidth);
            return;
        }

        int current = indices[0];

        for (var i = 1; i < indices.Length; i++)
        {
            var next = indices[i];
            var key = (current << 8) | next;

            if (dictionary.TryGetValue(key, out var existing))
            {
                current = existing;
                continue;
            }

            writer.Write(current, codeWidth);

            if (nextCode < MaxDictionarySize)
            {
                dictionary[key] = nextCode++;

                // Widen only after the code that needed the extra bit has been emitted, so the
                // decoder widens at the same moment we do.
                if (nextCode > (1 << codeWidth) && codeWidth < MaxCodeWidth)
                    codeWidth++;
            }
            else
            {
                writer.Write(clearCode, codeWidth);
                dictionary.Clear();
                codeWidth = minimumCodeWidth + 1;
                nextCode = endCode + 1;
            }

            current = next;
        }

        writer.Write(current, codeWidth);
        writer.Write(endCode, codeWidth);
    }

    /// <summary>Packs codes LSB-first and emits them as length-prefixed sub-blocks.</summary>
    sealed class BlockWriter(Stream output) : IDisposable
    {
        readonly byte[] block = new byte[255];

        int blockLength;
        int bitBuffer;
        int bitCount;

        public void Write(int code, int width)
        {
            bitBuffer |= code << bitCount;
            bitCount += width;

            while (bitCount >= 8)
            {
                Append((byte)(bitBuffer & 0xFF));
                bitBuffer >>= 8;
                bitCount -= 8;
            }
        }

        void Append(byte value)
        {
            block[blockLength++] = value;

            if (blockLength == block.Length)
                FlushBlock();
        }

        void FlushBlock()
        {
            if (blockLength == 0)
                return;

            output.WriteByte((byte)blockLength);
            output.Write(block, 0, blockLength);
            blockLength = 0;
        }

        public void Dispose()
        {
            // Pad the final partial byte; trailing bits past the end code are ignored by decoders.
            if (bitCount > 0)
            {
                Append((byte)(bitBuffer & 0xFF));
                bitBuffer = 0;
                bitCount = 0;
            }

            FlushBlock();
            output.WriteByte(0); // Block terminator.
        }
    }
}
