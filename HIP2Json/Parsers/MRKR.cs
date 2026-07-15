using System.IO;

namespace HIP2Json;

public sealed class MRKRParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        return new MRKR
        {
            pos = ReadVector3BE(br)
        };
    }

    public override object Serialize(object obj)
    {
        MRKR mrkr = (MRKR)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteVector3BE(bw, mrkr.pos);

        return ms.ToArray();
    }
}

public class MRKR
{
    public xVec3 pos { get; set; }
}