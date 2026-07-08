using System.IO;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class game_object_bungee_dropParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version)
    {
        return new game_object_bungee_drop
        {
            marker = ReadUInt32BE(br),
            set_view_angl = ReadUInt32BE(br),
            view_angle = ReadFloatBE(br),
        };
    }

    public override byte[] Serialize(object obj, short version)
    {
        game_object_bungee_drop bungeeDrop = (game_object_bungee_drop)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, bungeeDrop.marker);
        WriteUInt32BE(bw, bungeeDrop.set_view_angl);
        WriteFloatBE(bw, bungeeDrop.view_angle);

        return ms.ToArray();
    }

    public override string GetFolderName() { return "BungeeDrop"; }
}

public class game_object_bungee_drop
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint marker { get; set; }
    public uint set_view_angl { get; set; } //removed the e bc c# is being very c# right now
    public float view_angle { get; set; }
}