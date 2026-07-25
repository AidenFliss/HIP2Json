using System;
using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class BUTNParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        uint modelPressedInfoID = ReadUInt32BE(br);
        int actMethod = ReadInt32BE(br);

        ButtonType type = Enum.IsDefined(typeof(ButtonType), actMethod) ? (ButtonType)actMethod : ButtonType.Button;

        return new BUTN
        {
            modelPressedInfoID = modelPressedInfoID,
            actMethod = type,
            initButtonState = ReadInt32BE(br),
            isReset = ReadInt32BE(br),
            resetDelay = ReadFloatBE(br),
            buttonActFlags = (ButtonHitmask)ReadInt32BE(br),
            motion = ReadMotion(br),
        };
    }

    public override object Serialize(object obj)
    {
        BUTN butn = (BUTN)obj;

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, butn.modelPressedInfoID);
        WriteInt32BE(bw, (int)butn.actMethod);
        WriteInt32BE(bw, butn.initButtonState);
        WriteInt32BE(bw, butn.isReset);
        WriteFloatBE(bw, butn.resetDelay);
        WriteInt32BE(bw, (int)butn.buttonActFlags);
        WriteMotion(bw, butn.motion);

        return ms.ToArray();
    }
}

public class BUTN
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint modelPressedInfoID { get; set; }
    public ButtonType actMethod { get; set; }
    public int initButtonState { get; set; }
    public int isReset { get; set; }
    public float resetDelay { get; set; }
    public ButtonHitmask buttonActFlags { get; set; }
    public xMotion motion { get; set; }
}
