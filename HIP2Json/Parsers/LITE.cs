using System;
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
            lightType = (LightType)ReadByte(br),
            lightEffect = (LightEffect)ReadByte(br),
            pad_0 = ReadByte(br),
            pad_1 = ReadByte(br),
            lightFlags = (LightFlags)ReadUInt32BE(br),
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

        WriteByte(bw, (byte)lite.lightType);
        WriteByte(bw, (byte)lite.lightEffect);
        WriteByte(bw, lite.pad_0);
        WriteByte(bw, lite.pad_1);
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
    public byte pad_0 { get; set; }
    public byte pad_1 { get; set; }
    public LightFlags lightFlags { get; set; }
    public float[] lightColor { get; set; }
    public xVec3 lightDir { get; set; }
    public float lightConeAngle { get; set; }
    public xVec3 lightSphere_center { get; set; }
    public float lightSphere_r { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint attachID { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LightType : byte
{
    Point = 0,
    Spot = 1,
    Point2 = 2,
    Point3 = 3
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LightEffect : byte
{
    None = 0,
    NoneAlt = 1,
    FlickerSlow = 2,
    Flicker = 3,
    FlickerErratic = 4,
    StrobeSlow = 5,
    Strobe = 6,
    StrobeFast = 7,
    DimSlow = 8,
    Dim = 9,
    DimFast = 10,
    HalfDimSlow = 11,
    HalfDim = 12,
    HalfDimFast = 13,
    RandomColorSlow = 14,
    RandomColor = 15,
    RandomColorFast = 16,
    Cauldron = 17
}

[Flags]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LightFlags : uint
{
    None = 0,
    Environment = 0x08,
    On = 0x20
}