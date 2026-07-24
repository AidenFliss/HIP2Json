using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class effect_spotlightParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version, string dynaType)
    {
        uint flags = ReadUInt32BE(br);
        uint attach_to = ReadUInt32BE(br);
        uint target = ReadUInt32BE(br);
        byte attach_bone = ReadByte(br);
        byte target_bone = ReadByte(br);
        br.ReadBytes(2);
        float radius = ReadFloatBE(br);
        float view_angle = ReadFloatBE(br);
        float max_dist = ReadFloatBE(br);
        xColor lightColor = ReadColor(br);
        xColor auraColor = ReadColor(br);
        uint flareTexture = ReadUInt32BE(br);
        float size_min = ReadFloatBE(br);
        float size_max = ReadFloatBE(br);
        byte glow_min = ReadByte(br);
        byte glow_max = ReadByte(br);
        br.ReadBytes(2);

        return new effect_spotlight
        {
            flags = flags,
            attach_to = attach_to,
            target = target,
            attach_bone = attach_bone,
            target_bone = target_bone,
            radius = radius,
            view_angle = view_angle,
            max_dist = max_dist,
            lightColor = lightColor,
            auraColor = auraColor,
            flareTexture = flareTexture,
            size_min = size_min,
            size_max = size_max,
            glow_min = glow_min,
            glow_max = glow_max
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
        bw.Write(new byte[2]);
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
        bw.Write(new byte[2]);

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
}