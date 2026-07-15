using System.IO;
using System.Linq;
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
            lights[i] = new xLightKitLight()
            {
                type = ReadUInt32BE(br),
                color = ReadColorBE(br),
                unk_1 = ReadVector3BE(br),
                unk_2 = ReadFloatBE(br),
                unk_3 = ReadVector3BE(br),
                unk_4 = ReadFloatBE(br),
                unk_5 = ReadVector3BE(br),
                unk_6 = ReadFloatBE(br),
                unk_7 = ReadVector3BE(br),
                unk_8 = ReadFloatBE(br),
                radius = ReadFloatBE(br),
                angle = ReadFloatBE(br)
            };
        }

        return new LKIT
        {
            tagID = tagID,
            groupID = groupID,
            lightCount = lightCount,
            lightList = lightList,
            lights = lights
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
            WriteVector3BE(bw, light.unk_1);
            WriteFloatBE(bw, light.unk_2);
            WriteVector3BE(bw, light.unk_3);
            WriteFloatBE(bw, light.unk_4);
            WriteVector3BE(bw, light.unk_5);
            WriteFloatBE(bw, light.unk_6);
            WriteVector3BE(bw, light.unk_7);
            WriteFloatBE(bw, light.unk_8);
            WriteFloatBE(bw, light.radius);
            WriteFloatBE(bw, light.angle);
        }
        //prob gonna cause issues when editing.. but im NOT reversing a renderware sdk piece for this dumb project...
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
    public xVec3 unk_1 { get; set; }
    public float unk_2 { get; set; }
    public xVec3 unk_3 { get; set; }
    public float unk_4 { get; set; }
    public xVec3 unk_5 { get; set; }
    public float unk_6 { get; set; }
    public xVec3 unk_7 { get; set; }
    public float unk_8 { get; set; }
    public float radius { get; set; }
    public float angle { get; set; }
    //rest is unknown but seems to be renderware struct i dont know what the types would be...
}