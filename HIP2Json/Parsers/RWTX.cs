using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace HIP2Json;

public sealed class RWTXParser : AssetParser
{
    private const int HeaderOffset = 0x34;
    private const int PixelOffset = 0xA0;
    private const int PlatformTypeOffset = HeaderOffset + 0x00;
    private const int FilterAddressOffset = HeaderOffset + 0x04;
    private const int TextureNameOffset = HeaderOffset + 0x18;
    private const int AlphaNameOffset = HeaderOffset + 0x38;
    private const int RasterFormatOffset = HeaderOffset + 0x58;
    private const int WidthOffset = HeaderOffset + 0x5C;
    private const int HeightOffset = HeaderOffset + 0x5E;
    private const int BitDepthOffset = HeaderOffset + 0x60;
    private const int MipCountOffset = HeaderOffset + 0x61;
    private const int RasterTypeOffset = HeaderOffset + 0x62;
    private const int CompressionOffset = HeaderOffset + 0x63;
    private const int TransparencyOffset = HeaderOffset + 0x67;

    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        byte[] data = br.ReadBytes((int)(br.BaseStream.Length - br.BaseStream.Position));

        if (data.Length < PixelOffset)
            return new RWTX { prefix = Convert.ToBase64String(data) };

        bool gcLayout = Util.ReadUInt32(data, PlatformTypeOffset) == 6;

        if (!gcLayout)
        {
            return new RWTX
            {
                platformType = Util.ReadUInt32(data, PlatformTypeOffset),
                filterAndAddress = Util.ReadUInt32(data, FilterAddressOffset),
                prefix = Convert.ToBase64String(data.AsSpan(0, PixelOffset)),
                pixelData = Convert.ToBase64String(data.AsSpan(PixelOffset)),
            };
        }

        ushort width = Util.ReadUInt16(data, WidthOffset);
        ushort height = Util.ReadUInt16(data, HeightOffset);
        byte bitDepth = data[BitDepthOffset];
        uint fmt = Util.ReadUInt32(data, RasterFormatOffset);

        byte[] payload = data.AsSpan(PixelOffset).ToArray();

        string format = null;
        string pngBase64 = null;

        GxFormat gx = RwtxGx.Detect(bitDepth, fmt);
        if (gx != GxFormat.None && width > 0 && height > 0 && TryDecodeMip0(payload, gx, fmt, width, height, out byte[] rgba, out byte[] _))
        {
            byte[] png = PngCodec.Encode(width, height, rgba);
            if (png != null)
                pngBase64 = Convert.ToBase64String(png);

            format = RwtxGx.FormatName(gx, fmt);
        }

