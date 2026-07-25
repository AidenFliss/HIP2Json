using System;
using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class CONDParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        int constNum = ReadInt32BE(br);
        uint rawExprl = ReadUInt32BE(br);

        object exprl = Program.CurrentGame switch
        {
            GameType.BFBB => (ConditionalVariableBFBB)rawExprl,
            GameType.TSSM => (ConditionalVariableTSSM)rawExprl,
            _ => rawExprl
        };

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

        uint rawExprl = cond.exprl switch
        {
            ConditionalVariableBFBB bfbb => (uint)bfbb,
            ConditionalVariableTSSM tssm => (uint)tssm,
            uint val => val,
            _ => 0
        };

        WriteUInt32BE(bw, rawExprl);
        WriteInt32BE(bw, (int)cond.op);
        WriteUInt32BE(bw, cond.value_asset);

        return ms.ToArray();
    }
}

public class COND
{
    public int constNum { get; set; }
    [JsonConverter(typeof(GameEnumConverter<ConditionalVariableBFBB, ConditionalVariableTSSM>))]
    public object exprl { get; set; }
    public Operation op { get; set; }

    [JsonConverter(typeof(AssetIDConverter))]
    public uint value_asset { get; set; }
}
