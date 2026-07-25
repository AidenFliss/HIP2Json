using System.IO;

namespace HIP2Json;

public sealed class SIMPParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        float animSpeed = ReadFloatBE(br);
        int initAnimState = ReadInt32BE(br);
        CollisionType collType = (CollisionType)ReadByte(br);
        byte flags = ReadByte(br);
        br.ReadBytes(2);

        return new SIMP
        {
            animSpeed = animSpeed,
            initAnimState = initAnimState,
            collType = collType,
            flags = flags,
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
        bw.Write(new byte[2]);

        return ms.ToArray();
    }
}

public class SIMP
{
    public float animSpeed { get; set; }
    public int initAnimState { get; set; }
    public CollisionType collType { get; set; }
    public byte flags { get; set; }
}
