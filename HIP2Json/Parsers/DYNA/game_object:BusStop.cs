using System.IO;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class game_object_BusStopParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version, string dynaType)
    {
        return new game_object_BusStop
        {
            marker = ReadUInt32BE(br),
            character = (PlayableCharacter)ReadInt32BE(br),
            cameraID = ReadUInt32BE(br),
            busID = ReadUInt32BE(br),
            delay = ReadFloatBE(br)
        };
    }

    public override byte[] Serialize(object obj, short version, string dynaType)
    {
        game_object_BusStop busStop = (game_object_BusStop)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, busStop.marker);
        WriteInt32BE(bw, (int)busStop.character);
        WriteUInt32BE(bw, busStop.cameraID);
        WriteUInt32BE(bw, busStop.busID);
        WriteFloatBE(bw, busStop.delay);

        return ms.ToArray();
    }

    public override string GetFolderName() { return "BusStop"; }
}

public class game_object_BusStop
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint marker { get; set; }
    public PlayableCharacter character { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint cameraID { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint busID { get; set; }
    public float delay { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PlayableCharacter
{
    Patrick = 0,
    Sandy = 1
}