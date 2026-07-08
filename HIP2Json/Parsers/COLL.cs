using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class COLLParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        uint sTableCount = ReadUInt32BE(br);

        zCollGeomTable[] entries = new zCollGeomTable[sTableCount];
        for (uint i = 0; i < sTableCount; i++)
        {
            entries[i] = new zCollGeomTable
            {
                baseModel = ReadUInt32BE(br),
                colModel = ReadUInt32BE(br),
                camcolModel = ReadUInt32BE(br)
            };
        }

        return new COLL
        {
            sTableCount = sTableCount,
            entries = entries
        };
    }

    public override object Serialize(object obj)
    {
        COLL coll = (COLL)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, coll.sTableCount);
        foreach (var entry in coll.entries)
        {
            WriteUInt32BE(bw, entry.baseModel);
            WriteUInt32BE(bw, entry.colModel);
            WriteUInt32BE(bw, entry.camcolModel);
        }
        
        return ms.ToArray();
    }
}

public class COLL
{
    public uint sTableCount { get; set; }
    public zCollGeomTable[] entries { get; set; }
}

public class zCollGeomTable
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint baseModel { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint colModel { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint camcolModel { get; set; }
}