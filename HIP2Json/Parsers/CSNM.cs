using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class CSNMParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        uint cutsceneAssetID = ReadUInt32BE(br);
        uint flags = ReadUInt32BE(br);
        float interpSpeed = ReadFloatBE(br);

        uint uSubtitlesID = 0;
        if (Program.CurrentGame != GameType.BFBB)
            uSubtitlesID = ReadUInt32BE(br);

        float[] startTime = Enumerable.Range(0, 14)
                    .Select(_ => ReadFloatBE(br))
                    .ToArray();
        float[] endTime = Enumerable.Range(0, 14)
                    .Select(_ => ReadFloatBE(br))
                    .ToArray();
        uint[] emitID = Enumerable.Range(0, 14)
                    .Select(_ => ReadUInt32BE(br))
                    .ToArray();

        return new CSNM
        {
            cutsceneAssetID = cutsceneAssetID,
            flags = flags,
            interpSpeed = interpSpeed,
            uSubtitlesID = uSubtitlesID,
            startTime = startTime,
            endTime = endTime,
            emitID = emitID
        };
    }

    public override object Serialize(object obj)
    {
        CSNM csnm = (CSNM)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, csnm.cutsceneAssetID);
        WriteUInt32BE(bw, csnm.flags);
        WriteFloatBE(bw, csnm.interpSpeed);
        if (Program.CurrentGame != GameType.BFBB)
            WriteUInt32BE(bw, csnm.uSubtitlesID);
        foreach (var time in csnm.startTime)
            WriteFloatBE(bw, time);
        foreach (var time in csnm.endTime)
            WriteFloatBE(bw, time);
        foreach (var emit in csnm.emitID)
            WriteUInt32BE(bw, emit);
        
        return ms.ToArray();
    }
}

public class CSNM
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint cutsceneAssetID { get; set; }
    public uint flags { get; set; }
    public float interpSpeed { get; set; }
    public uint uSubtitlesID { get; set; } // movie only
    public float[] startTime { get; set; }
    public float[] endTime { get; set; }
    [JsonConverter(typeof(AssetIDArrayConverter))]
    public uint[] emitID { get; set; }
}