        return new RWTX
        {
            platformType = Util.ReadUInt32(data, PlatformTypeOffset),
            filterAndAddress = Util.ReadUInt32(data, FilterAddressOffset),
            textureName = ReadName(data, TextureNameOffset),
            alphaName = ReadName(data, AlphaNameOffset),
            rasterFormatFlags = fmt,
            width = width,
            height = height,
            bitDepth = bitDepth,
            mipMapCount = data[MipCountOffset],
            rasterType = data[RasterTypeOffset],
            compression = data[CompressionOffset],
            transparency = data[TransparencyOffset],
            prefix = Convert.ToBase64String(data.AsSpan(0, PixelOffset)),
            pixelData = Convert.ToBase64String(payload),
            format = format,
            pngBase64 = pngBase64,
        };
    }

    public override object Serialize(object obj)
    {
        RWTX rwtx = (RWTX)obj;

        byte[] prefix = Convert.FromBase64String(rwtx.prefix ?? string.Empty);

        if (prefix.Length != PixelOffset)
            throw new InvalidDataException("RWTX prefix length mismatch");

        if (rwtx.platformType == 6)
        {
            WriteUInt32(prefix, RasterFormatOffset, rwtx.rasterFormatFlags);
            WriteUInt16(prefix, WidthOffset, rwtx.width);
            WriteUInt16(prefix, HeightOffset, rwtx.height);
            prefix[BitDepthOffset] = rwtx.bitDepth;
            prefix[MipCountOffset] = rwtx.mipMapCount;
            prefix[RasterTypeOffset] = rwtx.rasterType;
            prefix[CompressionOffset] = rwtx.compression;
            prefix[TransparencyOffset] = rwtx.transparency;
        }

        byte[] pixels = Convert.FromBase64String(rwtx.pixelData ?? string.Empty);

        if (rwtx.platformType == 6 && rwtx.width > 0 && rwtx.height > 0 && !string.IsNullOrEmpty(rwtx.pngBase64))
        {
            try
            {
                GxFormat gx = RwtxGx.Detect(rwtx.bitDepth, rwtx.rasterFormatFlags);
                if (gx != GxFormat.None)
                {
                    PngImage png = PngCodec.Decode(Convert.FromBase64String(rwtx.pngBase64));
                    if (png.Width == rwtx.width && png.Height == rwtx.height && TryDecodeMip0(pixels, gx, rwtx.rasterFormatFlags, rwtx.width, rwtx.height, out byte[] origRgba, out byte[] origMip0))
                    {
                        if (!RwtxGx.EqualRgba(origRgba, png.Rgba))
                        {
                            byte[] reencoded = RwtxGx.EncodeMip0Pixels(pixels, gx, rwtx.rasterFormatFlags, rwtx.width, rwtx.height, png.Rgba, origMip0, origRgba);
                            if (reencoded != null)
                                pixels = reencoded;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning("RWTX png re-encode skipped: " + ex.Message);
            }
        }

        using var ms = new MemoryStream(prefix.Length + pixels.Length);
        ms.Write(prefix, 0, prefix.Length);
        ms.Write(pixels, 0, pixels.Length);
        return ms.ToArray();
    }

    private static bool TryDecodeMip0(byte[] payload, GxFormat gx, uint fmt, int width, int height, out byte[] rgba, out byte[] mip0)
    {
        rgba = null;
        mip0 = null;
        try
        {
            rgba = RwtxGx.DecodeMip0(payload, gx, fmt, width, height, out mip0);
            return rgba != null && mip0 != null;
        }
        catch
        {
            return false;
        }
    }

    private static string ReadName(byte[] data, int offset)
    {
        int len = 0;
        while (len < 32 && data[offset + len] != 0 && data[offset + len] != 0xDD)
            len++;
        return Encoding.Latin1.GetString(data, offset, len);
    }

    private static void WriteUInt16(byte[] data, int offset, ushort value)
    {
        byte[] b = BitConverter.GetBytes(value);
        if (Program.BigEndian == true)
            Array.Reverse(b);
        Array.Copy(b, 0, data, offset, 2);
    }

    private static void WriteUInt32(byte[] data, int offset, uint value)
    {
        byte[] b = BitConverter.GetBytes(value);
        if (Program.BigEndian == true)
            Array.Reverse(b);
        Array.Copy(b, 0, data, offset, 4);
    }
}

public class RWTX
{
    public uint platformType { get; set; }
    public uint filterAndAddress { get; set; }
    public string textureName { get; set; }
    public string alphaName { get; set; }
    public uint rasterFormatFlags { get; set; }
    public ushort width { get; set; }
    public ushort height { get; set; }
    public byte bitDepth { get; set; }
    public byte mipMapCount { get; set; }
    public byte rasterType { get; set; }
    public byte compression { get; set; }
    public byte transparency { get; set; }
    public string prefix { get; set; }
    public string pixelData { get; set; }
    public string format { get; set; }
    public string pngBase64 { get; set; }
}

public enum GxFormat
{
    None,
    I4,
    I8,
    IA4,
    IA8,
    CI4,
    CI8,
    Rgb565,
    Rgb5A3,
    Rgba8,
    Cmpr,
}

public enum GxPalette
{
    None,
    Rgb565,
    Argb4444,
}

public static class RwtxGx
{
    private const uint RasterFormatPal8 = 0x2000;
    private const uint RasterFormat565 = 0x0200;
    private const uint RasterFormat4444 = 0x0300;
    private const int Ci8PaletteSize = 512;
    private const int Ci4PaletteSize = 32;
    private static readonly byte[][] TrailerPatterns = new[]
    {
        new byte[] { 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x03, 0x10,
                     0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x03, 0x10 },
    };

    public static GxFormat Detect(byte bitDepth, uint fmt)
    {
        uint pf = fmt & 0x0F00;
        bool pal8 = (fmt & RasterFormatPal8) != 0;

        if (pal8)
            return bitDepth == 8 ? GxFormat.CI8 : bitDepth == 4 ? GxFormat.CI4 : GxFormat.None;

        if (pf == RasterFormat565)
            return GxFormat.Rgb565;
        if (pf == RasterFormat4444)
            return GxFormat.Rgb5A3;
        if (bitDepth == 4)
            return GxFormat.I4;
        if (bitDepth == 8)
            return GxFormat.I8;

        return GxFormat.None;
    }

    public static GxPalette PaletteFormat(uint fmt)
    {
        if ((fmt & 0x0F00) == RasterFormat565)
            return GxPalette.Rgb565;
        return GxPalette.Argb4444;
    }

    public static string FormatName(GxFormat gx, uint fmt)
    {
        switch (gx)
        {
            case GxFormat.I4:
                return "I4";
            case GxFormat.I8:
                return "I8";
            case GxFormat.IA4:
                return "IA4";
            case GxFormat.IA8:
                return "IA8";
            case GxFormat.CI4:
                return "CI4";
            case GxFormat.CI8:
                return PaletteFormat(fmt) == GxPalette.Rgb565 ? "CI8 (RGB565 palette)" : "CI8 (ARGB4444 palette)";
            case GxFormat.Rgb565:
                return "RGB565";
            case GxFormat.Rgb5A3:
                return "RGB5A3";
            case GxFormat.Rgba8:
                return "RGBA8";
            case GxFormat.Cmpr:
                return "CMPR";
            default:
                return null;
        }
    }

    public static bool EqualRgba(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
            return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
                return false;
        }
        return true;
    }

    public static byte[] DecodeMip0(byte[] payload, GxFormat gx, uint fmt, int width, int height, out byte[] mip0)
    {
        int paletteSize = gx == GxFormat.CI8 ? Ci8PaletteSize : gx == GxFormat.CI4 ? Ci4PaletteSize : 0;
        int trailer = TrailerEndsWith(payload) ? 24 : 0;

        byte[] palette = null;
        if (paletteSize > 0)
        {
            palette = new byte[paletteSize];
            Array.Copy(payload, 0, palette, 0, paletteSize);
        }

        int core = payload.Length - trailer - paletteSize;
        if (core < 0)
        {
            mip0 = null;
            return null;
        }

        int mip0Bytes = RwtxGx.MipBytes(gx, width, height);
        if (mip0Bytes <= 0 || core < mip0Bytes)
        {
            mip0 = null;
            return null;
        }

        mip0 = new byte[mip0Bytes];
        Array.Copy(payload, paletteSize, mip0, 0, mip0Bytes);

        return DecodeMip(mip0, width, height, gx, palette, fmt);
    }

    public static byte[] EncodeMip0Pixels(byte[] payload, GxFormat gx, uint fmt, int width, int height, byte[] rgba, byte[] origMip0, byte[] origRgba)
    {
        int paletteSize = gx == GxFormat.CI8 ? Ci8PaletteSize : gx == GxFormat.CI4 ? Ci4PaletteSize : 0;
        int trailer = TrailerEndsWith(payload) ? 24 : 0;

        byte[] palette = null;
        if (paletteSize > 0)
        {
            palette = new byte[paletteSize];
            Array.Copy(payload, 0, palette, 0, paletteSize);
        }

        byte[] newMip0 = EncodeMip(rgba, width, height, gx, palette, fmt, origMip0, origRgba);
        if (newMip0 == null)
            return null;

        byte[] outBytes = new byte[payload.Length];
        int offset = 0;

        if (paletteSize > 0)
        {
            Array.Copy(palette, 0, outBytes, 0, paletteSize);
            offset = paletteSize;
        }

        Array.Copy(newMip0, 0, outBytes, offset, newMip0.Length);
        offset += newMip0.Length;

        if (trailer > 0)
        {
            int rest = payload.Length - trailer - offset;
            if (rest > 0)
                Array.Copy(payload, offset, outBytes, offset, rest);
            Array.Copy(payload, payload.Length - trailer, outBytes, payload.Length - trailer, trailer);
        }

        return outBytes;
    }

    public static int MipBytes(GxFormat gx, int width, int height)
    {
        GetBlock(gx, out int bw, out int bh, out int blockBytes);
        int tilesX = (width + bw - 1) / bw;
        int tilesY = (height + bh - 1) / bh;
        return tilesX * tilesY * blockBytes;
    }

    public static void GetBlock(GxFormat gx, out int bw, out int bh, out int blockBytes)
    {
        switch (gx)
        {
            case GxFormat.I4:
            case GxFormat.CI4:
            case GxFormat.Cmpr:
                bw = 8;
                bh = 8;
                blockBytes = gx == GxFormat.Cmpr ? 64 : 32;
                break;
            case GxFormat.I8:
                bw = 8;
                bh = 4;
                blockBytes = 32;
                break;
            case GxFormat.CI8:
            case GxFormat.IA4:
                bw = 8;
                bh = 4;
                blockBytes = 32;
                break;
            default:
                bw = 4;
                bh = 4;
                blockBytes = gx == GxFormat.Rgba8 ? 64 : 32;
                break;
        }
    }

    private static bool TrailerEndsWith(byte[] data)
    {
        if (data.Length < 24)
            return false;
        foreach (byte[] pattern in TrailerPatterns)
        {
            bool match = true;
            for (int i = 0; i < 24; i++)
            {
                if (data[data.Length - 24 + i] != pattern[i])
                {
                    match = false;
                    break;
                }
            }
            if (match)
                return true;
        }
        return false;
    }

    public static byte[] DecodeMip(byte[] src, int width, int height, GxFormat gx, byte[] palette, uint fmt)
    {
        if (src == null || width <= 0 || height <= 0)
            return null;

        byte[] rgba = new byte[width * height * 4];
        GxPalette palFmt = gx == GxFormat.CI8 || gx == GxFormat.CI4 ? PaletteFormat(fmt) : GxPalette.None;

        GetBlock(gx, out int bw, out int bh, out int blockBytes);
        int tilesX = (width + bw - 1) / bw;

        for (int by = 0; by < height; by += bh)
        {
            for (int bx = 0; bx < width; bx += bw)
            {
                int tile = (by / bh) * tilesX + (bx / bw);
                int blockOffset = tile * blockBytes;
                DecodeBlock(src, blockOffset, rgba, width, height, bx, by, bw, bh, gx, palette, palFmt);
            }
        }

        return rgba;
    }

    private static void DecodeBlock(byte[] src, int blockOffset, byte[] rgba, int width, int height, int bx, int by, int bw, int bh, GxFormat gx, byte[] palette, GxPalette palFmt)
    {
        switch (gx)
        {
            case GxFormat.I4:
                for (int y = 0; y < bh; y++)
                {
                    for (int x = 0; x < bw; x++)
                    {
                        int byteIndex = blockOffset + y * (bw / 2) + x / 2;
                        byte val = src[byteIndex];
                        if (x % 2 == 0)
                            val >>= 4;
                        val &= 0x0F;
                        int v = val * 17;
                        SetPixel(rgba, width, height, bx + x, by + y, (byte)v, (byte)v, (byte)v, 255);
                    }
                }
                break;

            case GxFormat.I8:
                for (int y = 0; y < bh; y++)
                {
                    for (int x = 0; x < bw; x++)
                    {
                        byte v = src[blockOffset + y * bw + x];
                        SetPixel(rgba, width, height, bx + x, by + y, v, v, v, 255);
                    }
                }
                break;

            case GxFormat.IA4:
                for (int y = 0; y < bh; y++)
                {
                    for (int x = 0; x < bw; x++)
                    {
                        byte val = src[blockOffset + y * bw + x];
                        byte a = (byte)(((val >> 4) & 0x0F) * 17);
                        byte i = (byte)((val & 0x0F) * 17);
                        SetPixel(rgba, width, height, bx + x, by + y, i, i, i, a);
                    }
                }
                break;

            case GxFormat.IA8:
                for (int y = 0; y < bh; y++)
                {
                    for (int x = 0; x < bw; x++)
                    {
                        ushort val = (ushort)((src[blockOffset + y * (bw * 2) + x * 2] << 8) | src[blockOffset + y * (bw * 2) + x * 2 + 1]);
                        byte i = (byte)(val >> 8);
                        byte a = (byte)(val & 0xFF);
                        SetPixel(rgba, width, height, bx + x, by + y, i, i, i, a);
                    }
                }
                break;

            case GxFormat.CI4:
                for (int y = 0; y < bh; y++)
                {
                    for (int x = 0; x < bw; x++)
                    {
                        int byteIndex = blockOffset + y * (bw / 2) + x / 2;
                        byte val = src[byteIndex];
                        if (x % 2 == 0)
                            val >>= 4;
                        val &= 0x0F;
                        SetPalettePixel(rgba, width, height, bx + x, by + y, val, palette, palFmt);
                    }
                }
                break;

            case GxFormat.CI8:
                for (int y = 0; y < bh; y++)
                {
                    for (int x = 0; x < bw; x++)
                    {
                        byte val = src[blockOffset + y * bw + x];
                        SetPalettePixel(rgba, width, height, bx + x, by + y, val, palette, palFmt);
                    }
                }
                break;

            case GxFormat.Rgb565:
                for (int y = 0; y < bh; y++)
                {
                    for (int x = 0; x < bw; x++)
                    {
                        ushort val = ReadBe16(src, blockOffset + y * (bw * 2) + x * 2);
                        SetPixel(rgba, width, height, bx + x, by + y, Decode565r(val), Decode565g(val), Decode565b(val), 255);
                    }
                }
                break;

            case GxFormat.Rgb5A3:
                for (int y = 0; y < bh; y++)
                {
                    for (int x = 0; x < bw; x++)
                    {
                        ushort val = ReadBe16(src, blockOffset + y * (bw * 2) + x * 2);
                        Decode5a3(val, out byte r, out byte g, out byte b, out byte a);
                        SetPixel(rgba, width, height, bx + x, by + y, r, g, b, a);
                    }
                }
                break;

            case GxFormat.Rgba8:
                for (int y = 0; y < bh; y++)
                {
                    for (int x = 0; x < bw; x++)
                    {
                        int plane0 = blockOffset + y * (bw * 2) + x * 2;
                        int plane1 = blockOffset + 32 + y * (bw * 2) + x * 2;
                        byte a = src[plane0];
                        byte r = src[plane0 + 1];
                        byte g = src[plane1];
                        byte b = src[plane1 + 1];
                        SetPixel(rgba, width, height, bx + x, by + y, r, g, b, a);
                    }
                }
                break;

            case GxFormat.Cmpr:
                DecodeCmpr(src, blockOffset, rgba, width, height, bx, by);
                break;
        }
    }

    public static byte[] EncodeMip(byte[] rgba, int width, int height, GxFormat gx, byte[] palette, uint fmt, byte[] origMip0, byte[] origRgba)
    {
        if (rgba == null || width <= 0 || height <= 0)
            return null;

        GetBlock(gx, out int bw, out int bh, out int blockBytes);
        int tilesX = (width + bw - 1) / bw;
        int tilesY = (height + bh - 1) / bh;
        byte[] outBytes = new byte[tilesX * tilesY * blockBytes];
        GxPalette palFmt = gx == GxFormat.CI8 || gx == GxFormat.CI4 ? PaletteFormat(fmt) : GxPalette.None;

        for (int by = 0; by < height; by += bh)
        {
            for (int bx = 0; bx < width; bx += bw)
            {
                int tile = (by / bh) * tilesX + (bx / bw);
                int blockOffset = tile * blockBytes;

                if (origMip0 != null && origMip0.Length >= blockOffset + blockBytes && origRgba != null && BlockUnchanged(rgba, origRgba, width, height, bx, by, bw, bh))
                {
                    Array.Copy(origMip0, blockOffset, outBytes, blockOffset, blockBytes);
                    continue;
                }

                EncodeBlock(outBytes, blockOffset, rgba, width, height, bx, by, bw, bh, gx, palette, palFmt, origMip0, origRgba);
            }
        }

        return outBytes;
    }

    private static bool BlockUnchanged(byte[] rgba, byte[] origRgba, int width, int height, int bx, int by, int bw, int bh)
    {
        for (int y = 0; y < bh; y++)
        {
            for (int x = 0; x < bw; x++)
            {
                if (!SamplesEqual(rgba, origRgba, width, height, bx + x, by + y))
                    return false;
            }
        }
        return true;
    }

    private static bool SamplesEqual(byte[] a, byte[] b, int width, int height, int x, int y)
    {
        if (x >= width || y >= height)
            return true;
        int i = (y * width + x) * 4;
        return a[i] == b[i] && a[i + 1] == b[i + 1] && a[i + 2] == b[i + 2] && a[i + 3] == b[i + 3];
    }

    private static void EncodeBlock(byte[] outBytes, int blockOffset, byte[] rgba, int width, int height, int bx, int by, int bw, int bh, GxFormat gx, byte[] palette, GxPalette palFmt, byte[] origMip0, byte[] origRgba)
    {
        for (int y = 0; y < bh; y++)
        {
            for (int x = 0; x < bw; x++)
            {
                if (bx + x >= width || by + y >= height)
                    continue;

                bool keep = origMip0 != null && origMip0.Length >= blockOffset + GetUnitBytes(gx) && origRgba != null && SamplesEqual(rgba, origRgba, width, height, bx + x, by + y);

                switch (gx)
                {
                    case GxFormat.I8:
                        {
                            int off = blockOffset + y * bw + x;
                            outBytes[off] = keep ? origMip0[off] : (byte)GetLuma(rgba, width, height, bx + x, by + y);
                        }
                        break;

                    case GxFormat.IA4:
                        {
                            int off = blockOffset + y * bw + x;
                            if (keep)
                                outBytes[off] = origMip0[off];
                            else
                            {
                                int i4 = GetLuma(rgba, width, height, bx + x, by + y) >> 4;
                                int a4 = GetAlpha(rgba, width, height, bx + x, by + y) >> 4;
                                if (i4 > 15)
                                    i4 = 15;
                                if (a4 > 15)
                                    a4 = 15;
                                outBytes[off] = (byte)((a4 << 4) | i4);
                            }
                        }
                        break;

                    case GxFormat.CI8:
                        {
                            int off = blockOffset + y * bw + x;
                            outBytes[off] = keep ? origMip0[off] : NearestPaletteIndex(rgba, width, height, bx + x, by + y, palette, palFmt);
                        }
                        break;

                    case GxFormat.IA8:
                        {
                            int off = blockOffset + y * (bw * 2) + x * 2;
                            if (keep)
                            {
                                outBytes[off] = origMip0[off];
                                outBytes[off + 1] = origMip0[off + 1];
                            }
                            else
                            {
                                outBytes[off] = (byte)GetLuma(rgba, width, height, bx + x, by + y);
                                outBytes[off + 1] = GetAlpha(rgba, width, height, bx + x, by + y);
                            }
                        }
                        break;

                    case GxFormat.Rgb565:
                        {
                            int off = blockOffset + y * (bw * 2) + x * 2;
                            if (keep)
                            {
                                outBytes[off] = origMip0[off];
                                outBytes[off + 1] = origMip0[off + 1];
                            }
                            else
                            {
                                ushort val = Encode565(GetRed(rgba, width, height, bx + x, by + y), GetGreen(rgba, width, height, bx + x, by + y), GetBlue(rgba, width, height, bx + x, by + y));
                                WriteBe16(outBytes, off, val);
                            }
                        }
                        break;

                    case GxFormat.Rgb5A3:
                        {
                            int off = blockOffset + y * (bw * 2) + x * 2;
                            if (keep)
                            {
                                outBytes[off] = origMip0[off];
                                outBytes[off + 1] = origMip0[off + 1];
                            }
                            else
                            {
                                ushort val = Encode5A3(GetRed(rgba, width, height, bx + x, by + y), GetGreen(rgba, width, height, bx + x, by + y), GetBlue(rgba, width, height, bx + x, by + y), GetAlpha(rgba, width, height, bx + x, by + y));
                                WriteBe16(outBytes, off, val);
                            }
                        }
                        break;

                    case GxFormat.Rgba8:
                        {
                            int plane0 = blockOffset + y * (bw * 2) + x * 2;
                            int plane1 = blockOffset + 32 + y * (bw * 2) + x * 2;
                            if (keep)
                            {
                                outBytes[plane0] = origMip0[plane0];
                                outBytes[plane0 + 1] = origMip0[plane0 + 1];
                                outBytes[plane1] = origMip0[plane1];
                                outBytes[plane1 + 1] = origMip0[plane1 + 1];
                            }
                            else
                            {
                                int pixelOffset = (bx + x + (by + y) * width) * 4;
                                outBytes[plane0] = rgba[pixelOffset + 3];
                                outBytes[plane0 + 1] = rgba[pixelOffset];
                                outBytes[plane1] = rgba[pixelOffset + 1];
                                outBytes[plane1 + 1] = rgba[pixelOffset + 2];
                            }
                        }
                        break;

                    case GxFormat.Cmpr:
                        break;

                    case GxFormat.I4:
                    case GxFormat.CI4:
                        {
                            int byteIndex = blockOffset + y * (bw / 2) + x / 2;
                            byte origByte = origMip0 != null && origMip0.Length > byteIndex ? origMip0[byteIndex] : (byte)0;
                            byte high = (byte)(origByte >> 4);
                            byte low = (byte)(origByte & 0x0F);

                            if (x % 2 == 0)
                            {
                                if (keep)
                                    outBytes[byteIndex] = (byte)(high << 4);
                                else
                                {
                                    int v = gx == GxFormat.I4 ? GetLuma(rgba, width, height, bx + x, by + y) >> 4 : NearestPaletteIndex(rgba, width, height, bx + x, by + y, palette, palFmt);
                                    if (v > 15)
                                        v = 15;
                                    outBytes[byteIndex] = (byte)(v << 4);
                                }
                                outBytes[byteIndex] |= low;
                            }
                            else
                            {
                                if (keep)
                                    outBytes[byteIndex] |= low;
                                else
                                {
                                    int v = gx == GxFormat.I4 ? GetLuma(rgba, width, height, bx + x, by + y) >> 4 : NearestPaletteIndex(rgba, width, height, bx + x, by + y, palette, palFmt);
                                    if (v > 15)
                                        v = 15;
                                    outBytes[byteIndex] |= (byte)v;
                                }
                                outBytes[byteIndex] |= (byte)(high << 4);
                            }
                        }
                        break;
                }
            }
        }
    }

    private static int GetUnitBytes(GxFormat gx)
    {
        switch (gx)
        {
            case GxFormat.IA8:
            case GxFormat.Rgb565:
            case GxFormat.Rgb5A3:
                return 2;
            case GxFormat.Rgba8:
                return 4;
            default:
                return 1;
        }
    }

    private static void DecodeCmpr(byte[] src, int blockOffset, byte[] rgba, int width, int height, int bx, int by)
    {
        int[] subOffsets = new[] { 0, 8, 32, 40 };
        for (int sy = 0; sy < 2; sy++)
        {
            for (int sx = 0; sx < 2; sx++)
            {
                DecodeDxt(src, blockOffset + subOffsets[sy * 2 + sx], rgba, width, height, bx + sx * 4, by + sy * 4);
            }
        }
    }

    private static void DecodeDxt(byte[] src, int offset, byte[] rgba, int width, int height, int px, int py)
    {
        ushort c1 = ReadBe16(src, offset);
        ushort c2 = ReadBe16(src, offset + 2);
        int[] colors = new int[4];
        colors[0] = ((Decode565r(c1) << 16) | (Decode565g(c1) << 8) | Decode565b(c1));
        colors[1] = ((Decode565r(c2) << 16) | (Decode565g(c2) << 8) | Decode565b(c2));
        if (c1 > c2)
        {
            colors[2] = (((2 * ((colors[0] >> 16) & 0xFF) + ((colors[1] >> 16) & 0xFF)) / 3) << 16) | (((2 * ((colors[0] >> 8) & 0xFF) + ((colors[1] >> 8) & 0xFF)) / 3) << 8) | ((2 * (colors[0] & 0xFF) + (colors[1] & 0xFF)) / 3);
            colors[3] = (((((colors[0] >> 16) & 0xFF) + 2 * ((colors[1] >> 16) & 0xFF)) / 3) << 16) | (((((colors[0] >> 8) & 0xFF) + 2 * ((colors[1] >> 8) & 0xFF)) / 3) << 8) | (((colors[0] & 0xFF) + 2 * (colors[1] & 0xFF)) / 3);
        }
        else
        {
            colors[2] = ((((colors[0] >> 16) & 0xFF) + ((colors[1] >> 16) & 0xFF)) / 2) << 16 | ((((colors[0] >> 8) & 0xFF) + ((colors[1] >> 8) & 0xFF)) / 2) << 8 | (((colors[0] & 0xFF) + (colors[1] & 0xFF)) / 2);
            colors[3] = colors[2];
        }

        for (int y = 0; y < 4; y++)
        {
            byte line = src[offset + 4 + y];
            for (int x = 0; x < 4; x++)
            {
                int ci = (line >> (6 - x * 2)) & 3;
                int px2 = px + x;
                int py2 = py + y;
                if (px2 >= width || py2 >= height)
                    continue;
                int c = colors[ci];
                byte r = (byte)(c >> 16);
                byte g = (byte)((c >> 8) & 0xFF);
                byte b = (byte)(c & 0xFF);
                byte a = (byte)255;
                if (!(c1 > c2) && ci == 3)
                    a = 0;
                SetPixel(rgba, width, height, px2, py2, r, g, b, a);
            }
        }
    }

    private static void SetPixel(byte[] rgba, int width, int height, int x, int y, byte r, byte g, byte b, byte a)
    {
        if (x < 0 || y < 0 || x >= width || y >= height)
            return;
        int i = (y * width + x) * 4;
        rgba[i] = r;
        rgba[i + 1] = g;
        rgba[i + 2] = b;
        rgba[i + 3] = a;
    }

    private static void SetPalettePixel(byte[] rgba, int width, int height, int x, int y, byte index, byte[] palette, GxPalette palFmt)
    {
        if (x < 0 || y < 0 || x >= width || y >= height)
            return;
        DecodePalette(index, palette, palFmt, out byte r, out byte g, out byte b, out byte a);
        int i = (y * width + x) * 4;
        rgba[i] = r;
        rgba[i + 1] = g;
        rgba[i + 2] = b;
        rgba[i + 3] = a;
    }

    private static void DecodePalette(byte index, byte[] palette, GxPalette palFmt, out byte r, out byte g, out byte b, out byte a)
    {
        r = 0;
        g = 0;
        b = 0;
        a = 0;
        if (palette == null)
            return;
        int wordOffset = index * 2;
        if (wordOffset + 1 >= palette.Length)
            return;
        ushort val = (ushort)((palette[wordOffset] << 8) | palette[wordOffset + 1]);

        if (palFmt == GxPalette.Rgb565)
        {
            r = Decode565r(val);
            g = Decode565g(val);
            b = Decode565b(val);
            a = 255;
        }
        else
        {
            a = (byte)(((val >> 12) & 0x0F) * 17);
            r = (byte)(((val >> 8) & 0x0F) * 17);
            g = (byte)(((val >> 4) & 0x0F) * 17);
            b = (byte)((val & 0x0F) * 17);
        }
    }

    private static byte NearestPaletteIndex(byte[] rgba, int width, int height, int x, int y, byte[] palette, GxPalette palFmt)
    {
        int pixelOffset = (y * width + x) * 4;
        byte r = rgba[pixelOffset];
        byte g = rgba[pixelOffset + 1];
        byte b = rgba[pixelOffset + 2];
        byte a = rgba[pixelOffset + 3];
        int best = 0;
        int bestDist = int.MaxValue;

        if (palette != null)
        {
            int count = palette.Length / 2;
            for (int i = 0; i < count; i++)
            {
                DecodePalette((byte)i, palette, palFmt, out byte pr, out byte pg, out byte pb, out byte pa);
                int dr = r - pr;
                int dg = g - pg;
                int db = b - pb;
                int da = a - pa;
                int dist = dr * dr + dg * dg + db * db + da * da;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = i;
                }
            }
        }

        return (byte)best;
    }

    private static int GetLuma(byte[] rgba, int width, int height, int x, int y)
    {
        int i = (y * width + x) * 4;
        return (rgba[i] * 299 + rgba[i + 1] * 587 + rgba[i + 2] * 114) / 1000;
    }

    private static byte GetRed(byte[] rgba, int width, int height, int x, int y)
    {
        return rgba[(y * width + x) * 4];
    }

    private static byte GetGreen(byte[] rgba, int width, int height, int x, int y)
    {
        return rgba[(y * width + x) * 4 + 1];
    }

    private static byte GetBlue(byte[] rgba, int width, int height, int x, int y)
    {
        return rgba[(y * width + x) * 4 + 2];
    }

    private static byte GetAlpha(byte[] rgba, int width, int height, int x, int y)
    {
        return rgba[(y * width + x) * 4 + 3];
    }

    private static byte Decode565r(ushort val)
    {
        return (byte)(((val >> 11) & 0x1F) * 255 / 31);
    }

    private static byte Decode565g(ushort val)
    {
        return (byte)(((val >> 5) & 0x3F) * 255 / 63);
    }

    private static byte Decode565b(ushort val)
    {
        return (byte)((val & 0x1F) * 255 / 31);
    }

    private static ushort Encode565(byte r, byte g, byte b)
    {
        int r5 = (r * 31 + 127) / 255;
        int g6 = (g * 63 + 127) / 255;
        int b5 = (b * 31 + 127) / 255;
        return (ushort)((r5 << 11) | (g6 << 5) | b5);
    }

    private static void Decode5a3(ushort val, out byte r, out byte g, out byte b, out byte a)
    {
        if ((val & 0x8000) != 0)
        {
            r = (byte)(((val >> 10) & 0x1F) * 255 / 31);
            g = (byte)(((val >> 5) & 0x1F) * 255 / 31);
            b = (byte)((val & 0x1F) * 255 / 31);
            a = 255;
        }
        else
        {
            a = (byte)(((val >> 12) & 0x07) * 255 / 7);
            r = (byte)(((val >> 8) & 0x0F) * 255 / 15);
            g = (byte)(((val >> 4) & 0x0F) * 255 / 15);
            b = (byte)((val & 0x0F) * 255 / 15);
        }
    }

    private static ushort Encode5A3(byte r, byte g, byte b, byte a)
    {
        if (a >= 255)
        {
            int r5 = (r * 31 + 127) / 255;
            int g5 = (g * 31 + 127) / 255;
            int b5 = (b * 31 + 127) / 255;
            return (ushort)(0x8000 | (r5 << 10) | (g5 << 5) | b5);
        }

        int a3 = (a * 7 + 127) / 255;
        int r4 = (r * 15 + 127) / 255;
        int g4 = (g * 15 + 127) / 255;
        int b4 = (b * 15 + 127) / 255;
        return (ushort)((a3 << 12) | (r4 << 8) | (g4 << 4) | b4);
    }

    private static ushort ReadBe16(byte[] data, int offset)
    {
        return (ushort)((data[offset] << 8) | data[offset + 1]);
    }

    private static void WriteBe16(byte[] data, int offset, ushort value)
    {
        data[offset] = (byte)(value >> 8);
        data[offset + 1] = (byte)(value & 0xFF);
    }
}

