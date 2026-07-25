using System;
using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class CONDParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        int constNum = ReadInt32BE(br);

        ConditionalVariableBFBB exprlBFBB = ConditionalVariableBFBB.SoundMode;
        ConditionalVariableTSSM exprlTSSM = ConditionalVariableTSSM.SoundMode;

        if (Program.CurrentGame == GameType.BFBB)
        {
            exprlBFBB = (ConditionalVariableBFBB)ReadUInt32BE(br);
        }
        else if (Program.CurrentGame == GameType.TSSM)
        {
            exprlTSSM = (ConditionalVariableTSSM)ReadUInt32BE(br);
        }

        int op = ReadInt32BE(br);
        uint valueAsset = ReadUInt32BE(br);

        return new COND
        {
            constNum = constNum,
            exprlBFBB = exprlBFBB,
            exprlTSSM = exprlTSSM,
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
        if (Program.CurrentGame == GameType.BFBB)
        {
            WriteUInt32BE(bw, (uint)cond.exprlBFBB);
        }
        else if (Program.CurrentGame == GameType.TSSM)
        {
            WriteUInt32BE(bw, (uint)cond.exprlTSSM);
        }
        WriteInt32BE(bw, (int)cond.op);
        WriteUInt32BE(bw, cond.value_asset);

        return ms.ToArray();
    }
}

public class COND
{
    public int constNum { get; set; }
    public ConditionalVariableBFBB exprlBFBB { get; set; }
    public ConditionalVariableTSSM exprlTSSM { get; set; }
    public Operation op { get; set; }

    [JsonConverter(typeof(AssetIDConverter))]
    public uint value_asset { get; set; }
}
