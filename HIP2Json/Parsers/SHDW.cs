using System;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class SHDWParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        uint entryCount = ReadUInt32BE(br);

        ShadowInfoEntry[] entries = new ShadowInfoEntry[entryCount];

        for (int i = 0; i < entryCount; i++)
        {
            entries[i] = new ShadowInfoEntry
            {
                modelAssetID = ReadUInt32BE(br),
                shadowAssetID = ReadUInt32BE(br),
                unknown = ReadUInt32BE(br),
            };
        }

        return new SHDW
        {
            entryCount = entryCount,
            entries = entries,
        };
    }

    public override object Serialize(object obj)
    {
        SHDW shdw = (SHDW)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, shdw.entryCount);
        foreach (var entry in shdw.entries)
        {
            WriteUInt32BE(bw, entry.modelAssetID);
            WriteUInt32BE(bw, entry.shadowAssetID);
            WriteUInt32BE(bw, entry.unknown);
        }

        return ms.ToArray();
    }
}

public class SHDW
{
    public uint entryCount { get; set; }
    public ShadowInfoEntry[] entries { get; set; }
}

public class ShadowInfoEntry
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint modelAssetID { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint shadowAssetID { get; set; }
    public uint unknown { get; set; }
}