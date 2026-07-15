using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class SDFXParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        return new SDFX
        {
            soundGroupAssetID = ReadUInt32BE(br),
            attachID = ReadUInt32BE(br),
            pos = ReadVector3BE(br),
            flags = (SDFXFlags)ReadInt32BE(br),
        };
    }

    public override object Serialize(object obj)
    {
        SDFX sdfx = (SDFX)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, sdfx.soundGroupAssetID);
        WriteUInt32BE(bw, sdfx.attachID);
        WriteVector3BE(bw, sdfx.pos);
        WriteInt32BE(bw, (int)sdfx.flags);

        return ms.ToArray();
    }
}

public class SDFX
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint soundGroupAssetID { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint attachID { get; set; }
    public xVec3 pos { get; set; }
    public SDFXFlags flags { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SDFXFlags : int
{
    Normal = 0,
    Unknown1 = 1,
    Unknown2 = 2,
    Unknown3 = 3,
    PlayFromEntity = 4,
}