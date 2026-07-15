using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class effect_water_bodyParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version, string dynaType)
    {
        return new effect_water_body
        {
            flags = ReadUInt32BE(br),
            motion_type = ReadUInt32BE(br),
            body = ReadUInt32BE(br),
            facade_refract = ReadUInt32BE(br),
            facade_reflect = ReadUInt32BE(br),
            light_dir = ReadUInt32BE(br),
        };
    }

    public override byte[] Serialize(object obj, short version, string dynaType)
    {
        effect_water_body waterBody = (effect_water_body)obj;

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, waterBody.flags);
        WriteUInt32BE(bw, waterBody.motion_type);
        WriteUInt32BE(bw, waterBody.body);
        WriteUInt32BE(bw, waterBody.facade_refract);
        WriteUInt32BE(bw, waterBody.facade_reflect);
        WriteUInt32BE(bw, waterBody.light_dir);

        return ms.ToArray();
    }

    public override string GetFolderName() { return "Spotlight"; }
}

public class effect_water_body
{
    public uint flags { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint motion_type { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint body { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint facade_refract { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint facade_reflect { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint light_dir { get; set; }
}