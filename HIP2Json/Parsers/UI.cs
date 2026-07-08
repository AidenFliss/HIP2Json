using System.IO;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class UIParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        return new UI
        {
            uiFlags = ReadInt32BE(br),
            width = ReadInt16BE(br),
            height = ReadInt16BE(br),
            texture = ReadUInt32BE(br),
            uva = ReadVector2BE(br),
            uvb = ReadVector2BE(br),
            uvc = ReadVector2BE(br),
            uvd = ReadVector2BE(br),
        };
    }

    public override object Serialize(object obj)
    {
        UI ui = (UI)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteInt32BE(bw, ui.uiFlags);
        WriteInt16BE(bw, ui.width);
        WriteInt16BE(bw, ui.height);
        WriteUInt32BE(bw, ui.texture);
        WriteVector2BE(bw, ui.uva);
        WriteVector2BE(bw, ui.uvb);
        WriteVector2BE(bw, ui.uvc);
        WriteVector2BE(bw, ui.uvd);
        
        return ms.ToArray();
    }
}

public class UI
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
}