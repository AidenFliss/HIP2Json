using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class LITEParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        LightType lightType = (LightType)ReadByte(br);
        LightEffect lightEffect = (LightEffect)ReadByte(br);

        br.ReadBytes(2);

        LightFlags lightFlags = (LightFlags)ReadUInt32BE(br);
        float[] lightColor = Enumerable.Range(0, 3).Select(_ => ReadFloatBE(br)).ToArray();
        xVec3 lightDir = ReadVector3BE(br);
        float lightConeAngle = ReadFloatBE(br);
        xVec3 lightSphere_center = ReadVector3BE(br);
        float lightSphere_r = ReadFloatBE(br);

        return new LITE
        {
            lightType = lightType,
            lightEffect = lightEffect,
            lightFlags = lightFlags,
            lightColor = lightColor,
            lightDir = lightDir,
            lightConeAngle = lightConeAngle,
            lightSphere_center = lightSphere_center,
            lightSphere_r = lightSphere_r,
        };
    }

    public override object Serialize(object obj)
    {
        LITE lite = (LITE)obj;

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteByte(bw, (byte)lite.lightType);
        WriteByte(bw, (byte)lite.lightEffect);
        bw.Write(new byte[2]);
        WriteUInt32BE(bw, (uint)lite.lightFlags);
        foreach (var color in lite.lightColor)
            WriteFloatBE(bw, color);
        WriteVector3BE(bw, lite.lightDir);
        WriteFloatBE(bw, lite.lightConeAngle);
        WriteVector3BE(bw, lite.lightSphere_center);
        WriteFloatBE(bw, lite.lightSphere_r);

        return ms.ToArray();
    }
}

public class LITE
{
    public LightType lightType { get; set; }
    public LightEffect lightEffect { get; set; }
    public LightFlags lightFlags { get; set; }
    public float[] lightColor { get; set; }
    public xVec3 lightDir { get; set; }
    public float lightConeAngle { get; set; }
    public xVec3 lightSphere_center { get; set; }
    public float lightSphere_r { get; set; }

    [JsonConverter(typeof(AssetIDConverter))]
    public uint attachID { get; set; }
}
