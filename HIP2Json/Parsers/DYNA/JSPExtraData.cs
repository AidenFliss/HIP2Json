using System.IO;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class JSPExtraDataParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version)
    {
        return new JSPExtraData
        {
            jspID = ReadUInt32BE(br),
            groupID = ReadUInt32BE(br),
        };
    }

    public override byte[] Serialize(object obj, short version)
    {
        JSPExtraData jspExtraData = (JSPExtraData)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, jspExtraData.jspID);
        WriteUInt32BE(bw, jspExtraData.groupID);
        
        return ms.ToArray();
    }

    public override string GetFolderName() { return "JSPExtraData"; }
}

public class JSPExtraData
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint jspID { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint groupID { get; set; }
}