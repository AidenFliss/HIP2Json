using System;
using System.IO;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

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

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ConditionalVariableBFBB : uint
{
    SoundMode = 0x29600EB0,
    MusicVolume = 0x84D4A26D,
    SfxVolume = 0x1E0EEB55,
    MemoryCardAvailable = 0x42453758,
    VibrationEnabled = 0x3B93C93F,
    SceneLetter = 0x704D04A9,
    Room = 0x0B11B427,
    CurrentLevelCollectable = 0x9653DA31,
    PatsSocks = 0x18249056,
    TotalPatsSocks = 0xD1FEEEE2,
    ShinyObjects = 0xD6FCCFE7,
    GoldenSpatulas = 0xC7E0F71C,
    CurrentDate = 0x9482683D,
    CurrentHour = 0x950F49B7,
    CurrentMinute = 0xBD2884E7,
    CounterValue = 0x4329EFFD,
    IsEnabled = 0xA6956B3F,
    IsVisible = 0x1E42996C,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ConditionalVariableTSSM : uint
{
    SoundMode = 0x29600EB0,
    MusicVolume = 0x84D4A26D,
    SfxVolume = 0x1E0EEB55,
    MemoryCardAvailable = 0x42453758,
    VibrationEnabled = 0x3B93C93F,
    SubtitlesEnabled = 0xD1A7DE2C,
    SceneLetter = 0x704D04A9,
    Room = 0x0B11B427,
    CurrentDate = 0x9482683D,
    CurrentHour = 0x950F49B7,
    CurrentMinute = 0xBD2884E7,
    CounterValue = 0x4329EFFD,
    IsEnabled = 0xA6956B3F,
    IsVisible = 0x1E42996C,
    TimerSecondsLeft = 0x6897B48B,
    TimerMillisecondsLeft = 0xF4FE2282,
    IsMnus = 0x649FA12A,
    DemoType = 0x0B9F22CF,
    GoofyGooberTokens = 0x43DD1E00,
    ManlinessPoints = 0xD8A29291,
    LevelTreasureChests = 0xFE31C583,
    PlayerCurrentHealth = 0x25CD9F4A,
    IsReferenceNull = 0x1F5BAA4D,
    AlwaysPortal = 0x5B85F809,
}