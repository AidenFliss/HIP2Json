using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class PIPTParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        uint entryCount = ReadUInt32BE(br);

        xModelPipeInfo[] entries = new xModelPipeInfo[entryCount];

        for (int i = 0; i < entryCount; i++)
        {
            uint modelAssetID = ReadUInt32BE(br);
            uint subObjectBits = ReadUInt32BE(br);
            uint pipeFlags = ReadUInt32BE(br);

            byte layer = 0;
            byte alphaDiscard = 0;
            if (Program.CurrentGame == GameType.TSSM)
            {
                layer = ReadByte(br);
                alphaDiscard = ReadByte(br);
                br.ReadBytes(2);
            }

            entries[i] = new xModelPipeInfo
            {
                modelAssetID = modelAssetID,
                subObjectBits = subObjectBits,
                pipeFlags = pipeFlags,
                layer = layer,
                alphaDiscard = alphaDiscard
            };
        }

        return new PIPT
        {
            entryCount = entryCount,
            entries = entries,
        };
    }

    public override object Serialize(object obj)
    {
        PIPT pipt = (PIPT)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, pipt.entryCount);
        foreach (var entry in pipt.entries)
        {
            WriteUInt32BE(bw, entry.modelAssetID);
            WriteUInt32BE(bw, entry.subObjectBits);
            WriteUInt32BE(bw, entry.pipeFlags);
            if (Program.CurrentGame == GameType.TSSM)
            {
                WriteByte(bw, entry.layer);
                WriteByte(bw, entry.alphaDiscard);
                bw.Write(new byte[2]);
            }
        }

        return ms.ToArray();
    }
}

public class PIPT
{
    public uint entryCount { get; set; }
    public xModelPipeInfo[] entries { get; set; }
}

public class xModelPipeInfo
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint modelAssetID { get; set; }
    public uint subObjectBits { get; set; }
    public uint pipeFlags { get; set; }
    public byte layer { get; set; }
    public byte alphaDiscard { get; set; }
}