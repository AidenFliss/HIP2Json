using System.IO;

namespace PortHeavyIronGameRewrite;

public sealed class SIMPParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        return new SIMP
        {
            animSpeed = ReadFloatBE(br),
            initAnimState = ReadInt32BE(br),
            collType = ReadByte(br),
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
        WriteByte(bw, simp.collType);
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
    public byte collType { get; set; }
    public byte flags { get; set; }
    public byte pad1 { get; set; }
    public byte pad2 { get; set; }
}