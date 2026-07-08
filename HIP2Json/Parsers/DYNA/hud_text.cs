using System.IO;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class hud_textParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version)
    {
        return new hud_text
        {
            loc = ReadVector3BE(br),
            size = ReadVector3BE(br),
            text_box = ReadUInt32BE(br),
            text = ReadUInt32BE(br)
        };
    }

    public override byte[] Serialize(object obj, short version)
    {
        hud_text hud_text = (hud_text)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteVector3BE(bw, hud_text.loc);
        WriteVector3BE(bw, hud_text.size);
        WriteUInt32BE(bw, hud_text.text_box);
        WriteUInt32BE(bw, hud_text.text);

        return ms.ToArray();
    }

    public override string GetFolderName() { return "HUDText"; }
}

public class hud_text
{
    public xVec3 loc { get; set; }
    public xVec3 size { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint text_box { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint text { get; set; }
}