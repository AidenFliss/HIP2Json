using System.IO;

namespace PortHeavyIronGameRewrite;

public sealed class pointerParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version)
    {
        return new pointer
        {
            loc = ReadVector3BE(br),
            rotation = ReadVector3BE(br),
        };
    }

    public override byte[] Serialize(object obj, short version)
    {
        pointer pointer = (pointer)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteVector3BE(bw, pointer.loc);
        WriteVector3BE(bw, pointer.rotation);

        return ms.ToArray();
    }

    public override string GetFolderName() { return "Pointer"; }
}

#pragma warning disable CS8981
public class pointer
{
    public xVec3 loc { get; set; }
    public xVec3 rotation { get; set; }
}