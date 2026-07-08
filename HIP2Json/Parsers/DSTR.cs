using System.IO;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class DSTRParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        return new DSTR
        {
            animSpeed = ReadFloatBE(br),
            initAnimState = ReadUInt32BE(br),
            health = ReadUInt32BE(br),
            spawnItemID = ReadUInt32BE(br),
            dflags = ReadUInt32BE(br),
            collType = ReadByte(br),
            fxType = ReadByte(br),
            pad0 = ReadByte(br),
            pad1 = ReadByte(br),
            blast_radius = ReadFloatBE(br),
            blast_strength = ReadFloatBE(br),
            shrapnelID_destroy = ReadUInt32BE(br),
            shrapnelID_hit = ReadUInt32BE(br),
            sfx_destroy = ReadUInt32BE(br),
            sfx_hit = ReadUInt32BE(br),
            hitModel = ReadUInt32BE(br),
            destroyModel = ReadUInt32BE(br),
        };
    }

    public override object Serialize(object obj)
    {
        DSTR dstr = (DSTR)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteFloatBE(bw, dstr.animSpeed);
        WriteUInt32BE(bw, dstr.initAnimState);
        WriteUInt32BE(bw, dstr.health);
        WriteUInt32BE(bw, dstr.spawnItemID);
        WriteUInt32BE(bw, dstr.dflags);
        WriteByte(bw, dstr.collType);
        WriteByte(bw, dstr.fxType);
        WriteByte(bw, dstr.pad0);
        WriteByte(bw, dstr.pad1);
        WriteFloatBE(bw, dstr.blast_radius);
        WriteFloatBE(bw, dstr.blast_strength);
        WriteUInt32BE(bw, dstr.shrapnelID_destroy);
        WriteUInt32BE(bw, dstr.shrapnelID_hit);
        WriteUInt32BE(bw, dstr.sfx_destroy);
        WriteUInt32BE(bw, dstr.sfx_hit);
        WriteUInt32BE(bw, dstr.hitModel);
        WriteUInt32BE(bw, dstr.destroyModel);
        
        return ms.ToArray();
    }
}

public class DSTR
{
    public float animSpeed { get; set; }
    public uint initAnimState { get; set; }
    public uint health { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint spawnItemID { get; set; }
    public uint dflags { get; set; }
    public byte collType { get; set; }
    public byte fxType { get; set; }
    public byte pad0 { get; set; }
    public byte pad1 { get; set; }
    public float blast_radius { get; set; }
    public float blast_strength { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint shrapnelID_destroy { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint shrapnelID_hit { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint sfx_destroy { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint sfx_hit { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint hitModel { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint destroyModel { get; set; }
}