using System.IO;

namespace PortHeavyIronGameRewrite;

public sealed class TIMRParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        return new TIMR
        {
            seconds = ReadFloatBE(br),
            randomRange = ReadFloatBE(br),
        };
    }

    public override object Serialize(object obj)
    {
        TIMR timr = (TIMR)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteFloatBE(bw, timr.seconds);
        WriteFloatBE(bw, timr.randomRange);
        
        return ms.ToArray();
    }
}

public class TIMR
{
    public float seconds { get; set; }
    public float randomRange { get; set; }
}