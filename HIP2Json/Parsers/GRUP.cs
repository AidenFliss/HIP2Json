using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class GRUPParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        ushort itemCount = ReadUInt16BE(br);

        return new GRUP
        {
            itemCount = itemCount,
            groupFlags = ReadUInt16BE(br),
            assets = Enumerable.Range(0, itemCount)
                .Select(_ => ReadUInt32BE(br))
                .ToArray(),
        };
    }

    public override object Serialize(object obj)
    {
        GRUP grup = (GRUP)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt16BE(bw, grup.itemCount);
        WriteUInt16BE(bw, grup.groupFlags);
        foreach (var asset in grup.assets)
            WriteUInt32BE(bw, asset);
        
        return ms.ToArray();
    }
}

public class GRUP
{
    public ushort itemCount { get; set; }
    public ushort groupFlags { get; set; }
    [JsonConverter(typeof(AssetIDArrayConverter))]
    public uint[] assets { get; set; }
}