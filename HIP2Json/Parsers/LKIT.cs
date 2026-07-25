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
            xColor color = ReadColorBE(br);
            xVec4 matrixRow0 = ReadVector4BE(br);
            xVec4 matrixRow1 = ReadVector4BE(br);
            xVec4 matrixRow2 = ReadVector4BE(br);
            xVec4 matrixRow3 = ReadVector4BE(br);
            float radius = ReadFloatBE(br);
            float angle = ReadFloatBE(br);
            br.ReadBytes(4);

            lights[i] = new xLightKitLight()
            {
                type = type,
                color = color,
                matrixRow0 = matrixRow0,
                matrixRow1 = matrixRow1,
                matrixRow2 = matrixRow2,
                matrixRow3 = matrixRow3,
                radius = radius,
                angle = angle,
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
            WriteColorBE(bw, light.color);

            WriteVector4BE(bw, light.matrixRow0);
            WriteVector4BE(bw, light.matrixRow1);
            WriteVector4BE(bw, light.matrixRow2);
            WriteVector4BE(bw, light.matrixRow3);

            WriteFloatBE(bw, light.radius);
            WriteFloatBE(bw, light.angle);
            bw.Write(new byte[4]);
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
    public xColor color { get; set; }
    public xVec4 matrixRow0 { get; set; }
    public xVec4 matrixRow1 { get; set; }
    public xVec4 matrixRow2 { get; set; }
    public xVec4 matrixRow3 { get; set; }
    public float radius { get; set; }
    public float angle { get; set; } //4 bytes of 00 right here assigned at runtime
}
