using System.IO;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class effect_spotlightParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version, string dynaType)
    {
        return new effect_spotlight
        {
            flags = ReadUInt32BE(br),
            attach_to = ReadUInt32BE(br),
            target = ReadUInt32BE(br),
            attach_bone = ReadByte(br),
            target_bone = ReadByte(br),
            pad1 = ReadByte(br),
            pad2 = ReadByte(br),
            radius = ReadFloatBE(br),
            view_angle = ReadFloatBE(br),
            max_dist = ReadFloatBE(br),
            lightColor = ReadColor(br),
            auraColor = ReadColor(br),
            flareTexture = ReadUInt32BE(br),
            size_min = ReadFloatBE(br),
            size_max = ReadFloatBE(br),
            glow_min = ReadByte(br),
            glow_max = ReadByte(br),
            pad3 = ReadByte(br),
            pad4 = ReadByte(br),
        };
    }

    public override byte[] Serialize(object obj, short version, string dynaType)
    {
        effect_spotlight spotlight = (effect_spotlight)obj;

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, spotlight.flags);
        WriteUInt32BE(bw, spotlight.attach_to);
        WriteUInt32BE(bw, spotlight.target);
        WriteByte(bw, spotlight.attach_bone);
        WriteByte(bw, spotlight.target_bone);
        WriteByte(bw, spotlight.pad1);
        WriteByte(bw, spotlight.pad2);
        WriteFloatBE(bw, spotlight.radius);
        WriteFloatBE(bw, spotlight.view_angle);
        WriteFloatBE(bw, spotlight.max_dist);
        WriteColorBE(bw, spotlight.lightColor);
        WriteColorBE(bw, spotlight.auraColor);
        WriteUInt32BE(bw, spotlight.flareTexture);
        WriteFloatBE(bw, spotlight.size_min);
        WriteFloatBE(bw, spotlight.size_max);
        WriteByte(bw, spotlight.glow_min);
        WriteByte(bw, spotlight.glow_max);
        WriteByte(bw, spotlight.pad3);
        WriteByte(bw, spotlight.pad4);

        return ms.ToArray();
    }

    public override string GetFolderName() { return "Spotlight"; }
}

public class effect_spotlight
{
    public uint flags { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint attach_to { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint target { get; set; }
    public byte attach_bone { get; set; }
    public byte target_bone { get; set; }
    public byte pad1 { get; set; }
    public byte pad2 { get; set; }
    public float radius { get; set; }
    public float view_angle { get; set; }
    public float max_dist { get; set; }
    public xColor lightColor { get; set; }
    public xColor auraColor { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint flareTexture { get; set; }
    public float size_min { get; set; }
    public float size_max { get; set; }
    public byte glow_min { get; set; }
    public byte glow_max { get; set; }
    public byte pad3 { get; set; }
    public byte pad4 { get; set; }
}