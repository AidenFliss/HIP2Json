using System;
using System.IO;
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

        return new RWTX
        {
            platformType = Util.ReadUInt32(data, PlatformTypeOffset),
            filterAndAddress = Util.ReadUInt32(data, FilterAddressOffset),
            textureName = gcLayout ? ReadName(data, TextureNameOffset) : null,
            alphaName = gcLayout ? ReadName(data, AlphaNameOffset) : null,
            rasterFormatFlags = gcLayout ? Util.ReadUInt32(data, RasterFormatOffset) : 0,
            width = gcLayout ? Util.ReadUInt16(data, WidthOffset) : (ushort)0,
            height = gcLayout ? Util.ReadUInt16(data, HeightOffset) : (ushort)0,
            bitDepth = gcLayout ? data[BitDepthOffset] : (byte)0,
            mipMapCount = gcLayout ? data[MipCountOffset] : (byte)0,
            rasterType = gcLayout ? data[RasterTypeOffset] : (byte)0,
            compression = gcLayout ? data[CompressionOffset] : (byte)0,
            transparency = gcLayout ? data[TransparencyOffset] : (byte)0,
            prefix = Convert.ToBase64String(data.AsSpan(0, PixelOffset)),
            pixelData = Convert.ToBase64String(data.AsSpan(PixelOffset)),
        };
    }

    public override object Serialize(object obj)
    {
        RWTX rwtx = (RWTX)obj;

        byte[] prefix = Convert.FromBase64String(rwtx.prefix ?? string.Empty);
        byte[] pixels = Convert.FromBase64String(rwtx.pixelData ?? string.Empty);

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

        using var ms = new MemoryStream(prefix.Length + pixels.Length);
        ms.Write(prefix, 0, prefix.Length);
        ms.Write(pixels, 0, pixels.Length);
        return ms.ToArray();
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
}