public class PngImage
{
    public int Width { get; set; }
    public int Height { get; set; }
    public byte[] Rgba { get; set; }
}

public static class PngCodec
{
    private static readonly uint[] CrcTable = BuildCrcTable();

    public static byte[] Encode(int width, int height, byte[] rgba)
    {
        if (width <= 0 || height <= 0)
            return null;

        using (var ms = new MemoryStream())
        {
            byte[] signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            ms.Write(signature, 0, signature.Length);

            WriteChunk(ms, "IHDR", BuildIhdr(width, height));

            byte[] idat = BuildIdat(width, height, rgba);
            WriteChunk(ms, "IDAT", idat);

            WriteChunk(ms, "IEND", Array.Empty<byte>());

            return ms.ToArray();
        }
    }

    public static PngImage Decode(byte[] png)
    {
        if (png == null || png.Length < 33)
            throw new InvalidDataException("PNG too short");

        for (int i = 0; i < 8; i++)
        {
            if (png[i] != new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }[i])
                throw new InvalidDataException("PNG signature mismatch");
        }

        int pos = 8;
        int width = 0;
        int height = 0;
        int bitDepth = 0;
        int colorType = 0;
        bool interlace = false;
        byte[] palette = null;
        byte[] trns = null;
        byte[] idat = null;

