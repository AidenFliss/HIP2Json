using System;
using System.IO;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class PKUPParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        return new PKUP
        {
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