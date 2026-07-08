using System.IO;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class FOGParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        return new FOG
        {
            bkgndColor = ReadColorBE(br),
            fogColor = ReadColorBE(br),
            fogDensity = ReadFloatBE(br),
            fogStart = ReadFloatBE(br),
            fogStop = ReadFloatBE(br),
            transitionTime = ReadFloatBE(br),
            fogType = ReadByte(br),
            padFog0 = ReadByte(br),
            padFog1 = ReadByte(br),
            padFog2 = ReadByte(br),
        };
    }

    public override object Serialize(object obj)
    {
        FOG fog = (FOG)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteColorBE(bw, fog.bkgndColor);
        WriteColorBE(bw, fog.fogColor);
        WriteFloatBE(bw, fog.fogDensity);
        WriteFloatBE(bw, fog.fogStart);
        WriteFloatBE(bw, fog.fogStop);
        WriteFloatBE(bw, fog.transitionTime);
        WriteByte(bw, fog.fogType);
        WriteByte(bw, fog.padFog0);
        WriteByte(bw, fog.padFog1);
        WriteByte(bw, fog.padFog2);
        
        return ms.ToArray();
    }
}

public class FOG
{
    public xColor bkgndColor { get; set; }
    public xColor fogColor { get; set; }
    public float fogDensity { get; set; }
    public float fogStart { get; set; }
    public float fogStop { get; set; }
    public float transitionTime { get; set; }
    public byte fogType { get; set; }
    public byte padFog0 { get; set; }
    public byte padFog1 { get; set; }
    public byte padFog2 { get; set; }
}