        while (pos < png.Length)
        {
            if (pos + 8 > png.Length)
                break;
            int len = ReadBeInt32(png, pos);
            string type = System.Text.Encoding.ASCII.GetString(png, pos + 4, 4);
            int dataStart = pos + 8;
            int dataEnd = dataStart + len;
            if (dataEnd + 4 > png.Length)
                break;

            if (type == "IHDR")
            {
                width = ReadBeInt32(png, dataStart);
                height = ReadBeInt32(png, dataStart + 4);
                bitDepth = png[dataStart + 8];
                colorType = png[dataStart + 9];
                interlace = png[dataStart + 12] != 0;
            }
            else if (type == "PLTE")
            {
                palette = new byte[len];
                Array.Copy(png, dataStart, palette, 0, len);
            }
            else if (type == "tRNS")
            {
                trns = new byte[len];
                Array.Copy(png, dataStart, trns, 0, len);
            }
            else if (type == "IDAT")
            {
                if (idat == null)
                    idat = new byte[len];
                else
                    Array.Resize(ref idat, idat.Length + len);
                Array.Copy(png, dataStart, idat, idat.Length - len, len);
            }
            else if (type == "IEND")
            {
                break;
            }

            pos = dataEnd + 4;
        }

        if (width <= 0 || height <= 0 || bitDepth == 0)
            throw new InvalidDataException("PNG missing IHDR");
        if (interlace)
            throw new InvalidDataException("PNG interlace unsupported");

