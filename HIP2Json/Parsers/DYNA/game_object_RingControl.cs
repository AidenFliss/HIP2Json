using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class game_object_RingControlParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version, string dynaType)
    {
        int player = ReadInt32BE(br);
        uint modelForRings = ReadUInt32BE(br);
        float defaultWarningTime = ReadFloatBE(br);
        uint ringCount = ReadUInt32BE(br);
        uint notUsedOffset = ReadUInt32BE(br);

        uint[] sounds = Enumerable.Range(0, 4)
                    .Select(_ => ReadUInt32BE(br))
                    .ToArray();
        
        uint numNextRingsToShow = ReadUInt32BE(br);

        uint[] ringList = Enumerable.Range(0, (int)ringCount)
                    .Select(_ => ReadUInt32BE(br))
                    .ToArray();

        return new game_object_RingControl
        {
            player = player,
            modelForRings = modelForRings,
            defaultWarningTime = defaultWarningTime,
            ringCount = ringCount,
            notUsedOffset = notUsedOffset,
            sounds = sounds,
            numNextRingsToShow = numNextRingsToShow,
            ringList = ringList
        };
    }

    public override byte[] Serialize(object obj, short version, string dynaType)
    {
        game_object_RingControl ringControl = (game_object_RingControl)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteInt32BE(bw, ringControl.player);
        WriteUInt32BE(bw, ringControl.modelForRings);
        WriteFloatBE(bw, ringControl.defaultWarningTime);
        WriteUInt32BE(bw, ringControl.ringCount);
        WriteUInt32BE(bw, ringControl.notUsedOffset);

        foreach (var sound in ringControl.sounds)
            WriteUInt32BE(bw, sound);

        WriteUInt32BE(bw, ringControl.numNextRingsToShow);

        foreach (var ring in ringControl.ringList)
            WriteUInt32BE(bw, ring);
        
        return ms.ToArray();
    }

    public override string GetFolderName() { return "RingControl"; }
}

public class game_object_RingControl
{
    public int player { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint modelForRings { get; set; }
    public float defaultWarningTime { get; set; }
    public uint ringCount { get; set; }
    public uint notUsedOffset { get; set; }
    [JsonConverter(typeof(AssetIDArrayConverter))]
    public uint[] sounds { get; set; }
    public uint numNextRingsToShow { get; set; }
    [JsonConverter(typeof(AssetIDArrayConverter))]
    public uint[] ringList { get; set; }
}