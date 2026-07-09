using System.IO;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class game_object_RingParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version)
    {
        return new game_object_Ring
        {
            location = ReadVector3BE(br),
            direction = ReadVector3BE(br),
            offset = ReadVector3BE(br),
            scale = ReadVector3BE(br),
            triggerBoundsType = ReadUInt32BE(br),
            radius = ReadFloatBE(br),
            width = ReadFloatBE(br),
            height = ReadFloatBE(br),
            timeOut = ReadFloatBE(br),
            warningTime = ReadFloatBE(br),
            drivenById = ReadUInt32BE(br),
        };
    }

    public override byte[] Serialize(object obj, short version)
    {
        game_object_Ring ring = (game_object_Ring)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteVector3BE(bw, ring.location);
        WriteVector3BE(bw, ring.direction);
        WriteVector3BE(bw, ring.offset);
        WriteVector3BE(bw, ring.scale);
        WriteUInt32BE(bw, ring.triggerBoundsType);
        WriteFloatBE(bw, ring.radius);
        WriteFloatBE(bw, ring.width);
        WriteFloatBE(bw, ring.height);
        WriteFloatBE(bw, ring.timeOut);
        WriteFloatBE(bw, ring.warningTime);
        WriteUInt32BE(bw, ring.drivenById);

        return ms.ToArray();
    }

    public override string GetFolderName() { return "Ring"; }
}

public class game_object_Ring
{
    public xVec3 location { get; set; }
    public xVec3 direction { get; set; }
    public xVec3 offset { get; set; }
    public xVec3 scale { get; set; }
    public uint triggerBoundsType { get; set; }
    public float radius { get; set; }
    public float width { get; set; }
    public float height { get; set; }
    public float timeOut { get; set; }
    public float warningTime { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint drivenById { get; set; }
}