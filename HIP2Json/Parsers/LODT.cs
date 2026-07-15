using System;
using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class LODTParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        int numextra = ReadInt32BE(br);

        zLODTable[] tables = new zLODTable[numextra];

        for (int i = 0; i < numextra; i++)
        {
            uint baseBucket = ReadUInt32BE(br);
            float noRenderDist = ReadFloatBE(br);

            uint flags = 0;
            if (Program.CurrentGame != GameType.BFBB)
                flags = ReadUInt32BE(br);

            uint lodModel1 = ReadUInt32BE(br);
            uint lodModel2 = ReadUInt32BE(br);
            uint lodModel3 = ReadUInt32BE(br);
            float lodDist1 = ReadFloatBE(br);
            float lodDist2 = ReadFloatBE(br);
            float lodDist3 = ReadFloatBE(br);

            tables[i] = new zLODTable()
            {
                baseBucket = baseBucket,
                noRenderDist = noRenderDist,
                flags = flags,
                lodModel1 = lodModel1,
                lodModel2 = lodModel2,
                lodModel3 = lodModel3,
                lodDist1 = lodDist1,
                lodDist2 = lodDist2,
                lodDist3 = lodDist3
            };
        }

        return new LODT
        {
            numextra = numextra,
            lodTables = tables
        };
    }

    public override object Serialize(object obj)
    {
        LODT lodt = (LODT)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteInt32BE(bw, lodt.numextra);
        foreach (var table in lodt.lodTables)
        {
            WriteUInt32BE(bw, table.baseBucket);
            WriteFloatBE(bw, table.noRenderDist);
            if (Program.CurrentGame != GameType.BFBB)
                WriteUInt32BE(bw, table.flags);
            WriteUInt32BE(bw, table.lodModel1);
            WriteUInt32BE(bw, table.lodModel2);
            WriteUInt32BE(bw, table.lodModel3);
            WriteFloatBE(bw, table.lodDist1);
            WriteFloatBE(bw, table.lodDist2);
            WriteFloatBE(bw, table.lodDist3);
        }
        
        return ms.ToArray();
    }
}

public class LODT
{
    public int numextra { get; set; }
    public zLODTable[] lodTables { get; set; }
}

public class zLODTable
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint baseBucket { get; set; }
    public float noRenderDist { get; set; }
    public uint flags { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint lodModel1 { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint lodModel2 { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint lodModel3 { get; set; }
    public float lodDist1 { get; set; }
    public float lodDist2 { get; set; }
    public float lodDist3 { get; set; }
}