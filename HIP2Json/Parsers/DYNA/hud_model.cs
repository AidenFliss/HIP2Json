using System.IO;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class hud_modelParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version, string dynaType)
    {
        return new hud_model
        {
            loc = ReadVector3BE(br),
            size = ReadVector3BE(br),
            model = ReadUInt32BE(br)
        };
    }

    public override byte[] Serialize(object obj, short version, string dynaType)
    {
        hud_model hud_model = (hud_model)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteVector3BE(bw, hud_model.loc);
        WriteVector3BE(bw, hud_model.size);
        WriteUInt32BE(bw, hud_model.model);

        return ms.ToArray();
    }

    public override string GetFolderName() { return "HUDModel"; }
}

public class hud_model
{
    public xVec3 loc { get; set; }
    public xVec3 size { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint model { get; set; }
}