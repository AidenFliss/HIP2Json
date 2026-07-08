using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class game_object_TaxiParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version)
    {
        return new game_object_Taxi
        {
            marker = ReadUInt32BE(br),
            cameraID = ReadUInt32BE(br),
            portalID = ReadUInt32BE(br),
            talkBoxID = ReadUInt32BE(br),
            textID = ReadUInt32BE(br),
            taxiID = ReadUInt32BE(br),
            invTimer = ReadFloatBE(br),
            portalDelay = ReadFloatBE(br),
        };
    }

    public override byte[] Serialize(object obj, short version)
    {
        game_object_Taxi taxi = (game_object_Taxi)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, taxi.marker);
        WriteUInt32BE(bw, taxi.cameraID);
        WriteUInt32BE(bw, taxi.portalID);
        WriteUInt32BE(bw, taxi.talkBoxID);
        WriteUInt32BE(bw, taxi.textID);
        WriteUInt32BE(bw, taxi.taxiID);
        WriteFloatBE(bw, taxi.invTimer);
        WriteFloatBE(bw, taxi.portalDelay);

        return ms.ToArray();
    }

    public override string GetFolderName() { return "Taxi"; }
}

public class game_object_Taxi
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint marker { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint cameraID { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint portalID { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint talkBoxID { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint textID { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint taxiID { get; set; }
    public float invTimer { get; set; }
    public float portalDelay { get; set; }
}