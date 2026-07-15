using System.IO;

namespace HIP2Json;

public sealed class PLYRParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        return new PLYR
        {
        };
    }

    public override object Serialize(object obj)
    {
        PLYR plyr = (PLYR)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        return ms.ToArray();
    }
}

public class PLYR
{
}