using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class UIFTParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        return new UIFT
        {
            uiFlags = ReadInt32BE(br),
            width = ReadInt16BE(br),
            height = ReadInt16BE(br),
            texture = ReadUInt32BE(br),
            uva = ReadVector2BE(br),
            uvb = ReadVector2BE(br),
            uvc = ReadVector2BE(br),
            uvd = ReadVector2BE(br),
            uiFontFlags = ReadInt16BE(br),
            mode = ReadByte(br),
            fontID = ReadByte(br),
            textAssetID = ReadUInt32BE(br),
            bcolor = ReadColorBE(br),
            color = ReadColorBE(br),
            inset = Enumerable.Range(0, 3)
                    .Select(_ => ReadInt16BE(br))
                    .ToArray(),
            space = Enumerable.Range(0, 1)
                    .Select(_ => ReadInt16BE(br))
                    .ToArray(),
            cdim = Enumerable.Range(0, 1)
                    .Select(_ => ReadInt16BE(br))
                    .ToArray(),
            max_height = ReadInt32BE(br)
        };
    }

    public override object Serialize(object obj)
    {
        UIFT uift = (UIFT)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteInt32BE(bw, uift.uiFlags);
        WriteInt16BE(bw, uift.width);
        WriteInt16BE(bw, uift.height);
        WriteUInt32BE(bw, uift.texture);
        WriteVector2BE(bw, uift.uva);
        WriteVector2BE(bw, uift.uvb);
        WriteVector2BE(bw, uift.uvc);
        WriteVector2BE(bw, uift.uvd);
        WriteInt16BE(bw, uift.uiFontFlags);
        WriteByte(bw, uift.mode);
        WriteByte(bw, uift.fontID);
        WriteUInt32BE(bw, uift.textAssetID);
        WriteColorBE(bw, uift.bcolor);
        WriteColorBE(bw, uift.color);
        foreach (short s in uift.inset)
            WriteInt16BE(bw, s);
        foreach (short s in uift.space)
            WriteInt16BE(bw, s);
        foreach (short s in uift.cdim)
            WriteInt16BE(bw, s);
        WriteInt32BE(bw, uift.max_height);
        
        return ms.ToArray();
    }
}

public class UIFT
{
    public int uiFlags { get; set; }
    public short width { get; set; }
    public short height { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint texture { get; set; }
    public xVec2 uva { get; set; }
    public xVec2 uvb { get; set; }
    public xVec2 uvc { get; set; }
    public xVec2 uvd { get; set; }
    public short uiFontFlags { get; set; }
    public byte mode { get; set; }
    public byte fontID { get; set; }
    public uint textAssetID { get; set; }
    public xColor bcolor { get; set; }
    public xColor color { get; set; }
    public short[] inset { get; set; }
    public short[] space { get; set; }
    public short[] cdim { get; set; }
    public int max_height { get; set; }
}