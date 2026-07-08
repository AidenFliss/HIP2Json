using System.IO;

namespace PortHeavyIronGameRewrite;

public sealed class DPATParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        return new DPAT
        {
        }; //dpat is just events, so nothing needed but a placeholder to allow it to be outputted in json
    }

    public override object Serialize(object obj)
    {
        DPAT dpat = (DPAT)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        //just events, so just write 0 bytes, yeah, real.
        
        return ms.ToArray();
    }
}

public class DPAT
{
}