using System;
using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class SIMPParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        return new SIMP
        {
            animSpeed = ReadFloatBE(br),
            initAnimState = ReadInt32BE(br),
            collType = (CollisionType)ReadByte(br),
            flags = ReadByte(br),
            pad1 = ReadByte(br),
            pad2 = ReadByte(br)
        };
    }

    public override object Serialize(object obj)
    {
        SIMP simp = (SIMP)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteFloatBE(bw, simp.animSpeed);
        WriteInt32BE(bw, simp.initAnimState);
        WriteByte(bw, (byte)simp.collType);
        WriteByte(bw, simp.flags);
        WriteByte(bw, simp.pad1);
        WriteByte(bw, simp.pad2);

        return ms.ToArray();
    }
}

public class SIMP
{
    public float animSpeed { get; set; }
    public int initAnimState { get; set; }
    public CollisionType collType { get; set; }
    public byte flags { get; set; }
    public byte pad1 { get; set; }
    public byte pad2 { get; set; }
}

[Flags]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CollisionType : byte //weird bit stuff again, yay
{
    None    = 0,
    Trigger = 1 << 0,
    Static  = 1 << 1,
    Dynamic = 1 << 2,
    NPC     = 1 << 3,
    Player  = 1 << 4
}