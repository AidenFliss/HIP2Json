using System;
using System.IO;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

internal sealed class TRIGParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        br.BaseStream.Seek(assetStart + 0x09, SeekOrigin.Begin);

        byte t = ReadByte(br);

        br.BaseStream.Seek(dataStart, SeekOrigin.Begin);

        xVec3[] positions = new xVec3[4];

        for (int i = 0; i < 4; i++)
            positions[i] = ReadVector3BE(br);

        xVec3 direction = ReadVector3BE(br);

        uint flags = ReadUInt32BE(br);

        return new TRIG
        {
            Type = (TriggerType)t,
            Positions = positions,
            Direction = direction,
            Flags = flags
        };
    }

    public override object Serialize(object obj)
    {
        TRIG trig = (TRIG)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms); //not writing type here, becase the program.cs sphagetti needs to, because out of the byte array

        foreach (xVec3 pos in trig.Positions)
            WriteVector3BE(bw, pos);
        WriteVector3BE(bw, trig.Direction);
        WriteUInt32BE(bw, trig.Flags);
        
        return ms.ToArray();
    }
}

class TRIG
{
    public TriggerType Type { get; set; }
    public xVec3[] Positions { get; set; }
    public xVec3 Direction { get; set; }
    public uint Flags { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
enum TriggerType : byte
{
    Box = 0,
    Sphere = 1,
    Cylinder = 2,
    Unknown = 255,
}