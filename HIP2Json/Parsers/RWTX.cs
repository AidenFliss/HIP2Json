using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace HIP2Json;

public sealed class RWTXParser : AssetParser
{
    private const int HeaderOffset = 0x34;
    private const int PixelOffset = 0xA0;
    private const int BlobPrefixedSpan = 0x70;
    private const uint RWTXGcPlatform = 6;
    private const uint RWTXXboxPlatform = 5;
    private const int XboxPixelOffset = 0x90;
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
        {
            return new RWTX
            {
                platformType = data.Length >= HeaderOffset + 4 ? Util.ReadUInt32(data, PlatformTypeOffset) : 0,
                rwVersion = data.Length >= 0x0C ? ReadLe32(data, 0x08) : 0,
                filterAndAddress = data.Length >= HeaderOffset + 8 ? Util.ReadUInt32(data, FilterAddressOffset) : 0,
            };
        }

        uint rwVersion = ReadLe32(data, 0x08);
        ushort textureCount = ReadLe16(data, 0x18);
        ushort dictUnknown = ReadLe16(data, 0x1A);

        uint platformType = Util.ReadUInt32(data, PlatformTypeOffset);
        bool gcLayout = platformType == RWTXGcPlatform;

        if (platformType == RWTXXboxPlatform)
        {
            return ParseXbox(data, rwVersion, textureCount, dictUnknown);
        }

        if (!gcLayout)
        {
            ushort ps2Width = 0;
            ushort ps2Height = 0;
            byte ps2BitDepth = 0;
            byte ps2Mips = 0;
            string ps2Format = null;
            string ps2PngBase64 = null;
            string preMipBase64 = null;
            string ps2TailBase64 = null;

            if (platformType == RwtxPs2.Platform)
            {
                ps2Width = (ushort)RwtxPs2.ReadLe32(data, RwtxPs2.WidthOffset);
                ps2Height = (ushort)RwtxPs2.ReadLe32(data, RwtxPs2.HeightOffset);
                ps2BitDepth = data[RwtxPs2.BitDepthOffset];

                if (ps2Width > 0 && ps2Height > 0 && TryDecodeMip0(data.AsSpan(PixelOffset).ToArray(), ps2Width, ps2Height, ps2BitDepth, out byte[] ps2Rgba, out byte[] _, out int ps2MipCount))
                {
                    byte[] png = PngCodec.Encode(ps2Width, ps2Height, ps2Rgba);
                    if (png != null)
                        ps2PngBase64 = Convert.ToBase64String(png);

                    ps2Format = ps2BitDepth switch
                    {
                        4 => "PS2 PSMT4 (RGBA palette)",
                        8 => "PS2 PSMT8 (RGBA palette)",
                        32 => "PS2 PSMCT32 (RGBA)",
                        _ => null,
                    };
                    ps2Mips = (byte)ps2MipCount;
                }

                int ps2Span = RwtxPs2.Mip0Bytes(ps2Width, ps2Height, ps2BitDepth);
                byte[] ps2Payload = data.AsSpan(PixelOffset).ToArray();
                if (ps2Span > 0 && ps2Payload.Length >= BlobPrefixedSpan + ps2Span)
                {
                    preMipBase64 = Convert.ToBase64String(ps2Payload, 0, BlobPrefixedSpan);
                    ps2TailBase64 = Convert.ToBase64String(ps2Payload, BlobPrefixedSpan + ps2Span, ps2Payload.Length - BlobPrefixedSpan - ps2Span);
                }
            }

            uint[] gsRegisters = new uint[8];
            for (int i = 0; i < 8; i++)
                gsRegisters[i] = RwtxPs2.ReadLe32(data, 0x80 + i * 4);

            return new RWTX
            {
                platformType = platformType,
                rwVersion = rwVersion,
                filterAndAddress = RwtxPs2.ReadLe32(data, FilterAddressOffset),
                width = ps2Width,
                height = ps2Height,
                bitDepth = ps2BitDepth,
                mipMapCount = ps2Mips,
                textureCount = textureCount,
                dictUnknown = dictUnknown,
                ps2DataSize = RwtxPs2.ReadLe32(data, 0x60),
                ps2Stride = RwtxPs2.ReadLe32(data, 0x6C),
                gsRegisters = gsRegisters,
                preMipBase64 = preMipBase64,
                tailBase64 = ps2TailBase64,
                format = ps2Format,
                pngBase64 = ps2PngBase64,
            };
        }

        ushort width = Util.ReadUInt16(data, WidthOffset);
        ushort height = Util.ReadUInt16(data, HeightOffset);
        byte bitDepth = data[BitDepthOffset];
        uint fmt = Util.ReadUInt32(data, RasterFormatOffset);

        byte[] payload = data.AsSpan(PixelOffset).ToArray();

        string format = null;
        string pngBase64 = null;
        string paletteBase64 = null;
        string gcTailBase64 = null;

        byte rasterType = data[RasterTypeOffset];

        GxFormat gx = RwtxGx.Detect(bitDepth, fmt, rasterType);
        if (gx != GxFormat.None && width > 0 && height > 0 && TryDecodeMip0(payload, gx, fmt, width, height, out byte[] rgba, out byte[] _))
        {
            byte[] png = PngCodec.Encode(width, height, rgba);
            if (png != null)
                pngBase64 = Convert.ToBase64String(png);

            format = RwtxGx.FormatName(gx, fmt);
        }

        if (gx != GxFormat.None && width > 0 && height > 0)
        {
            int paletteSize = RwtxGx.PaletteSize(gx);
            int mip0Bytes = RwtxGx.MipBytes(gx, width, height);
            int spanEnd = paletteSize + mip0Bytes;
            if (mip0Bytes > 0 && spanEnd <= payload.Length)
            {
                if (paletteSize > 0)
                    paletteBase64 = Convert.ToBase64String(payload, 0, paletteSize);
                gcTailBase64 = Convert.ToBase64String(payload, spanEnd, payload.Length - spanEnd);
            }
        }

        uint[] gcnUnknown = new uint[4];
        for (int i = 0; i < 4; i++)
            gcnUnknown[i] = Util.ReadUInt32(data, 0x3C + i * 4);