        byte[] raw = InflateIdat(idat);
        byte[] rgba = Unfilter(raw, width, height, bitDepth, colorType, palette, trns);

        return new PngImage { Width = width, Height = height, Rgba = rgba };
    }

    private static byte[] BuildIhdr(int width, int height)
    {
        byte[] ihdr = new byte[13];
        WriteBeInt32(ihdr, 0, width);
        WriteBeInt32(ihdr, 4, height);
        ihdr[8] = 8;
        ihdr[9] = 6;
        ihdr[10] = 0;
        ihdr[11] = 0;
        ihdr[12] = 0;
        return ihdr;
    }

    private static byte[] BuildIdat(int width, int height, byte[] rgba)
    {
        int stride = width * 4;
        byte[] scanlines = new byte[(stride + 1) * height];
        for (int y = 0; y < height; y++)
        {
            int rowStart = y * (stride + 1);
            scanlines[rowStart] = 0;
            Array.Copy(rgba, y * stride, scanlines, rowStart + 1, stride);
        }

        byte[] deflated;
        using (var ms = new MemoryStream())
        {
            using (var ds = new DeflateStream(ms, CompressionLevel.Optimal, true))
            {
                ds.Write(scanlines, 0, scanlines.Length);
            }
            deflated = ms.ToArray();
        }

        byte[] zlib = new byte[deflated.Length + 6];
        zlib[0] = 0x78;
        zlib[1] = 0x9C;
        Array.Copy(deflated, 0, zlib, 2, deflated.Length);
        uint adler = Adler32(scanlines);
        WriteBeInt32(zlib, zlib.Length - 4, unchecked((int)adler));

        return zlib;
    }

    private static byte[] InflateIdat(byte[] idat)
    {
        if (idat == null || idat.Length < 6)
            throw new InvalidDataException("PNG missing IDAT");

        int start = 0;
        if ((idat[0] & 0x0F) == 8)
            start = 2;

        using (var src = new MemoryStream(idat, start, idat.Length - start - 4))
        using (var inflater = new DeflateStream(src, CompressionMode.Decompress))
        using (var dst = new MemoryStream())
        {
            inflater.CopyTo(dst);
            return dst.ToArray();
        }
    }

    private static byte[] Unfilter(byte[] raw, int width, int height, int bitDepth, int colorType, byte[] palette, byte[] trns)
    {
        int channels = colorType == 0 ? 1 : colorType == 2 ? 3 : colorType == 3 ? 1 : colorType == 4 ? 2 : 4;
        int bitsPerPixel = channels * bitDepth;
        int bytesPerRow = (bitsPerPixel * width + 7) / 8;
        int stride = bytesPerRow + 1;

        if (raw.Length < stride * height)
            throw new InvalidDataException("PNG scanline data too short");

        byte[] unfiltered = new byte[bytesPerRow * height];
        for (int y = 0; y < height; y++)
        {
            byte filter = raw[y * stride];
            byte[] row = unfiltered.AsSpan(y * bytesPerRow, bytesPerRow).ToArray();
            byte[] prev = y == 0 ? new byte[bytesPerRow] : unfiltered.AsSpan((y - 1) * bytesPerRow, bytesPerRow).ToArray();
            byte[] cur = raw.AsSpan(y * stride + 1, bytesPerRow).ToArray();

            int bpp = Math.Max(1, bitsPerPixel / 8);

            for (int x = 0; x < bytesPerRow; x++)
            {
                byte a = x >= bpp ? cur[x - bpp] : (byte)0;
                byte b = prev[x];
                byte c = x >= bpp ? prev[x - bpp] : (byte)0;
                int val = cur[x];
                switch (filter)
                {
                    case 0:
                        break;
                    case 1:
                        val += a;
                        break;
                    case 2:
                        val += b;
                        break;
                    case 3:
                        val += (a + b) / 2;
                        break;
                    case 4:
                    {
                        int p = a + b - c;
                        int pa = Math.Abs(p - a);
                        int pb = Math.Abs(p - b);
                        int pc = Math.Abs(p - c);
                        val += (pa <= pb && pa <= pc) ? a : (pb <= pc ? b : c);
                        break;
                    }
                    default:
                        throw new InvalidDataException("PNG unknown filter");
                }
                row[x] = (byte)(val & 0xFF);
            }

            Array.Copy(row, 0, unfiltered, y * bytesPerRow, bytesPerRow);
        }

        return ToRgba(unfiltered, width, height, bitDepth, colorType, channels, bitsPerPixel, palette, trns);
    }

    private static byte[] ToRgba(byte[] data, int width, int height, int bitDepth, int colorType, int channels, int bitsPerPixel, byte[] palette, byte[] trns)
    {
        byte[] rgba = new byte[width * height * 4];
        int rowBytes = (bitsPerPixel * width + 7) / 8;
        int samplesPerByte = 8 / bitDepth;

        for (int y = 0; y < height; y++)
        {
            int rowBase = y * rowBytes;
            for (int x = 0; x < width; x++)
            {
                int pixelOffset = (y * width + x) * 4;

                if (bitDepth == 8)
                {
                    int srcBase = rowBase + x * channels;
                    if (colorType == 0)
                    {
                        byte v = data[srcBase];
                        rgba[pixelOffset] = v;
                        rgba[pixelOffset + 1] = v;
                        rgba[pixelOffset + 2] = v;
                        rgba[pixelOffset + 3] = trns != null && trns.Length > 0 && trns[0] == v ? (byte)0 : (byte)255;
                    }
                    else if (colorType == 2)
                    {
                        rgba[pixelOffset] = data[srcBase];
                        rgba[pixelOffset + 1] = data[srcBase + 1];
                        rgba[pixelOffset + 2] = data[srcBase + 2];
                        if (trns != null && trns.Length >= 6 && rgba[pixelOffset] == trns[0] && rgba[pixelOffset + 1] == trns[1] && rgba[pixelOffset + 2] == trns[2])
                            rgba[pixelOffset + 3] = 0;
                        else
                            rgba[pixelOffset + 3] = 255;
                    }
                    else if (colorType == 3)
                    {
                        byte index = data[srcBase];
                        DecodePaletteEntry(index, palette, trns, rgba, pixelOffset);
                    }
                    else if (colorType == 4)
                    {
                        byte v = data[srcBase];
                        rgba[pixelOffset] = v;
                        rgba[pixelOffset + 1] = v;
                        rgba[pixelOffset + 2] = v;
                        rgba[pixelOffset + 3] = data[srcBase + 1];
                    }
                    else if (colorType == 6)
                    {
                        rgba[pixelOffset] = data[srcBase];
                        rgba[pixelOffset + 1] = data[srcBase + 1];
                        rgba[pixelOffset + 2] = data[srcBase + 2];
                        rgba[pixelOffset + 3] = data[srcBase + 3];
                    }
                }
                else
                {
                    int bitIndex = x * bitsPerPixel;
                    int byteIndex = rowBase + bitIndex / 8;
                    int shift = 8 - bitDepth - (bitIndex % 8);
                    int sample = (data[byteIndex] >> shift) & ((1 << bitDepth) - 1);
                    int expanded = sample * (255 / ((1 << bitDepth) - 1));

                    if (colorType == 0)
                    {
                        rgba[pixelOffset] = (byte)expanded;
                        rgba[pixelOffset + 1] = (byte)expanded;
                        rgba[pixelOffset + 2] = (byte)expanded;
                        rgba[pixelOffset + 3] = 255;
                    }
                    else if (colorType == 3)
                    {
                        DecodePaletteEntry((byte)sample, palette, trns, rgba, pixelOffset);
                    }
                }
            }
        }

        return rgba;
    }

    private static void DecodePaletteEntry(byte index, byte[] palette, byte[] trns, byte[] rgba, int pixelOffset)
    {
        if (palette == null || index * 3 + 2 >= palette.Length)
        {
            rgba[pixelOffset] = 0;
            rgba[pixelOffset + 1] = 0;
            rgba[pixelOffset + 2] = 0;
            rgba[pixelOffset + 3] = 255;
            return;
        }

        rgba[pixelOffset] = palette[index * 3];
        rgba[pixelOffset + 1] = palette[index * 3 + 1];
        rgba[pixelOffset + 2] = palette[index * 3 + 2];
        rgba[pixelOffset + 3] = trns != null && index < trns.Length ? trns[index] : (byte)255;
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        byte[] typeBytes = Encoding.ASCII.GetBytes(type);
        byte[] whole = new byte[4 + data.Length];
        Array.Copy(typeBytes, 0, whole, 0, 4);
        Array.Copy(data, 0, whole, 4, data.Length);

        WriteBeInt32(stream, data.Length);
        stream.Write(whole, 0, whole.Length);
        WriteBeInt32(stream, unchecked((int)Crc32(whole)));
    }

    private static uint Crc32(byte[] data)
    {
        uint crc = 0xFFFFFFFF;
        for (int i = 0; i < data.Length; i++)
            crc = CrcTable[(crc ^ data[i]) & 0xFF] ^ (crc >> 8);
        return crc ^ 0xFFFFFFFF;
    }

    private static uint[] BuildCrcTable()
    {
        uint[] table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint c = i;
            for (int k = 0; k < 8; k++)
            {
                c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
            }
            table[i] = c;
        }
        return table;
    }

    private static uint Adler32(byte[] data)
    {
        uint a = 1;
        uint b = 0;
        for (int i = 0; i < data.Length; i++)
        {
            a = (a + data[i]) % 65521;
            b = (b + a) % 65521;
        }
        return (b << 16) | a;
    }

    private static int ReadBeInt32(byte[] data, int offset)
    {
        return (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
    }

    private static void WriteBeInt32(byte[] data, int offset, int value)
    {
        data[offset] = (byte)(value >> 24);
        data[offset + 1] = (byte)(value >> 16);
        data[offset + 2] = (byte)(value >> 8);
        data[offset + 3] = (byte)value;
    }

    private static void WriteBeInt32(Stream stream, int value)
    {
        stream.WriteByte((byte)(value >> 24));
        stream.WriteByte((byte)(value >> 16));
        stream.WriteByte((byte)(value >> 8));
        stream.WriteByte((byte)value);
    }
}