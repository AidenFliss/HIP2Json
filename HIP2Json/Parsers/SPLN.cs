using System;
using System.IO;

namespace PortHeavyIronGameRewrite;

public sealed class SPLNParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        int spline_type = ReadInt32BE(br);
        int total_count = ReadInt32BE(br);
        int _ = ReadInt32BE(br); //point count -1
        uint unknown_hash_14 = ReadUInt32BE(br);
        uint unknown_hash_18 = ReadUInt32BE(br);

        int point_count = total_count - spline_type;

        xVec4[] points = new xVec4[point_count];
        for (int i = 0; i < point_count; i++)
        {
            points[i] = new xVec4
            {
                x = ReadFloatBE(br),
                y = ReadFloatBE(br),
                z = ReadFloatBE(br)
            };
        }

        if (spline_type == 1)
        {
            br.BaseStream.Position += 8;
        }
        else if (spline_type == 3)
        {
            br.BaseStream.Position += 16;
        }

        for (int i = 0; i < point_count; i++)
        {
            points[i].w = ReadFloatBE(br);
        }

        return new SPLN
        {
            spline_type = spline_type,
            unknown_hash_14 = unknown_hash_14,
            unknown_hash_18 = unknown_hash_18,
            points = points
        };
    }

    public override object Serialize(object obj)
    {
        SPLN spln = (SPLN)obj;

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        int point_count = spln.points?.Length ?? 0;

        WriteInt32BE(bw, spln.spline_type);
        WriteInt32BE(bw, point_count + spln.spline_type);
        WriteInt32BE(bw, Math.Max(0, point_count - 1));
        WriteUInt32BE(bw, spln.unknown_hash_14);
        WriteUInt32BE(bw, spln.unknown_hash_18);

        if (spln.points != null)
        {
            foreach (var point in spln.points)
            {
                WriteFloatBE(bw, point.x);
                WriteFloatBE(bw, point.y);
                WriteFloatBE(bw, point.z);
            }
        }

        if (spln.spline_type == 1)
        {
            bw.Write(new byte[8]);
        }
        else if (spln.spline_type == 3)
        {
            bw.Write(new byte[16]);
        }

        if (spln.points != null)
        {
            foreach (var point in spln.points)
            {
                WriteFloatBE(bw, point.w);
            }
        }

        return ms.ToArray();
    }
}

public class SPLN
{
    public int spline_type { get; set; }
    public uint unknown_hash_14 { get; set; }
    public uint unknown_hash_18 { get; set; }
    public xVec4[] points { get; set; }
}