using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class FOGParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        xColor bkgndColor = ReadColorBE(br);
        xColor fogColor = ReadColorBE(br);
        float fogDensity = ReadFloatBE(br);
        float fogStart = ReadFloatBE(br);
        float fogStop = ReadFloatBE(br);
        float transitionTime = ReadFloatBE(br);
        byte fogType = ReadByte(br);
        br.ReadBytes(3);

        return new FOG
        {
            bkgndColor = bkgndColor,
            fogColor = fogColor,
            fogDensity = fogDensity,
            fogStart = fogStart,
            fogStop = fogStop,
            transitionTime = transitionTime,
            fogType = fogType
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
        bw.Write(new byte[3]);
        
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
}