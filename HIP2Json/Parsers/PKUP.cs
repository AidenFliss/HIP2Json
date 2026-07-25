using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

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
