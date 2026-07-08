using System.IO;
using System.Linq;

namespace PortHeavyIronGameRewrite;

public sealed class FLYParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        long sizeOfAsset = br.BaseStream.Length;
        long entries = sizeOfAsset / 64;

        zFlyKey[] keys = new zFlyKey[entries];
        for (uint i = 0; i < entries; i++)
        {
            keys[i] = new zFlyKey
            {
                frame = ReadInt32LE(br),
                matrix = Enumerable.Range(0, 12)
                    .Select(_ => ReadFloatLE(br))
                    .ToArray(),
                aperture = Enumerable.Range(0, 2)
                    .Select(_ => ReadFloatLE(br))
                    .ToArray(),
                focal = ReadFloatLE(br)
            };
        }

        return new FLY
        {
            keys = keys
        };
    }

    public override object Serialize(object obj)
    {
        FLY fly = (FLY)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        foreach (var key in fly.keys)
        {
            WriteInt32LE(bw, key.frame);
            foreach (var value in key.matrix)
                WriteFloatLE(bw, value);
            foreach (var value in key.aperture)
                WriteFloatLE(bw, value);
            WriteFloatLE(bw, key.focal);
        }
        
        return ms.ToArray();
    }
}

public class FLY
{
    public zFlyKey[] keys;
}

public class zFlyKey
{
    public int frame { get; set; }
    public float[] matrix { get; set; }
    public float[] aperture { get; set; }
    public float focal { get; set; }
}