        return new RWTX
        {
            platformType = Util.ReadUInt32(data, PlatformTypeOffset),
            rwVersion = rwVersion,
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
            textureCount = textureCount,
            dictUnknown = dictUnknown,
            gcnUnknown = gcnUnknown,
            gcnReserved = Util.ReadUInt32(data, 0x9C),
            paletteBase64 = paletteBase64,
            tailBase64 = gcTailBase64,
            format = format,
            pngBase64 = pngBase64,
        };
    }

    private static RWTX ParseXbox(byte[] data, uint rwVersion, ushort textureCount, ushort dictUnknown)
    {
        uint rasterFormatFlags = ReadLe32(data, 0x7C);
        ushort width = ReadLe16(data, 0x84);
        ushort height = ReadLe16(data, 0x86);
        byte bitDepth = data[0x88];
        byte mipCount = data[0x89];
        byte rasterType = data[0x8A];
        byte compression = data[0x8B];
        uint totalSize = ReadLe32(data, 0x8C);

        string format = null;
        string pngBase64 = null;
        byte[] payload = data.Length > XboxPixelOffset ? data.AsSpan(XboxPixelOffset).ToArray() : Array.Empty<byte>();

        if (width > 0 && height > 0 && TryDecodeXboxMip0(payload, width, height, bitDepth, rasterFormatFlags, compression, out byte[] xbRgba, out byte[] _))
        {
            byte[] png = PngCodec.Encode(width, height, xbRgba);
            if (png != null)
                pngBase64 = Convert.ToBase64String(png);

            format = RwtxXbox.FormatName(rasterFormatFlags, compression);
        }

        int span = RwtxXbox.Mip0Bytes(width, height, bitDepth, compression);
        string preMipBase64 = null;
        string xbTailBase64 = null;
        if (span > 0 && payload.Length >= span)
        {
            preMipBase64 = Convert.ToBase64String(payload, 0, span);
            if (payload.Length > span)
            {
                byte[] tail = new byte[payload.Length - span];
                Array.Copy(payload, span, tail, 0, tail.Length);
                xbTailBase64 = Convert.ToBase64String(tail);
            }
        }

        return new RWTX
        {
            platformType = RWTXXboxPlatform,
            rwVersion = rwVersion,
            filterAndAddress = ReadLe32(data, FilterAddressOffset),
            textureName = ReadName(data, 0x3C),
            alphaName = ReadName(data, 0x5C),
            rasterFormatFlags = rasterFormatFlags,
            width = width,
            height = height,
            bitDepth = bitDepth,
            mipMapCount = mipCount,
            rasterType = rasterType,
            compression = compression,
            transparency = 0,
            textureCount = textureCount,
            dictUnknown = dictUnknown,
            totalSize = totalSize,
            preMipBase64 = preMipBase64,
            tailBase64 = xbTailBase64,
            format = format,
            pngBase64 = pngBase64,
        };
    }

    private static bool TryDecodeXboxMip0(byte[] payload, int width, int height, byte bitDepth, uint rasterFormatFlags, byte compression, out byte[] rgba, out byte[] mip0)
    {
        rgba = null;
        mip0 = null;
        try
        {
            if (!RwtxXbox.TryDecodeMip0(payload, width, height, bitDepth, rasterFormatFlags, compression, out rgba, out mip0))
                return false;
            return rgba != null && mip0 != null;
        }
        catch
        {
            return false;
        }
    }

    public override object Serialize(object obj)
    {
        RWTX rwtx = (RWTX)obj;

        bool gc = rwtx.platformType == 6;
        bool xbox = rwtx.platformType == RWTXXboxPlatform;

        byte[] payload = gc ? SerializeGameCubePayload(rwtx) : xbox ? SerializeXboxPayload(rwtx) : SerializePs2Payload(rwtx);

        byte[] prefix = BuildPrefix(rwtx, gc, payload.Length);

        using var ms = new MemoryStream(prefix.Length + payload.Length);
        ms.Write(prefix, 0, prefix.Length);
        ms.Write(payload, 0, payload.Length);
        return ms.ToArray();
    }

    private static byte[] SerializeGameCubePayload(RWTX rwtx)
    {
        GxFormat gx = RwtxGx.Detect(rwtx.bitDepth, rwtx.rasterFormatFlags, rwtx.rasterType);
        int paletteSize = RwtxGx.PaletteSize(gx);
        int mip0Bytes = gx != GxFormat.None && rwtx.width > 0 && rwtx.height > 0 ? RwtxGx.MipBytes(gx, rwtx.width, rwtx.height) : 0;

        byte[] palette = !string.IsNullOrEmpty(rwtx.paletteBase64) ? Convert.FromBase64String(rwtx.paletteBase64) : Array.Empty<byte>();
        byte[] tail = !string.IsNullOrEmpty(rwtx.tailBase64) ? Convert.FromBase64String(rwtx.tailBase64) : Array.Empty<byte>();

        byte[] skeleton = new byte[palette.Length + mip0Bytes + tail.Length];
        Array.Copy(palette, 0, skeleton, 0, palette.Length);
        if (tail.Length > 0)
            Array.Copy(tail, 0, skeleton, palette.Length + mip0Bytes, tail.Length);

        if (mip0Bytes > 0 && !string.IsNullOrEmpty(rwtx.pngBase64))
        {
            try
            {
                PngImage png = PngCodec.Decode(Convert.FromBase64String(rwtx.pngBase64));
                if (png.Width == rwtx.width && png.Height == rwtx.height && gx != GxFormat.None)
                {
                    byte[] reencoded = RwtxGx.EncodeMip0Pixels(skeleton, gx, rwtx.rasterFormatFlags, rwtx.width, rwtx.height, png.Rgba, null, null);
                    if (reencoded != null && reencoded.Length == skeleton.Length)
                        skeleton = reencoded;
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning("RWTX png re-encode skipped: " + ex.Message);
            }
        }

        return skeleton;
    }

    private static byte[] SerializePs2Payload(RWTX rwtx)
    {
        int span = RwtxPs2.Mip0Bytes(rwtx.width, rwtx.height, rwtx.bitDepth);

        byte[] pre = !string.IsNullOrEmpty(rwtx.preMipBase64) ? Convert.FromBase64String(rwtx.preMipBase64) : Array.Empty<byte>();
        byte[] tail = !string.IsNullOrEmpty(rwtx.tailBase64) ? Convert.FromBase64String(rwtx.tailBase64) : Array.Empty<byte>();

        byte[] payload = new byte[pre.Length + span + tail.Length];
        Array.Copy(pre, 0, payload, 0, pre.Length);
        if (tail.Length > 0)
            Array.Copy(tail, 0, payload, pre.Length + span, tail.Length);

        if (span > 0 && !string.IsNullOrEmpty(rwtx.pngBase64))
        {
            try
            {
                PngImage png = PngCodec.Decode(Convert.FromBase64String(rwtx.pngBase64));
                if (png.Width == rwtx.width && png.Height == rwtx.height)
                {
                    byte[] reencoded = RwtxPs2.EncodeMip0(payload, rwtx.width, rwtx.height, rwtx.bitDepth, png.Rgba);
                    if (reencoded != null && reencoded.Length == payload.Length)
                        payload = reencoded;
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning("RWTX png re-encode skipped: " + ex.Message);
            }
        }

        return payload;
    }

    private static byte[] SerializeXboxPayload(RWTX rwtx)
    {
        int span = RwtxXbox.Mip0Bytes(rwtx.width, rwtx.height, rwtx.bitDepth, rwtx.compression);

        byte[] pre = !string.IsNullOrEmpty(rwtx.preMipBase64) ? Convert.FromBase64String(rwtx.preMipBase64) : Array.Empty<byte>();
        byte[] tail = !string.IsNullOrEmpty(rwtx.tailBase64) ? Convert.FromBase64String(rwtx.tailBase64) : Array.Empty<byte>();

        byte[] payload = new byte[pre.Length + tail.Length];
        Array.Copy(pre, 0, payload, 0, pre.Length);
        if (tail.Length > 0)
            Array.Copy(tail, 0, payload, pre.Length, tail.Length);

        if (rwtx.compression == 0 && span > 0 && pre.Length == span && !string.IsNullOrEmpty(rwtx.pngBase64))
        {
            try
            {
                PngImage png = PngCodec.Decode(Convert.FromBase64String(rwtx.pngBase64));
                if (png.Width == rwtx.width && png.Height == rwtx.height)
                {
                    byte[] reencoded = RwtxXbox.EncodeMip0(payload, span, rwtx.width, rwtx.height, rwtx.bitDepth, rwtx.rasterFormatFlags, png.Rgba);
                    if (reencoded != null && reencoded.Length == payload.Length)
                        payload = reencoded;
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning("RWTX png re-encode skipped: " + ex.Message);
            }
        }

        return payload;
    }

    private static byte[] BuildPrefix(RWTX rwtx, bool gc, int payloadLength)
    {
        bool xbox = rwtx.platformType == RWTXXboxPlatform;
        int offset = xbox ? XboxPixelOffset : PixelOffset;
        long total = offset + (long)payloadLength;
        byte[] p = new byte[offset];

        uint ver = rwtx.rwVersion != 0 ? rwtx.rwVersion : gc ? 0x1C02000Au : 0x1400FFFFu;

        WriteLe32(p, 0x00, 0x16);
        WriteLe32(p, 0x04, (uint)(total - 12));
        WriteLe32(p, 0x08, ver);
        WriteLe32(p, 0x0C, 0x01);
        WriteLe32(p, 0x10, 4);
        WriteLe32(p, 0x14, ver);
        WriteLe16(p, 0x18, rwtx.textureCount);
        WriteLe16(p, 0x1A, rwtx.dictUnknown);
        WriteLe32(p, 0x1C, 0x15);
        WriteLe32(p, 0x20, (uint)(total - 52));
        WriteLe32(p, 0x24, ver);
        WriteLe32(p, 0x28, 0x01);
        WriteLe32(p, 0x2C, gc ? (uint)(total - 76) : xbox ? (uint)(total - 76) : 8u);
        WriteLe32(p, 0x30, ver);

        if (gc)
        {
            WriteUInt32(p, 0x34, RWTXGcPlatform);
            WriteUInt32(p, 0x38, rwtx.filterAndAddress);
            for (int i = 0; i < 4; i++)
                WriteUInt32(p, 0x3C + i * 4, rwtx.gcnUnknown != null && i < rwtx.gcnUnknown.Length ? rwtx.gcnUnknown[i] : 0u);
            WriteString32(p, 0x4C, rwtx.textureName);
            WriteString32(p, 0x6C, rwtx.alphaName);
            WriteUInt32(p, 0x8C, rwtx.rasterFormatFlags);
            WriteUInt16(p, 0x90, rwtx.width);
            WriteUInt16(p, 0x92, rwtx.height);
            p[0x94] = rwtx.bitDepth;
            p[0x95] = rwtx.mipMapCount;
            p[0x96] = rwtx.rasterType;
            p[0x97] = rwtx.compression;
            p[0x98] = 0;
            p[0x99] = 0;
            p[0x9A] = 0;
            p[0x9B] = rwtx.transparency;
            WriteUInt32(p, 0x9C, rwtx.gcnReserved ?? 0u);
        }
        else if (xbox)
        {
            WriteLe32(p, 0x34, RWTXXboxPlatform);
            WriteLe32(p, 0x38, rwtx.filterAndAddress);
            WriteString32(p, 0x3C, rwtx.textureName);
            WriteString32(p, 0x5C, rwtx.alphaName);
            WriteLe32(p, 0x7C, rwtx.rasterFormatFlags);
            WriteLe16(p, 0x80, (ushort)(RwtxXbox.HasAlpha(rwtx.rasterFormatFlags) ? 1 : 0));
            WriteLe16(p, 0x82, 0);
            WriteLe16(p, 0x84, rwtx.width);
            WriteLe16(p, 0x86, rwtx.height);
            p[0x88] = rwtx.bitDepth;
            p[0x89] = rwtx.mipMapCount;
            p[0x8A] = rwtx.rasterType;
            p[0x8B] = rwtx.compression;
            WriteLe32(p, 0x8C, rwtx.totalSize != 0 ? rwtx.totalSize : (uint)payloadLength);
        }
        else
        {
            WriteLe32(p, 0x34, RwtxPs2.Platform);
            WriteLe32(p, 0x38, rwtx.filterAndAddress);
            WriteLe32(p, 0x3C, 2);
            WriteLe32(p, 0x40, 4);
            WriteLe32(p, 0x44, ver);
            WriteLe32(p, 0x48, 0xDDDDDD00u);
            WriteLe32(p, 0x4C, 2);
            WriteLe32(p, 0x50, 4);
            WriteLe32(p, 0x54, ver);
            WriteLe32(p, 0x58, 0xDDDDDD00u);
            WriteLe32(p, 0x5C, 1);
            WriteLe32(p, 0x60, rwtx.ps2DataSize ?? 0u);
            WriteLe32(p, 0x64, ver);
            WriteLe32(p, 0x68, 1);
            WriteLe32(p, 0x6C, rwtx.ps2Stride ?? 0u);
            WriteLe32(p, 0x70, ver);
            WriteLe32(p, 0x74, rwtx.width);
            WriteLe32(p, 0x78, rwtx.height);
            WriteLe32(p, 0x7C, rwtx.bitDepth);
            for (int i = 0; i < 8; i++)
                WriteLe32(p, 0x80 + i * 4, rwtx.gsRegisters != null && i < rwtx.gsRegisters.Length ? rwtx.gsRegisters[i] : 0u);
        }

        return p;
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

    private static bool TryDecodeMip0(byte[] payload, int width, int height, byte bitDepth, out byte[] rgba, out byte[] mip0, out int mips)
    {
        rgba = null;
        mip0 = null;
        mips = 0;
        try
        {
            if (!RwtxPs2.TryDecodeMip0(payload, width, height, bitDepth, out rgba, out mip0, out mips))
                return false;
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

    private static uint ReadLe32(byte[] data, int offset)
    {
        return (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
    }

    private static ushort ReadLe16(byte[] data, int offset)
    {
        return (ushort)(data[offset] | (data[offset + 1] << 8));
    }

    private static void WriteLe32(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)(value & 0xFF);
        data[offset + 1] = (byte)((value >> 8) & 0xFF);
        data[offset + 2] = (byte)((value >> 16) & 0xFF);
        data[offset + 3] = (byte)((value >> 24) & 0xFF);
    }

    private static void WriteLe16(byte[] data, int offset, ushort value)
    {
        data[offset] = (byte)(value & 0xFF);
        data[offset + 1] = (byte)((value >> 8) & 0xFF);
    }

    private static void WriteString32(byte[] data, int offset, string value)
    {
        byte[] raw = string.IsNullOrEmpty(value) ? Array.Empty<byte>() : Encoding.Latin1.GetBytes(value);
        int pos = 0;
        while (pos < 32 && pos < raw.Length)
        {
            data[offset + pos] = raw[pos];
            pos++;
        }
        if (pos < 32)
        {
            data[offset + pos] = 0;
            pos++;
        }
        while (pos < 32)
        {
            data[offset + pos] = 0xDD;
            pos++;
        }
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
    public uint rwVersion { get; set; }
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
    public uint totalSize { get; set; }
    public ushort textureCount { get; set; }
    public ushort dictUnknown { get; set; }
    public uint[] gcnUnknown { get; set; }
    public uint? gcnReserved { get; set; }
    public uint? ps2DataSize { get; set; }
    public uint? ps2Stride { get; set; }
    public uint[] gsRegisters { get; set; }
    public string paletteBase64 { get; set; }
    public string preMipBase64 { get; set; }
    public string tailBase64 { get; set; }
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

    public static GxFormat Detect(byte bitDepth, uint fmt, byte rasterType = 0xFF)
    {
        switch (rasterType)
        {
            case 0x00:
                return GxFormat.I4;
            case 0x01:
                return GxFormat.I8;
            case 0x02:
                return GxFormat.IA4;
            case 0x03:
                return GxFormat.IA8;
            case 0x04:
                return GxFormat.Rgb565;
            case 0x05:
                return GxFormat.Rgb5A3;
            case 0x06:
                return GxFormat.Rgba8;
            case 0x08:
                return GxFormat.CI4;
            case 0x09:
                return GxFormat.CI8;
            case 0x0E:
                return GxFormat.Cmpr;
        }

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

        if (offset < payload.Length)
        {
            int rest = payload.Length - trailer - offset;
            if (rest > 0)
                Array.Copy(payload, offset, outBytes, offset, rest);
            if (trailer > 0)
                Array.Copy(payload, payload.Length - trailer, outBytes, payload.Length - trailer, trailer);
        }

        return outBytes;
    }

    public static int PaletteSize(GxFormat gx)
    {
        switch (gx)
        {
            case GxFormat.CI8:
                return Ci8PaletteSize;
            case GxFormat.CI4:
                return Ci4PaletteSize;
            default:
                return 0;
        }
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
                bw = 8;
                bh = 8;
                blockBytes = 32;
                break;
            case GxFormat.Cmpr:
                bw = 8;
                bh = 8;
                blockBytes = 32;
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
        int[] subOffsets = new[] { 0, 8, 16, 24 };
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

public static class RwtxXbox
{
    public const uint Platform = 0x05;

    private const int RasterFormatC8888 = 0x0500;
    private const int RasterFormatC888 = 0x0600;
    private const int RasterFormatC1555 = 0x0100;
    private const int RasterFormatC565 = 0x0200;
    private const int RasterFormatC4444 = 0x0300;
    private const int RasterFormatLum8 = 0x0400;
    private const int RasterFormatC555 = 0x0A00;

    private const byte Dxt1Format = 0x0C;
    private const byte Dxt3Format = 0x0E;
    private const byte Dxt5Format = 0x0F;

    public static bool HasAlpha(uint rasterFormatFlags)
    {
        int fmt = (int)(rasterFormatFlags >> 8) & 0xF;
        return fmt switch
        {
            1 or 3 or 5 => true,
            _ => false,
        };
    }

    public static string FormatName(uint rasterFormatFlags, byte compression)
    {
        if (compression == Dxt1Format) return "Xbox D3D8 DXT1 (BC1, compressed)";
        if (compression == Dxt3Format) return "Xbox D3D8 DXT3 (BC2, compressed)";
        if (compression == Dxt5Format) return "Xbox D3D8 DXT5 (BC3, compressed)";

        return (rasterFormatFlags & 0x0F00) switch
        {
            RasterFormatC8888 => "Xbox D3D8 A8R8G8B8 (swizzled)",
            RasterFormatC888 => "Xbox D3D8 X8R8G8B8 (swizzled)",
            RasterFormatC1555 => "Xbox D3D8 A1R5G5B5 (swizzled)",
            RasterFormatC565 => "Xbox D3D8 R5G6B5 (swizzled)",
            RasterFormatC4444 => "Xbox D3D8 A4R4G4B4 (swizzled)",
            RasterFormatLum8 => "Xbox D3D8 L8 (swizzled)",
            RasterFormatC555 => "Xbox D3D8 X1R5G5B5 (swizzled)",
            _ => $"Xbox D3D8 format 0x{rasterFormatFlags:X8}",
        };
    }

    public static int Mip0Bytes(int width, int height, byte bitDepth, byte compression)
    {
        if (width <= 0 || height <= 0)
            return 0;

        if (compression == Dxt1Format)
            return ((width + 3) >> 2) * ((height + 3) >> 2) * 8;
        if (compression == Dxt3Format || compression == Dxt5Format)
            return ((width + 3) >> 2) * ((height + 3) >> 2) * 16;
        return width * height * (bitDepth >> 3);
    }

    public static bool TryDecodeMip0(byte[] pixelData, int width, int height, byte bitDepth, uint rasterFormatFlags, byte compression, out byte[] rgba, out byte[] mip0)
    {
        rgba = null;
        mip0 = null;

        if (width <= 0 || height <= 0)
            return false;

        int span = Mip0Bytes(width, height, bitDepth, compression);
        if (span <= 0 || pixelData.Length < span)
            return false;

        mip0 = new byte[span];
        Array.Copy(pixelData, mip0, span);

        try
        {
            if (compression == Dxt1Format || compression == Dxt3Format || compression == Dxt5Format)
            {
                rgba = DecodeDxt(pixelData, width, height, compression);
                return rgba != null;
            }

            int bpp = bitDepth >> 3;
            byte[] lin = new byte[width * height * bpp];
            if (!Unswizzle(pixelData, lin, width, height, bpp, span))
                return false;

            rgba = ConvertToRgba(lin, width, height, bitDepth, rasterFormatFlags);
            return rgba != null;
        }
        catch
        {
            rgba = null;
            mip0 = null;
            return false;
        }
    }

    public static byte[] EncodeMip0(byte[] pixelData, int span, int width, int height, byte bitDepth, uint rasterFormatFlags, byte[] rgba)
    {
        if (bitDepth != 32 || rgba == null || width <= 0 || height <= 0)
            return null;

        try
        {
            byte[] bgra = new byte[width * height * 4];
            for (int i = 0; i < width * height * 4; i += 4)
            {
                bgra[i] = rgba[i + 2];
                bgra[i + 1] = rgba[i + 1];
                bgra[i + 2] = rgba[i];
                bgra[i + 3] = rgba[i + 3];
            }

            byte[] outBytes = new byte[span];
            if (!Swizzle(bgra, outBytes, width, height, 4, span))
                return null;

            byte[] result = new byte[pixelData.Length];
            Array.Copy(pixelData, result, pixelData.Length);
            Array.Copy(outBytes, 0, result, 0, Math.Min(span, pixelData.Length));
            return result;
        }
        catch
        {
            return null;
        }
    }

    private static bool Unswizzle(byte[] src, byte[] dst, int width, int height, int bpp, int span)
    {
        uint maskU = 0;
        uint maskV = 0;
        int i = 1;
        int j = 1;
        int c;
        do
        {
            c = 0;
            if (i < width) { maskU |= (uint)j; j <<= 1; c = j; }
            if (i < height) { maskV |= (uint)j; j <<= 1; c = j; }
            i <<= 1;
        }
        while (c != 0);

        uint v = 0;
        for (int y = 0; y < height; y++)
        {
            uint u = 0;
            for (int x = 0; x < width; x++)
            {
                int so = (int)((u | v) * (uint)bpp);
                int doff = (y * width + x) * bpp;
                if (so < 0 || so + bpp > span || doff + bpp > dst.Length)
                    return false;
                for (int b = 0; b < bpp; b++)
                    dst[doff + b] = src[so + b];
                u = (u - maskU) & maskU;
            }
            v = (v - maskV) & maskV;
        }
        return true;
    }

    private static bool Swizzle(byte[] src, byte[] dst, int width, int height, int bpp, int span)
    {
        uint maskU = 0;
        uint maskV = 0;
        int i = 1;
        int j = 1;
        int c;
        do
        {
            c = 0;
            if (i < width) { maskU |= (uint)j; j <<= 1; c = j; }
            if (i < height) { maskV |= (uint)j; j <<= 1; c = j; }
            i <<= 1;
        }
        while (c != 0);

        uint v = 0;
        for (int y = 0; y < height; y++)
        {
            uint u = 0;
            for (int x = 0; x < width; x++)
            {
                int so = (int)((u | v) * (uint)bpp);
                int doff = (y * width + x) * bpp;
                if (so < 0 || so + bpp > span || doff + bpp > dst.Length)
                    return false;
                for (int b = 0; b < bpp; b++)
                    dst[so + b] = src[doff + b];
                u = (u - maskU) & maskU;
            }
            v = (v - maskV) & maskV;
        }
        return true;
    }

    private static byte[] ConvertToRgba(byte[] lin, int width, int height, byte bitDepth, uint rasterFormatFlags)
    {
        byte[] rgba = new byte[width * height * 4];
        int fmt = (int)(rasterFormatFlags & 0x0F00);

        if (bitDepth == 32)
        {
            for (int i = 0; i < width * height; i++)
            {
                int s = i * 4;
                rgba[s] = lin[s + 2];
                rgba[s + 1] = lin[s + 1];
                rgba[s + 2] = lin[s];
                rgba[s + 3] = fmt == RasterFormatC888 ? (byte)0xFF : lin[s + 3];
            }
            return rgba;
        }

        if (bitDepth == 16)
        {
            for (int i = 0; i < width * height; i++)
            {
                int s = i * 2;
                int word = lin[s] | (lin[s + 1] << 8);
                byte r, g, b, a;
                switch (fmt)
                {
                    case RasterFormatC565:
                        r = (byte)(((word >> 11) & 0x1F) * 255 / 31);
                        g = (byte)(((word >> 5) & 0x3F) * 255 / 63);
                        b = (byte)((word & 0x1F) * 255 / 31);
                        a = 0xFF;
                        break;
                    case RasterFormatC1555:
                        a = (byte)(((word >> 15) & 1) != 0 ? 0xFF : 0);
                        r = (byte)(((word >> 10) & 0x1F) * 255 / 31);
                        g = (byte)(((word >> 5) & 0x1F) * 255 / 31);
                        b = (byte)((word & 0x1F) * 255 / 31);
                        break;
                    case RasterFormatC555:
                        a = 0xFF;
                        r = (byte)(((word >> 10) & 0x1F) * 255 / 31);
                        g = (byte)(((word >> 5) & 0x1F) * 255 / 31);
                        b = (byte)((word & 0x1F) * 255 / 31);
                        break;
                    case RasterFormatC4444:
                        a = (byte)(((word >> 12) & 0xF) * 17);
                        r = (byte)(((word >> 8) & 0xF) * 17);
                        g = (byte)(((word >> 4) & 0xF) * 17);
                        b = (byte)((word & 0xF) * 17);
                        break;
                    default:
                        return null;
                }
                rgba[i * 4] = r;
                rgba[i * 4 + 1] = g;
                rgba[i * 4 + 2] = b;
                rgba[i * 4 + 3] = a;
            }
            return rgba;
        }

        if (bitDepth == 8)
        {
            for (int i = 0; i < width * height; i++)
            {
                byte g = lin[i];
                rgba[i * 4] = g;
                rgba[i * 4 + 1] = g;
                rgba[i * 4 + 2] = g;
                rgba[i * 4 + 3] = 0xFF;
            }
            return rgba;
        }

        return null;
    }

    private static byte[] DecodeDxt(byte[] src, int width, int height, byte compression)
    {
        byte[] rgba = new byte[width * height * 4];
        int blocksX = (width + 3) >> 2;
        int blocksY = (height + 3) >> 2;
        int off = 0;

        for (int by = 0; by < blocksY; by++)
        {
            for (int bx = 0; bx < blocksX; bx++)
            {
                if (compression == Dxt1Format)
                    off = DecodeDxt1Block(src, off, rgba, blocksX, width, height, bx, by);
                else if (compression == Dxt3Format)
                    off = DecodeDxt3Block(src, off, rgba, blocksX, width, height, bx, by);
                else
                    off = DecodeDxt5Block(src, off, rgba, blocksX, width, height, bx, by);
                if (off < 0)
                    return null;
            }
        }
        return rgba;
    }

    private static int DecodeDxt1Block(byte[] src, int off, byte[] dstPix, int blocksX, int w, int h, int bx, int by)
    {
        if (off + 8 > src.Length)
            return -1;

        int c0 = src[off] | (src[off + 1] << 8);
        int c1 = src[off + 2] | (src[off + 3] << 8);

        byte[,] r = new byte[4, 4];
        byte[,] g = new byte[4, 4];
        byte[,] b = new byte[4, 4];
        byte[,] a = new byte[4, 4];

        byte rr0 = (byte)(((c0 >> 11) & 0x1F) * 255 / 31);
        byte gg0 = (byte)(((c0 >> 5) & 0x3F) * 255 / 63);
        byte bb0 = (byte)((c0 & 0x1F) * 255 / 31);
        byte rr1 = (byte)(((c1 >> 11) & 0x1F) * 255 / 31);
        byte gg1 = (byte)(((c1 >> 5) & 0x3F) * 255 / 63);
        byte bb1 = (byte)((c1 & 0x1F) * 255 / 31);

        bool mode = c0 > c1;
        byte[] c2 = new byte[3], c3 = new byte[3];
        byte[] a2 = new byte[4], a3 = new byte[4];
        if (mode)
        {
            c2[0] = (byte)((2 * rr0 + rr1) / 3);
            c2[1] = (byte)((2 * gg0 + gg1) / 3);
            c2[2] = (byte)((2 * bb0 + bb1) / 3);
            c3[0] = (byte)((rr0 + 2 * rr1) / 3);
            c3[1] = (byte)((gg0 + 2 * gg1) / 3);
            c3[2] = (byte)((bb0 + 2 * bb1) / 3);
            a2[0] = a2[1] = a2[2] = a3[0] = a3[1] = a3[2] = 0xFF;
            a3[3] = 0xFF;
        }
        else
        {
            c2[0] = (byte)((rr0 + rr1) / 2);
            c2[1] = (byte)((gg0 + gg1) / 2);
            c2[2] = (byte)((bb0 + bb1) / 2);
            c3[0] = c3[1] = c3[2] = 0;
        }

        int indices = src[off + 4] | (src[off + 5] << 8) | (src[off + 6] << 16) | (src[off + 7] << 24);

        for (int yy = 0; yy < 4; yy++)
        {
            for (int xx = 0; xx < 4; xx++)
            {
                int idx = (indices >> (2 * (yy * 4 + xx))) & 3;
                if (idx == 0) { r[yy, xx] = rr0; g[yy, xx] = gg0; b[yy, xx] = bb0; a[yy, xx] = 0xFF; }
                else if (idx == 1) { r[yy, xx] = rr1; g[yy, xx] = gg1; b[yy, xx] = bb1; a[yy, xx] = 0xFF; }
                else if (idx == 2) { r[yy, xx] = c2[0]; g[yy, xx] = c2[1]; b[yy, xx] = c2[2]; a[yy, xx] = 0xFF; }
                else { r[yy, xx] = c3[0]; g[yy, xx] = c3[1]; b[yy, xx] = c3[2]; a[yy, xx] = mode ? a3[3] : (byte)0; }
            }
        }

        for (int yy = 0; yy < 4; yy++)
        {
            int py = by * 4 + yy;
            if (py >= h) continue;
            for (int xx = 0; xx < 4; xx++)
            {
                int px = bx * 4 + xx;
                if (px >= w) continue;
                int d = (py * w + px) * 4;
                dstPix[d] = r[yy, xx];
                dstPix[d + 1] = g[yy, xx];
                dstPix[d + 2] = b[yy, xx];
                dstPix[d + 3] = a[yy, xx];
            }
        }

        return off + 8;
    }

    private static int DecodeDxt3Block(byte[] src, int off, byte[] dstPix, int blocksX, int w, int h, int bx, int by)
    {
        if (off + 16 > src.Length)
            return -1;

        byte[] a = new byte[16];
        for (int i = 0; i < 8; i++)
        {
            int v = src[off + i];
            a[i * 2] = (byte)((v & 0xF) * 17);
            a[i * 2 + 1] = (byte)(((v >> 4) & 0xF) * 17);
        }

        int c0 = src[off + 8] | (src[off + 9] << 8);
        int c1 = src[off + 10] | (src[off + 11] << 8);
        int indices = src[off + 12] | (src[off + 13] << 8) | (src[off + 14] << 16) | (src[off + 15] << 24);

        byte rr0 = (byte)(((c0 >> 11) & 0x1F) * 255 / 31);
        byte gg0 = (byte)(((c0 >> 5) & 0x3F) * 255 / 63);
        byte bb0 = (byte)((c0 & 0x1F) * 255 / 31);
        byte rr1 = (byte)(((c1 >> 11) & 0x1F) * 255 / 31);
        byte gg1 = (byte)(((c1 >> 5) & 0x3F) * 255 / 63);
        byte bb1 = (byte)((c1 & 0x1F) * 255 / 31);
        byte c2r = (byte)((2 * rr0 + rr1) / 3), c2g = (byte)((2 * gg0 + gg1) / 3), c2b = (byte)((2 * bb0 + bb1) / 3);
        byte c3r = (byte)((rr0 + 2 * rr1) / 3), c3g = (byte)((gg0 + 2 * gg1) / 3), c3b = (byte)((bb0 + 2 * bb1) / 3);

        for (int yy = 0; yy < 4; yy++)
        {
            int py = by * 4 + yy;
            if (py >= h) continue;
            for (int xx = 0; xx < 4; xx++)
            {
                int px = bx * 4 + xx;
                if (px >= w) continue;
                int idx = (indices >> (2 * (yy * 4 + xx))) & 3;
                int d = (py * w + px) * 4;
                if (idx == 0) { dstPix[d] = rr0; dstPix[d + 1] = gg0; dstPix[d + 2] = bb0; }
                else if (idx == 1) { dstPix[d] = rr1; dstPix[d + 1] = gg1; dstPix[d + 2] = bb1; }
                else if (idx == 2) { dstPix[d] = c2r; dstPix[d + 1] = c2g; dstPix[d + 2] = c2b; }
                else { dstPix[d] = c3r; dstPix[d + 1] = c3g; dstPix[d + 2] = c3b; }
                dstPix[d + 3] = a[yy * 4 + xx];
            }
        }

        return off + 16;
    }

    private static int DecodeDxt5Block(byte[] src, int off, byte[] dstPix, int blocksX, int w, int h, int bx, int by)
    {
        if (off + 16 > src.Length)
            return -1;

        int a0 = src[off];
        int a1 = src[off + 1];
        byte[] alpha = new byte[8];
        alpha[0] = (byte)a0;
        alpha[1] = (byte)a1;
        if (a0 > a1)
        {
            alpha[2] = (byte)((6 * a0 + a1) / 7);
            alpha[3] = (byte)((5 * a0 + 2 * a1) / 7);
            alpha[4] = (byte)((4 * a0 + 3 * a1) / 7);
            alpha[5] = (byte)((3 * a0 + 4 * a1) / 7);
            alpha[6] = (byte)((2 * a0 + 5 * a1) / 7);
            alpha[7] = (byte)((a0 + 6 * a1) / 7);
        }
        else
        {
            alpha[2] = (byte)((4 * a0 + a1) / 5);
            alpha[3] = (byte)((3 * a0 + 2 * a1) / 5);
            alpha[4] = (byte)((2 * a0 + 3 * a1) / 5);
            alpha[5] = (byte)((a0 + 4 * a1) / 5);
            alpha[6] = 0;
            alpha[7] = 0xFF;
        }

        int c0 = src[off + 8] | (src[off + 9] << 8);
        int c1 = src[off + 10] | (src[off + 11] << 8);
        int indices = src[off + 12] | (src[off + 13] << 8) | (src[off + 14] << 16) | (src[off + 15] << 24);

        byte rr0 = (byte)(((c0 >> 11) & 0x1F) * 255 / 31);
        byte gg0 = (byte)(((c0 >> 5) & 0x3F) * 255 / 63);
        byte bb0 = (byte)((c0 & 0x1F) * 255 / 31);
        byte rr1 = (byte)(((c1 >> 11) & 0x1F) * 255 / 31);
        byte gg1 = (byte)(((c1 >> 5) & 0x3F) * 255 / 63);
        byte bb1 = (byte)((c1 & 0x1F) * 255 / 31);
        byte c2r = (byte)((2 * rr0 + rr1) / 3), c2g = (byte)((2 * gg0 + gg1) / 3), c2b = (byte)((2 * bb0 + bb1) / 3);
        byte c3r = (byte)((rr0 + 2 * rr1) / 3), c3g = (byte)((gg0 + 2 * gg1) / 3), c3b = (byte)((bb0 + 2 * bb1) / 3);

        long alphaBits = (long)(uint)(src[off + 2] | (src[off + 3] << 8) | (src[off + 4] << 16) | (src[off + 5] << 24)) | ((long)src[off + 6] << 32) | ((long)src[off + 7] << 40);

        for (int yy = 0; yy < 4; yy++)
        {
            int py = by * 4 + yy;
            if (py >= h) continue;
            for (int xx = 0; xx < 4; xx++)
            {
                int px = bx * 4 + xx;
                if (px >= w) continue;
                int cIdx = (indices >> (2 * (yy * 4 + xx))) & 3;
                int aIdx = (int)((alphaBits >> (3 * (yy * 4 + xx))) & 7);
                int d = (py * w + px) * 4;
                if (cIdx == 0) { dstPix[d] = rr0; dstPix[d + 1] = gg0; dstPix[d + 2] = bb0; }
                else if (cIdx == 1) { dstPix[d] = rr1; dstPix[d + 1] = gg1; dstPix[d + 2] = bb1; }
                else if (cIdx == 2) { dstPix[d] = c2r; dstPix[d + 1] = c2g; dstPix[d + 2] = c2b; }
                else { dstPix[d] = c3r; dstPix[d + 1] = c3g; dstPix[d + 2] = c3b; }
                dstPix[d + 3] = alpha[aIdx];
            }
        }

        return off + 16;
    }
}

public static class RwtxPs2
{
    public const uint Platform = 0x00325350;
    public const int WidthOffset = 0x74;
    public const int HeightOffset = 0x78;
    public const int BitDepthOffset = 0x7C;

    private const int BlobOffset = 0x20;
    private const int BlobSizeOffset = 0x18;
    private const int BlockHeaderSize = 0x50;
    private const int BlockDataSizeField = 0x40;
    private const int PaletteEntries = 256;

    public static uint ReadLe32(byte[] data, int offset)
    {
        return (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
    }

    public static bool TryDecodeMip0(byte[] pixelData, int width, int height, byte bitDepth, out byte[] rgba, out byte[] mip0, out int mipMapCount)
    {
        rgba = null;
        mip0 = null;
        mipMapCount = 0;

        try
        {
            byte[][] blocks = SplitBlocks(pixelData);
            if (blocks.Length < 1)
                return false;

            switch (bitDepth)
            {
                case 4:
                    return TryDecode4(blocks, width, height, out rgba, out mip0, out mipMapCount);
                case 8:
                    return TryDecode8(blocks, width, height, out rgba, out mip0, out mipMapCount);
                case 32:
                    return TryDecode32(blocks, width, height, out rgba, out mip0, out mipMapCount);
                default:
                    return false;
            }
        }
        catch
        {
            rgba = null;
            mip0 = null;
            mipMapCount = 0;
            return false;
        }
    }

    public static byte[] EncodeMip0(byte[] pixelData, int width, int height, byte bitDepth, byte[] rgba)
    {
        try
        {
            byte[][] blocks = SplitBlocks(pixelData);
            if (blocks.Length < 1)
                return null;

            switch (bitDepth)
            {
                case 4:
                    return Encode4(pixelData, blocks, width, height, rgba);
                case 8:
                    return Encode8(pixelData, blocks, width, height, rgba);
                case 32:
                    return Encode32(pixelData, blocks, width, height, rgba);
                default:
                    return null;
            }
        }
        catch
        {
            return null;
        }
    }

    public static int Mip0Bytes(int width, int height, byte bitDepth)
    {
        if (width <= 0 || height <= 0)
            return 0;
        switch (bitDepth)
        {
            case 4:
                return width * height >> 1;
            case 32:
                return width * height * 4;
            case 8:
                return width * height;
            default:
                return 0;
        }
    }

    private static bool TryDecode4(byte[][] blocks, int width, int height, out byte[] rgba, out byte[] mip0, out int mipMapCount)
    {
        rgba = null;
        mip0 = null;
        mipMapCount = 0;

        int packedBytes = width * height / 2;
        byte[] raw = blocks[0];
        if (packedBytes <= 0 || raw.Length < packedBytes)
            return false;

        byte[] palette = BuildPalette(blocks[blocks.Length - 1]);
        byte[] palUnswizzled = UnswizzlePalette(palette);
        byte[] idx = Unswizzle4(raw, width, height);

        rgba = new byte[width * height * 4];
        for (int i = 0; i < idx.Length; i++)
        {
            int po = i * 4;
            rgba[po] = palUnswizzled[idx[i] * 4];
            rgba[po + 1] = palUnswizzled[idx[i] * 4 + 1];
            rgba[po + 2] = palUnswizzled[idx[i] * 4 + 2];
            rgba[po + 3] = palUnswizzled[idx[i] * 4 + 3];
        }

        mip0 = new byte[packedBytes];
        Array.Copy(raw, mip0, packedBytes);
        mipMapCount = 1;
        return true;
    }

    private static bool TryDecode8(byte[][] blocks, int width, int height, out byte[] rgba, out byte[] mip0, out int mipMapCount)
    {
        rgba = null;
        mip0 = null;
        mipMapCount = 0;

        if (blocks.Length < 2 || blocks[0].Length < width * height)
            return false;

        byte[] palette = BuildPalette(blocks[blocks.Length - 1]);
        byte[] palUnswizzled = UnswizzlePalette(palette);
        byte[] raw = blocks[0];
        int mip0Bytes = width * height;

        rgba = new byte[mip0Bytes * 4];
        for (int py = 0; py < height; py++)
        {
            for (int px = 0; px < width; px++)
            {
                int src = Coord8(px, py, width);
                if (src >= raw.Length)
                    return false;
                byte index = raw[src];
                int po = (py * width + px) * 4;
                rgba[po] = palUnswizzled[index * 4];
                rgba[po + 1] = palUnswizzled[index * 4 + 1];
                rgba[po + 2] = palUnswizzled[index * 4 + 2];
                rgba[po + 3] = palUnswizzled[index * 4 + 3];
            }
        }

        mip0 = new byte[mip0Bytes];
        Array.Copy(raw, mip0, mip0Bytes);
        mipMapCount = CountMips(blocks, width, height);
        return true;
    }

    private static bool TryDecode32(byte[][] blocks, int width, int height, out byte[] rgba, out byte[] mip0, out int mipMapCount)
    {
        rgba = null;
        mip0 = null;
        mipMapCount = 0;

        int mip0Bytes = width * height * 4;
        byte[] raw = blocks[0];
        if (mip0Bytes <= 0 || raw.Length < mip0Bytes)
            return false;

        rgba = new byte[mip0Bytes];
        for (int py = 0; py < height; py++)
        {
            for (int px = 0; px < width; px++)
            {
                int src = Coord32(px, py, width);
                int po = (py * width + px) * 4;
                rgba[po] = raw[src];
                rgba[po + 1] = raw[src + 1];
                rgba[po + 2] = raw[src + 2];
                rgba[po + 3] = raw[src + 3];
            }
        }

        mip0 = new byte[mip0Bytes];
        Array.Copy(raw, mip0, mip0Bytes);
        mipMapCount = 1;
        return true;
    }

    private static byte[] BuildPalette(byte[] paletteBlock)
    {
        byte[] palette = new byte[PaletteEntries * 4];
        int n = Math.Min(paletteBlock.Length, palette.Length);
        Array.Copy(paletteBlock, palette, n);
        return palette;
    }

    private static byte[] Encode4(byte[] pixelData, byte[][] blocks, int width, int height, byte[] rgba)
    {
        int packedBytes = width * height / 2;
        byte[] raw = blocks[0];
        if (packedBytes <= 0 || raw.Length < packedBytes)
            return null;

        byte[] palette = BuildPalette(blocks[blocks.Length - 1]);
        byte[] palUnswizzled = UnswizzlePalette(palette);
        GetSwizzle4Map(width, height, out int[] srcByte, out int[] shift);

        byte[] newPacked = new byte[packedBytes];

        for (int i = 0; i < width * height; i++)
        {
            int s = srcByte[i];
            if (s < 0 || s >= packedBytes)
                continue;

            int index = NearestPaletteIndex(rgba, i * 4, palUnswizzled, 16);
            if (shift[i] == 0)
                newPacked[s] |= (byte)(index & 0x0F);
            else
                newPacked[s] |= (byte)((index & 0x0F) << 4);
        }

        byte[] outBytes = new byte[pixelData.Length];
        Array.Copy(pixelData, outBytes, pixelData.Length);
        Array.Copy(newPacked, 0, outBytes, BlobOffset + BlockHeaderSize, packedBytes);
        return outBytes;
    }

    private static byte[] Encode8(byte[] pixelData, byte[][] blocks, int width, int height, byte[] rgba)
    {
        if (blocks.Length < 2 || blocks[0].Length < width * height)
            return null;

        byte[] palette = BuildPalette(blocks[blocks.Length - 1]);
        byte[] palUnswizzled = UnswizzlePalette(palette);
        byte[] raw = blocks[0];
        int mip0Bytes = width * height;

        byte[] newMip0 = new byte[mip0Bytes];
        for (int py = 0; py < height; py++)
        {
            for (int px = 0; px < width; px++)
            {
                int src = Coord8(px, py, width);
                if (src >= raw.Length)
                    return null;
                newMip0[src] = NearestPaletteIndex(rgba, (py * width + px) * 4, palUnswizzled, 256);
            }
        }

        byte[] outBytes = new byte[pixelData.Length];
        Array.Copy(pixelData, outBytes, pixelData.Length);
        Array.Copy(newMip0, 0, outBytes, BlobOffset + BlockHeaderSize, mip0Bytes);
        return outBytes;
    }

    private static byte[] Encode32(byte[] pixelData, byte[][] blocks, int width, int height, byte[] rgba)
    {
        int mip0Bytes = width * height * 4;
        byte[] raw = blocks[0];
        if (mip0Bytes <= 0 || raw.Length < mip0Bytes)
            return null;

        byte[] newMip0 = new byte[mip0Bytes];
        for (int py = 0; py < height; py++)
        {
            for (int px = 0; px < width; px++)
            {
                int src = Coord32(px, py, width);
                int po = (py * width + px) * 4;
                newMip0[src] = rgba[po];
                newMip0[src + 1] = rgba[po + 1];
                newMip0[src + 2] = rgba[po + 2];
                newMip0[src + 3] = rgba[po + 3];
            }
        }

        byte[] outBytes = new byte[pixelData.Length];
        Array.Copy(pixelData, outBytes, pixelData.Length);
        Array.Copy(newMip0, 0, outBytes, BlobOffset + BlockHeaderSize, mip0Bytes);
        return outBytes;
    }

    private static byte[] Unswizzle4(byte[] packed, int width, int height)
    {
        GetSwizzle4Map(width, height, out int[] srcByte, out int[] shift);
        byte[] outIdx = new byte[width * height];
        for (int i = 0; i < outIdx.Length; i++)
        {
            int s = srcByte[i];
            byte v = (s >= 0 && s < packed.Length) ? packed[s] : (byte)0;
            outIdx[i] = (byte)((v >> shift[i]) & 0x0F);
        }
        return outIdx;
    }

    private static readonly int[] GsBlock4 = { 0, 2, 8, 10, 1, 3, 9, 11, 4, 6, 12, 14, 5, 7, 13, 15, 16, 18, 24, 26, 17, 19, 25, 27, 20, 22, 28, 30, 21, 23, 29, 31 };
    private static readonly int[] GsColEven = { 0, 1, 4, 5, 8, 9, 12, 13, 2, 3, 6, 7, 10, 11, 14, 15 };
    private static readonly int[] GsColOdd = { 8, 9, 12, 13, 0, 1, 4, 5, 10, 11, 14, 15, 2, 3, 6, 7 };

    private static void GetSwizzle4Map(int width, int height, out int[] srcByte, out int[] shift)
    {
        int dbw = Math.Max(1, width >> 7);
        int memSize = dbw * Math.Max(1, (height + 127) >> 7) * 8192;
        int[] mem = new int[memSize];

        int i = 0;
        for (int y = 0; y < height / 2; y++)
        {
            int pagey = (y >> 6) * dbw;
            int py = y & 0x3F;
            int blocky = (py >> 3) * 4;
            int column = (py & 7) >> 1;
            for (int x = 0; x < width / 2; x++)
            {
                int px = x & 0x3F;
                int block = GsBlock4[(px >> 4) + blocky];
                int cx = px & 0xF;
                int addr = ((x >> 6) + pagey) * 8192 + block * 256 + column * 64 + GsColEven[((py & 1) << 3) + (cx & 7)] * 4 + (cx >> 3) * 2;
                if (addr + 1 < memSize)
                {
                    mem[addr] = i;
                    mem[addr + 1] = i + 1;
                }
                i += 2;
            }
        }

        srcByte = new int[width * height];
        shift = new int[width * height];
        int k = 0;
        for (int y = 0; y < height; y++)
        {
            int pagey = (y >> 7) * dbw;
            int py = y & 0x7F;
            int blocky = (py >> 4) * 4;
            int by = py & 0xF;
            int column = by >> 2;
            int cy = by & 3;
            int[] pattern = (((cy >> 1) ^ (column & 1)) != 0) ? GsColOdd : GsColEven;
            for (int x = 0; x < width; x++)
            {
                int px = x & 0x7F;
                int block = GsBlock4[(px >> 5) + blocky];
                int cx = px & 0x1F;
                int nib = (cy >> 1) + (cx >> 3) * 2;
                int addr = ((x >> 7) + pagey) * 8192 + block * 256 + column * 64 + pattern[((cy & 1) << 3) + (cx & 7)] * 4 + (nib >> 1);
                srcByte[k] = (addr >= 0 && addr < memSize) ? mem[addr] : -1;
                shift[k] = (nib & 1) * 4;
                k++;
            }
        }
    }

    private static readonly int[] Block32 = { 0, 1, 4, 5, 16, 17, 20, 21, 2, 3, 6, 7, 18, 19, 22, 23, 8, 9, 12, 13, 24, 25, 28, 29, 10, 11, 14, 15, 26, 27, 30, 31 };
    private static readonly int[] ColumnWord32 = { 0, 1, 4, 5, 8, 9, 12, 13, 2, 3, 6, 7, 10, 11, 14, 15 };

    private static int Coord32(int x, int y, int width)
    {
        int dbw = Math.Max(1, (width + 63) >> 6);
        int pageX = x >> 6;
        int pageY = y >> 5;
        int page = pageX + pageY * dbw;
        int px = x & 0x3F;
        int py = y & 0x1F;
        int blockX = px >> 3;
        int blockY = py >> 3;
        int block = Block32[blockX + blockY * 8];
        int bx = px & 7;
        int by = py & 7;
        int column = by >> 1;
        int cy = by & 1;
        int cw = ColumnWord32[bx + cy * 8];
        return (page * 2048 + block * 64 + column * 16 + cw) * 4;
    }

    private static byte[][] SplitBlocks(byte[] pixelData)
    {
        int size = (int)ReadLe32(pixelData, BlobSizeOffset);
        if (size <= 0 || BlobOffset + size > pixelData.Length)
            size = pixelData.Length - BlobOffset;
        if (size <= 0)
            return Array.Empty<byte[]>();

        int end = BlobOffset + size;
        int count = 0;
        int offset = BlobOffset;
        while (offset + BlockHeaderSize <= end && pixelData[offset] == 0x03)
        {
            int dataSize = (int)ReadLe32(pixelData, offset + BlockDataSizeField) * 16;
            if (dataSize <= 0)
                break;
            offset += BlockHeaderSize + dataSize;
            count++;
        }

        byte[][] blocks = new byte[count][];
        offset = BlobOffset;
        for (int i = 0; i < count; i++)
        {
            int dataSize = (int)ReadLe32(pixelData, offset + BlockDataSizeField) * 16;
            blocks[i] = new byte[dataSize];
            Array.Copy(pixelData, offset + BlockHeaderSize, blocks[i], 0, dataSize);
            offset += BlockHeaderSize + dataSize;
        }

        return blocks;
    }

    private static int Coord8(int x, int y, int width)
    {
        int bl = (y & ~0xF) * width + (x & ~0xF) * 2;
        int ss = (((y + 2) >> 2) & 1) * 4;
        int py = (((y & ~3) >> 1) + (y & 1)) & 7;
        int cl = py * width * 2 + ((x + ss) & 7) * 4;
        int bn = ((y >> 1) & 1) + ((x >> 2) & 2);
        return bl + cl + bn;
    }

    private static byte[] UnswizzlePalette(byte[] palette)
    {
        byte[] unswizzled = new byte[PaletteEntries * 4];
        for (int block = 0; block < 8; block++)
        {
            int j = block * 32;
            int i = block * 32;
            Array.Copy(palette, j * 4, unswizzled, i * 4, 32);
            Array.Copy(palette, (j + 8) * 4, unswizzled, (i + 16) * 4, 32);
            Array.Copy(palette, (j + 16) * 4, unswizzled, (i + 8) * 4, 32);
            Array.Copy(palette, (j + 24) * 4, unswizzled, (i + 24) * 4, 32);
        }
        return unswizzled;
    }

    private static byte NearestPaletteIndex(byte[] rgba, int pixelOffset, byte[] palette, int entryCount)
    {
        byte r = rgba[pixelOffset];
        byte g = rgba[pixelOffset + 1];
        byte b = rgba[pixelOffset + 2];
        byte a = rgba[pixelOffset + 3];
        int best = 0;
        int bestDist = int.MaxValue;

        for (int i = 0; i < entryCount; i++)
        {
            int pr = palette[i * 4];
            int pg = palette[i * 4 + 1];
            int pb = palette[i * 4 + 2];
            int pa = palette[i * 4 + 3];
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

        return (byte)best;
    }

    private static int CountMips(byte[][] blocks, int width, int height)
    {
        int count = 0;
        int w = width;
        int h = height;

        for (int i = 0; i < blocks.Length; i++)
        {
            if (blocks[i].Length == w * h)
            {
                count++;
                if (w > 1)
                    w >>= 1;
                else
                    break;
                if (h > 1)
                    h >>= 1;
                else
                    break;
            }
            else
            {
                break;
            }
        }

        return count;
    }
}