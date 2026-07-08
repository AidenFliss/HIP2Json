using System.IO;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class MAPRParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        uint id = ReadUInt32BE(br);
        uint count = ReadUInt32BE(br);

        var entries = new zMaterialMapEntry[count];

        for (int i = 0; i < count; i++)
        {
            entries[i] = new zMaterialMapEntry
            {
                surfaceAssetID = ReadUInt32BE(br),
                materialIndex = ReadUInt32BE(br),
            };
        }

        return new MAPR
        {
            id = id,
            count = count,
            materialMapEntries = entries
        };
    }

    public override object Serialize(object obj)
    {
        MAPR mapr = (MAPR)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, mapr.id);
        WriteUInt32BE(bw, mapr.count);
        foreach (var entry in mapr.materialMapEntries)
        {
            WriteUInt32BE(bw, entry.surfaceAssetID);
            WriteUInt32BE(bw, entry.materialIndex);
        }
        
        return ms.ToArray();
    }
}

public class MAPR
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint id { get; set; }
    public uint count { get; set; }
    public zMaterialMapEntry[] materialMapEntries { get; set; }
}


public class zMaterialMapEntry
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint surfaceAssetID { get; set; }
    public uint materialIndex { get; set; }
}