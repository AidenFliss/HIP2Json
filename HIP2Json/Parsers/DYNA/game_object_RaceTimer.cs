using System.IO;
using System.Linq;

namespace PortHeavyIronGameRewrite;

public sealed class game_object_RaceTimerParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version, string dynaType)
    {
        byte countDown = ReadByte(br);

        byte[] padding = Enumerable.Range(0, 3)
                .Select(_ => ReadByte(br))
                .ToArray();
        
        int startTime = ReadInt32BE(br);
        int warnTime1 = ReadInt32BE(br);
        int warnTime2 = ReadInt32BE(br);
        int warnTime3 = ReadInt32BE(br);

        return new game_object_RaceTimer
        {
            countDown = countDown,
            padding = padding,
            startTime = startTime,
            warnTime1 = warnTime1,
            warnTime2 = warnTime2,
            warnTime3 = warnTime3,
        };
    }

    public override byte[] Serialize(object obj, short version, string dynaType)
    {
        game_object_RaceTimer raceTimer = (game_object_RaceTimer)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteByte(bw, raceTimer.countDown);

        foreach (var pad in raceTimer.padding)
            WriteByte(bw, pad);
        
        WriteInt32BE(bw, raceTimer.startTime);
        WriteInt32BE(bw, raceTimer.victoryTime);
        WriteFloatBE(bw, raceTimer.warnTime1);
        WriteFloatBE(bw, raceTimer.warnTime2);
        WriteFloatBE(bw, raceTimer.warnTime3);
        
        return ms.ToArray();
    }

    public override string GetFolderName() { return "RaceTimer"; }
}

public class game_object_RaceTimer
{
    public byte countDown { get; set; }
    public byte[] padding { get; set; }
    public int startTime { get; set; }
    public int victoryTime { get; set; }
    public float warnTime1 { get; set; }
    public float warnTime2 { get; set; }
    public float warnTime3 { get; set; }
}