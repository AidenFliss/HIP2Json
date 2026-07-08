using System;
using System.IO;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class CONDParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        int constNum = ReadInt32BE(br);
        uint exprl = ReadUInt32BE(br);
        int op = ReadInt32BE(br);
        uint valueAsset = ReadUInt32BE(br);

        return new COND
        {
            constNum = constNum,
            exprl = exprl,
            op = Enum.IsDefined(typeof(Operation), op) ? (Operation)op : Operation.UNKNOWN,
            value_asset = valueAsset,
        };
    }

    public override object Serialize(object obj)
    {
        COND cond = (COND)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteInt32BE(bw, cond.constNum);
        WriteUInt32BE(bw, cond.exprl);
        WriteInt32BE(bw, (int)cond.op);
        WriteUInt32BE(bw, cond.value_asset);
        
        return ms.ToArray();
    }
}

public class COND
{
    public int constNum { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint exprl { get; set; }
    public Operation op { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint value_asset { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Operation : int
{
    EQUAL_TO = 0,
    GREATER_THAN = 1,
    LESS_THAN = 2,
    GREATER_THAN_OR_EQUAL_TO = 3,
    LESS_THAN_OR_EQUAL_TO = 4,
    NOT_EQUAL_TO = 5,
    UNKNOWN = 255
}