using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class VILParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        return new VIL
        {
            npcFlags = ReadInt32BE(br),
            npcModel = ReadUInt32BE(br),
            npcProps = ReadUInt32BE(br),
            movePoint = ReadUInt32BE(br),
            taskWidgetPrime = ReadUInt32BE(br),
            taskWidgetSecond = ReadUInt32BE(br)
        };
    }

    public override object Serialize(object obj)
    {
        VIL vil = (VIL)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteInt32BE(bw, vil.npcFlags);
        WriteUInt32BE(bw, vil.npcModel);
        WriteUInt32BE(bw, vil.npcProps);
        WriteUInt32BE(bw, vil.movePoint);
        WriteUInt32BE(bw, vil.taskWidgetPrime);
        WriteUInt32BE(bw, vil.taskWidgetSecond);

        return ms.ToArray();
    }
}

public class VIL
{
    public int npcFlags { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint npcModel { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint npcProps { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint movePoint { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint taskWidgetPrime { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint taskWidgetSecond { get; set; }
}