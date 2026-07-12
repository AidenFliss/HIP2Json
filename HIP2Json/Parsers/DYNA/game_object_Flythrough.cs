using System.IO;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class game_object_FlythroughParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version, string dynaType)
    {
        return new game_object_Flythrough
        {
            flyID = ReadUInt32BE(br)
        };
    }

    public override byte[] Serialize(object obj, short version, string dynaType)
    {
        game_object_Flythrough flythrough = (game_object_Flythrough)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, flythrough.flyID);

        return ms.ToArray();
    }

    public override string GetFolderName() { return "FlythroughObject"; }
}

public class game_object_Flythrough
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint flyID { get; set; }
}