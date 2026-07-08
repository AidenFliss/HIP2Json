using System.IO;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class EGENParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        return new EGEN
        {
            src_dpos = ReadVector3BE(br),
            damage_type = ReadByte(br),
            flags = ReadByte(br),
            pad1 = ReadByte(br),
            pad2 = ReadByte(br),
            ontime = ReadFloatBE(br),
            onAnimID = ReadUInt32BE(br)
        };
    }

    public override object Serialize(object obj)
    {
        EGEN egen = (EGEN)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteVector3BE(bw, egen.src_dpos);
        WriteByte(bw, egen.damage_type);
        WriteByte(bw, egen.flags);
        WriteByte(bw, egen.pad1);
        WriteByte(bw, egen.pad2);
        WriteFloatBE(bw, egen.ontime);
        WriteUInt32BE(bw, egen.onAnimID);
        
        return ms.ToArray();
    }
}

public class EGEN
{
    public xVec3 src_dpos { get; set; }
    public byte damage_type { get; set; }
    public byte flags { get; set; }
    public byte pad1 { get; set; }
    public byte pad2 { get; set; }
    public float ontime { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint onAnimID { get; set; }
}