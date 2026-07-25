using System.IO;
using System.Linq;

namespace HIP2Json;

public sealed class ALSTParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        return new ALST { ids = Enumerable.Range(0, 10).Select(_ => ReadUInt32BE(br)).ToArray() };
    }

    public override object Serialize(object obj)
    {
        ALST alst = (ALST)obj;

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        foreach (uint id in alst.ids)
            WriteUInt32BE(bw, id);

        return ms.ToArray();
    }
}

public class ALST
{
    public uint[] ids { get; set; }
}
