using System.IO;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class BUTNParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        return new BUTN
        {
            modelPressedInfoID = ReadUInt32BE(br),
            actMethod = ReadInt32BE(br),
            initButtonState = ReadInt32BE(br),
            isReset = ReadInt32BE(br),
            resetDelay = ReadFloatBE(br),
            buttonActFlags = ReadInt32BE(br),
            motion = ReadMotion(br),
        };
    }

    public override object Serialize(object obj)
    {
        BUTN butn = (BUTN)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, butn.modelPressedInfoID);
        WriteInt32BE(bw, butn.actMethod);
        WriteInt32BE(bw, butn.initButtonState);
        WriteInt32BE(bw, butn.isReset);
        WriteFloatBE(bw, butn.resetDelay);
        WriteInt32BE(bw, butn.buttonActFlags);
        WriteMotion(bw, butn.motion);
        
        return ms.ToArray();
    }
}

public class BUTN
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint modelPressedInfoID { get; set; }
    public int actMethod { get; set; }
    public int initButtonState { get; set; }
    public int isReset { get; set; }
    public float resetDelay { get; set; }
    public int buttonActFlags { get; set; }
    public xMotion motion { get; set; }
}