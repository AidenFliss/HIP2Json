using System.IO;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class game_object_TeleportParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version)
    {
        uint marker = ReadUInt32BE(br);
        uint opened = ReadUInt32BE(br);
        uint launchAngle = ReadUInt32BE(br);

        uint camAngle = 0;
        if (version == 2 && Program.CurrentGame == GameType.BFBB)
            camAngle = ReadUInt32BE(br); // only in bfbb version 2
        
        uint targetID = ReadUInt32BE(br);

        return new game_object_Teleport
        {
            marker = marker,
            opened = opened,
            launchAngle = launchAngle,
            camAngle = camAngle,
            targetID = targetID,
        };
    }

    public override byte[] Serialize(object obj, short version)
    {
        game_object_Teleport teleport = (game_object_Teleport)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, teleport.marker);
        WriteUInt32BE(bw, teleport.opened);
        WriteUInt32BE(bw, teleport.launchAngle);

        if (version == 2 && Program.CurrentGame == GameType.BFBB)
            WriteUInt32BE(bw, teleport.camAngle);

        WriteUInt32BE(bw, teleport.targetID);

        return ms.ToArray();
    }

    public override string GetFolderName() { return "TeleportBox"; }
}

public class game_object_Teleport
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint marker { get; set; }
    public uint opened { get; set; }
    public uint launchAngle { get; set; }
    public uint camAngle { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint targetID { get; set; }
}