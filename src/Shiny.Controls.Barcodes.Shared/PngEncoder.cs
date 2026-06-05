using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Shiny.Controls.Barcodes;

/// <summary>
/// Minimal RGB8 PNG encoder for ZXing BitMatrix output. Avoids any platform-specific image dependency.
/// </summary>
internal static class PngEncoder
{
    static readonly byte[] Signature = { 137, 80, 78, 71, 13, 10, 26, 10 };

    public static byte[] Encode(
        ZXing.Common.BitMatrix matrix,
        byte fgR, byte fgG, byte fgB,
        byte bgR, byte bgG, byte bgB)
    {
        int w = matrix.Width;
        int h = matrix.Height;

        // Each row: 1 filter byte + w * 3 RGB bytes
        int rowStride = 1 + w * 3;
        var raw = new byte[h * rowStride];
        for (int y = 0; y < h; y++)
        {
            int rowStart = y * rowStride;
            raw[rowStart] = 0; // filter: None
            for (int x = 0; x < w; x++)
            {
                int p = rowStart + 1 + x * 3;
                if (matrix[x, y])
                {
                    raw[p] = fgR;
                    raw[p + 1] = fgG;
                    raw[p + 2] = fgB;
                }
                else
                {
                    raw[p] = bgR;
                    raw[p + 1] = bgG;
                    raw[p + 2] = bgB;
                }
            }
        }

        using var ms = new MemoryStream(raw.Length + 256);
        ms.Write(Signature, 0, Signature.Length);

        // IHDR
        var ihdr = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(0, 4), w);
        BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(4, 4), h);
        ihdr[8] = 8;  // bit depth
        ihdr[9] = 2;  // color type: RGB
        ihdr[10] = 0; // compression: deflate
        ihdr[11] = 0; // filter
        ihdr[12] = 0; // interlace: none
        WriteChunk(ms, "IHDR", ihdr);

        // IDAT (zlib-wrapped deflate)
        byte[] compressed;
        using (var dataStream = new MemoryStream())
        {
            using (var zlib = new ZLibStream(dataStream, CompressionLevel.Optimal, leaveOpen: true))
            {
                zlib.Write(raw, 0, raw.Length);
            }
            compressed = dataStream.ToArray();
        }
        WriteChunk(ms, "IDAT", compressed);

        // IEND
        WriteChunk(ms, "IEND", System.Array.Empty<byte>());

        return ms.ToArray();
    }

    static void WriteChunk(Stream s, string type, byte[] data)
    {
        var lenBuf = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(lenBuf, data.Length);
        s.Write(lenBuf, 0, 4);

        var typeBytes = Encoding.ASCII.GetBytes(type);
        s.Write(typeBytes, 0, typeBytes.Length);
        s.Write(data, 0, data.Length);

        var crc = Crc32.Compute(typeBytes, data);
        var crcBuf = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBuf, crc);
        s.Write(crcBuf, 0, 4);
    }

    static class Crc32
    {
        static readonly uint[] Table = BuildTable();

        static uint[] BuildTable()
        {
            var t = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                uint c = n;
                for (int k = 0; k < 8; k++)
                    c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
                t[n] = c;
            }
            return t;
        }

        public static uint Compute(byte[] a, byte[] b)
        {
            uint c = 0xFFFFFFFFu;
            for (int i = 0; i < a.Length; i++)
                c = Table[(c ^ a[i]) & 0xFF] ^ (c >> 8);
            for (int i = 0; i < b.Length; i++)
                c = Table[(c ^ b[i]) & 0xFF] ^ (c >> 8);
            return c ^ 0xFFFFFFFFu;
        }
    }
}
