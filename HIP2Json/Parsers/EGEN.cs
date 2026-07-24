using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class EGENParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        xVec3 src_dpos = ReadVector3BE(br);
        byte damage_type = ReadByte(br);
        byte flags = ReadByte(br);

        br.ReadBytes(2);

        float ontime = ReadFloatBE(br);
        uint onAnimID = ReadUInt32BE(br);

        return new EGEN
        {
            src_dpos = src_dpos,
            damage_type = damage_type,
            flags = flags,
            ontime = ontime,
            onAnimID = onAnimID
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
        bw.Write(new byte[2]);
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
    public float ontime { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint onAnimID { get; set; }
}