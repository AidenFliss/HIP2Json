using System;
using System.IO;
using System.Linq;

namespace HIP2Json;

public sealed class FLYParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        long sizeOfAsset = br.BaseStream.Length;
        long entries = sizeOfAsset / 64;
        long remainder = sizeOfAsset - (entries * 64);
        string rawTail = null;
        zFlyKey[] keys = new zFlyKey[entries];
        for (uint i = 0; i < entries; i++)
        {
            keys[i] = new zFlyKey
            {
                frame = ReadInt32LE(br),
                matrix = Enumerable.Range(0, 12).Select(_ => ReadFloatLE(br)).ToArray(),
                aperture = Enumerable.Range(0, 2).Select(_ => ReadFloatLE(br)).ToArray(),
                focal = ReadFloatLE(br),
            };
        }

        if (remainder > 0)
        {
            rawTail = Convert.ToBase64String(br.ReadBytes((int)remainder));
        }

        return new FLY { keys = keys, tail = rawTail };
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

        if (!string.IsNullOrEmpty(fly.tail))
            bw.Write(Convert.FromBase64String(fly.tail));

        return ms.ToArray();
    }
}

public class FLY
{
    public zFlyKey[] keys;
    public string tail;
}

public class zFlyKey
{
    public int frame { get; set; }
    public float[] matrix { get; set; }
    public float[] aperture { get; set; }
    public float focal { get; set; }
}
