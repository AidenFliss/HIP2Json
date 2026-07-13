using System;
using System.IO;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class PKUPParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        br.BaseStream.Seek(assetStart + 0x09, SeekOrigin.Begin);

        byte t = ReadByte(br);

        br.BaseStream.Seek(dataStart, SeekOrigin.Begin);

        return new PKUP
        {
            pickupType = (PickupType)t,
            pickupHash = ReadUInt32BE(br),
            pickupFlags = (PickupFlags)ReadInt16BE(br),
            pickupValue = ReadInt16BE(br),
        };
    }

    public override object Serialize(object obj)
    {
        PKUP pkup = (PKUP)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, pkup.pickupHash);
        WriteInt16BE(bw, (short)pkup.pickupFlags);
        WriteInt16BE(bw, pkup.pickupValue);

        return ms.ToArray();
    }
}

public class PKUP
{
    public PickupType pickupType { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint pickupHash { get; set; }
    public PickupFlags pickupFlags { get; set; }
    public short pickupValue { get; set; }
}

[Flags]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PickupFlags : short //funky math stuff yay
{
    None = 0,
    ReappearAfterCollecting = 1 << 0,
    EnabledOnStart = 1 << 1,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PickupType : byte
{
    Artwork = 0x10,
    Underwear = 0x13,
    RedManlinessPoint = 0x17,
    YellowManlinessPoint = 0x5A,
    GreenManlinessPoint = 0xD9,
    BlueManlinessPoint = 0x4C,
    PurpleManlinessPointOrExtra = 0x8A,
    KrabbyPatty = 0xD1,
    GoofyGooberToken = 0xB7,
    Sock = 0x24,
    SteeringWheel = 0x27,
    Clue = 0x28,
    GoldenUnderwear = 0x2E,
    GreenShinyObject = 0x34,
    YellowShinyObject = 0x3B,
    RedShinyObject = 0x3E,
    SpongeBall = 0x40,
    Savepoint = 0x5C,
    Shovel = 0x80,
    BlueShinyObject = 0x81,
    Snackgate = 0x86,
    PowerCrystal = 0xBB,
    ScoobySnack = 0xBC,
    PurpleShinyObject = 0xCB,
    GoldenSpatula = 0xDD,
    ScoobySnackBox = 0xEC
}