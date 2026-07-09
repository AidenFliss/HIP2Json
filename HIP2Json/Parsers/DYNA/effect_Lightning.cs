using System.IO;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class effect_LightningParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version)
    {
        return new effect_Lightning
        {
            start = ReadVector3BE(br),
            end = ReadVector3BE(br),
            color = ReadColor(br),
            thickness = ReadFloatBE(br),
            branchSpeed = ReadFloatBE(br),
            mainTexture = ReadUInt32BE(br),
            branchTexture = ReadUInt32BE(br),
            damage = ReadInt32BE(br),
            knockBackSpeed = ReadFloatBE(br),
            sound = ReadUInt32BE(br),
            soundHit1 = ReadUInt32BE(br),
            soundHit2 = ReadUInt32BE(br),
            followStart = ReadUInt32BE(br),
            followEnd = ReadUInt32BE(br),
            collisionEnabled = ReadUInt32BE(br),
        };
    }

    public override byte[] Serialize(object obj, short version)
    {
        effect_Lightning lightning = (effect_Lightning)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteVector3BE(bw, lightning.start);
        WriteVector3BE(bw, lightning.end);
        WriteColorBE(bw, lightning.color);
        WriteFloatBE(bw, lightning.thickness);
        WriteFloatBE(bw, lightning.branchSpeed);
        WriteUInt32BE(bw, lightning.mainTexture);
        WriteUInt32BE(bw, lightning.branchTexture);
        WriteInt32BE(bw, lightning.damage);
        WriteFloatBE(bw, lightning.knockBackSpeed);
        WriteUInt32BE(bw, lightning.sound);
        WriteUInt32BE(bw, lightning.soundHit1);
        WriteUInt32BE(bw, lightning.soundHit2);
        WriteUInt32BE(bw, lightning.followStart);
        WriteUInt32BE(bw, lightning.followEnd);
        WriteUInt32BE(bw, lightning.collisionEnabled);

        return ms.ToArray();
    }

    public override string GetFolderName() { return "Lightning"; }
}

public class effect_Lightning
{
    public xVec3 start { get; set; }
    public xVec3 end { get; set; }
    public xColor color { get; set; }
    public float thickness { get; set; }
    public float branchSpeed { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint mainTexture { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint branchTexture { get; set; }
    public int damage { get; set; }
    public float knockBackSpeed { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint sound { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint soundHit1 { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint soundHit2 { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint followStart { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint followEnd { get; set; }
    public uint collisionEnabled { get; set; }
}