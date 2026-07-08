using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class LITEParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        return new LITE
        {
            lightType = ReadByte(br),
            lightEffect = ReadByte(br),
            pad_0 = ReadByte(br),
            pad_1 = ReadByte(br),
            lightFlags = ReadUInt32BE(br),
            lightColor = Enumerable.Range(0, 3)
                .Select(_ => ReadFloatBE(br))
                .ToArray(),
            lightDir = ReadVector3BE(br),
            lightConeAngle = ReadFloatBE(br),
            lightSphere_center = ReadVector3BE(br),
            lightSphere_r = ReadFloatBE(br)
        };
    }

    public override object Serialize(object obj)
    {
        LITE lite = (LITE)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteByte(bw, lite.lightType);
        WriteByte(bw, lite.lightEffect);
        WriteByte(bw, lite.pad_0);
        WriteByte(bw, lite.pad_1);
        WriteUInt32BE(bw, lite.lightFlags);
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
    public byte lightType { get; set; }
    public byte lightEffect { get; set; }
    public byte pad_0 { get; set; }
    public byte pad_1 { get; set; }
    public uint lightFlags { get; set; }
    public float[] lightColor { get; set; }
    public xVec3 lightDir { get; set; }
    public float lightConeAngle { get; set; }
    public xVec3 lightSphere_center { get; set; }
    public float lightSphere_r { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint attachID { get; set; }
}