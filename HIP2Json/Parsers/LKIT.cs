using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class LKITParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        uint tagID = ReadUInt32BE(br);
        uint groupID = ReadUInt32BE(br);
        uint lightCount = ReadUInt32BE(br);
        uint lightList = ReadUInt32BE(br);

        xLightKitLight[] lights = new xLightKitLight[lightCount];

        for (uint i = 0; i < lightCount; i++)
        {
            uint type = ReadUInt32BE(br);
            float colorR = ReadFloatBE(br);
            float colorG = ReadFloatBE(br);
            float colorB = ReadFloatBE(br);
            float colorA = ReadFloatBE(br);
            xVec4 unknown02 = ReadVector4BE(br);
            xVec4 unknown03 = ReadVector4BE(br);
            xVec4 direction = ReadVector4BE(br);
            xVec4 unknown05 = ReadVector4BE(br);
            float radius = ReadFloatBE(br);
            float angle = ReadFloatBE(br);
            float platLight = ReadFloatBE(br);

            lights[i] = new xLightKitLight()
            {
                type = type,
                colorR = colorR,
                colorG = colorG,
                colorB = colorB,
                colorA = colorA,
                unknown02 = unknown02,
                unknown03 = unknown03,
                direction = direction,
                unknown05 = unknown05,
                radius = radius,
                angle = angle,
                platLight = platLight,
            };
        }

        return new LKIT
        {
            tagID = tagID,
            groupID = groupID,
            lightCount = lightCount,
            lightList = lightList,
            lights = lights,
        };
    }

    public override object Serialize(object obj)
    {
        LKIT lkit = (LKIT)obj;

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, lkit.tagID);
        WriteUInt32BE(bw, lkit.groupID);
        WriteUInt32BE(bw, lkit.lightCount);
        WriteUInt32BE(bw, lkit.lightList);

        foreach (var light in lkit.lights)
        {
            WriteUInt32BE(bw, light.type);
            WriteFloatBE(bw, light.colorR);
            WriteFloatBE(bw, light.colorG);
            WriteFloatBE(bw, light.colorB);
            WriteFloatBE(bw, light.colorA);

            WriteVector4BE(bw, light.unknown02);
            WriteVector4BE(bw, light.unknown03);
            WriteVector4BE(bw, light.direction);
            WriteVector4BE(bw, light.unknown05);

            WriteFloatBE(bw, light.radius);
            WriteFloatBE(bw, light.angle);
            WriteFloatBE(bw, light.platLight);
        }

        return ms.ToArray();
    }
}

public class LKIT
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint tagID { get; set; }

    [JsonConverter(typeof(AssetIDConverter))]
    public uint groupID { get; set; }
    public uint lightCount { get; set; }
    public uint lightList { get; set; }
    public xLightKitLight[] lights { get; set; }
}

public class xLightKitLight
{
    public uint type { get; set; }
    public float colorR { get; set; }
    public float colorG { get; set; }
    public float colorB { get; set; }
    public float colorA { get; set; }
    public xVec4 unknown02 { get; set; }
    public xVec4 unknown03 { get; set; }
    public xVec4 direction { get; set; }
    public xVec4 unknown05 { get; set; }
    public float radius { get; set; }
    public float angle { get; set; }
    public float platLight { get; set; }
}
