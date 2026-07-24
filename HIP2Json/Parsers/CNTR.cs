using System.IO;

namespace HIP2Json;

public sealed class CNTRParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        return new CNTR
        {
            count = ReadInt16BE(br)
        };
    }

    public override object Serialize(object obj)
    {
        CNTR cntr = (CNTR)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteInt16BE(bw, cntr.count);
        bw.Write(new byte[1]);
        
        return ms.ToArray();
    }
}

public class CNTR
{
    public short count { get; set; }
}