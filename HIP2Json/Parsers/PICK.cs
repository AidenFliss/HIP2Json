using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class PICKParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        uint magic = ReadUInt32BE(br);
        uint count = ReadUInt32BE(br);

        zAssetPickup[] pickups = new zAssetPickup[count];

        for (uint i = 0; i < count; i++)
        {
            pickups[i] = new zAssetPickup
            {
                pickupHash = ReadUInt32BE(br),
                pickupType = ReadByte(br),
                pickupIndex = ReadByte(br),
                pickupFlags = ReadUInt16BE(br),
                quantity = ReadUInt32BE(br),
                modelID = ReadUInt32BE(br),
                animID = ReadUInt32BE(br)
            };
        }

        return new PICK
        {
            magic = magic,
            count = count,
            pickups = pickups
        };
    }

    public override object Serialize(object obj)
    {
        PICK pick = (PICK)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, pick.magic);
        WriteUInt32BE(bw, pick.count);
        foreach (var pickup in pick.pickups)
        {
            WriteUInt32BE(bw, pickup.pickupHash);
            WriteByte(bw, pickup.pickupType);
            WriteByte(bw, pickup.pickupIndex);
            WriteUInt16BE(bw, pickup.pickupFlags);
            WriteUInt32BE(bw, pickup.quantity);
            WriteUInt32BE(bw, pickup.modelID);
            WriteUInt32BE(bw, pickup.animID);
        }

        return ms.ToArray();
    }
}

public class PICK
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint magic { get; set; }
    public uint count { get; set; }
    public zAssetPickup[] pickups { get; set; }
}

public class zAssetPickup
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint pickupHash { get; set; }
    public byte pickupType { get; set; }
    public byte pickupIndex { get; set; }
    public ushort pickupFlags { get; set; }
    public uint quantity { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint modelID { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint animID { get; set; }
}