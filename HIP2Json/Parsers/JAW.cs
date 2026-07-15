using System;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class JAWParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        uint entryCount = ReadUInt32BE(br);

        var table = new xJawDataTable[entryCount];

        for (int i = 0; i < entryCount; i++)
        {
            table[i] = new xJawDataTable
            {
                soundHashID = ReadUInt32BE(br),
                dataStart = ReadInt32BE(br),
                dataLength = ReadInt32BE(br)
            };
        }

        long directoryEnd = br.BaseStream.Position;

        var jawData = new xJawData[entryCount];

        for (int i = 0; i < entryCount; i++)
        {
            br.BaseStream.Position = directoryEnd + table[i].dataStart;

            int frameCount = br.ReadInt32();

            jawData[i] = new xJawData
            {
                length = frameCount,
                data = Enumerable.Range(0, frameCount)
                    .Select(_ => (int)ReadByte(br))
                    .ToArray()
            };
        }

        return new JAW
        {
            entryCount = entryCount,
            jawDataTables = table,
            jawData = jawData
        };
    }

    public override object Serialize(object obj)
    {
        JAW jaw = (JAW)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, jaw.entryCount);
        foreach (var table in jaw.jawDataTables)
        {
            WriteUInt32BE(bw, table.soundHashID);
            WriteInt32BE(bw, table.dataStart);
            WriteInt32BE(bw, table.dataLength);
        }
        foreach (var data in jaw.jawData)
        {
            WriteInt32BE(bw, data.length);
            foreach (var value in data.data)
                WriteByte(bw, (byte)value);
        }
        
        return ms.ToArray();
    }
}

public class JAW
{
    public uint entryCount { get; set; }
    public xJawDataTable[] jawDataTables { get; set; }
    public xJawData[] jawData { get; set; }
}

public class xJawDataTable
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint soundHashID { get; set; }
    public int dataStart { get; set; }
    public int dataLength { get; set; }
}

public class xJawData
{
    public int length { get; set; }
    public int[] data { get; set; }
}