using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class DSTRParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        float animSpeed = ReadFloatBE(br);
        uint initAnimState = ReadUInt32BE(br);
        uint health = ReadUInt32BE(br);
        uint spawnItemID = ReadUInt32BE(br);
        uint dflags = ReadUInt32BE(br);
        byte collType = ReadByte(br);
        byte fxType = ReadByte(br);

        br.ReadBytes(2);

        float blast_radius = ReadFloatBE(br);
        float blast_strength = ReadFloatBE(br);
        uint shrapnelID_destroy = ReadUInt32BE(br);
        uint shrapnelID_hit = ReadUInt32BE(br);
        uint sfx_destroy = ReadUInt32BE(br);
        uint sfx_hit = ReadUInt32BE(br);
        uint hitModel = ReadUInt32BE(br);
        uint destroyModel = ReadUInt32BE(br);

        return new DSTR
        {
            animSpeed = animSpeed,
            initAnimState = initAnimState,
            health = health,
            spawnItemID = spawnItemID,
            dflags = dflags,
            collType = collType,
            fxType = fxType,
            blast_radius = blast_radius,
            blast_strength = blast_strength,
            shrapnelID_destroy = shrapnelID_destroy,
            shrapnelID_hit = shrapnelID_hit,
            sfx_destroy = sfx_destroy,
            sfx_hit = sfx_hit,
            hitModel = hitModel,
            destroyModel = destroyModel
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
        bw.Write(new byte[2]);
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