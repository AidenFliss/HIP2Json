using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class PAREParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        var par = new PARE
        {
            emitFlags = ReadByte(br),
            emitType = (EmitType)ReadByte(br),
            pad = ReadInt16BE(br),
            propID = ReadUInt32BE(br),
        };

        switch (par.emitType)
        {
            case EmitType.Circle:
                par.specific = new CircleEmitter
                {
                    radius = ReadFloatBE(br),
                    deflection = ReadFloatBE(br),
                    dir = ReadVector3BE(br)
                };
                break;

            case EmitType.Sphere:
                par.specific = new SphereEmitter
                {
                    radius = ReadFloatBE(br)
                };
                break;

            case EmitType.Rect:
                par.specific = new RectEmitter
                {
                    xLen = ReadFloatBE(br),
                    zLen = ReadFloatBE(br)
                };
                break;

            case EmitType.Line:
                par.specific = new LineEmitter
                {
                    pos1 = ReadVector3BE(br),
                    pos2 = ReadVector3BE(br),
                    radius = ReadFloatBE(br)
                };
                break;

            case EmitType.Volume:
                par.specific = new VolumeEmitter
                {
                    volumeID = ReadUInt32BE(br)
                };
                break;

            case EmitType.OffsetPoint:
                par.specific = new OffsetPointEmitter
                {
                    offset = ReadVector3BE(br)
                };
                break;

            case EmitType.VCylEdge:
                par.specific = new VCylEmitter
                {
                    height = ReadFloatBE(br),
                    radius = ReadFloatBE(br),
                    deflection = ReadFloatBE(br)
                };
                break;

            case EmitType.EntityBone:
                par.specific = new EntityBoneEmitter
                {
                    flags = ReadByte(br),
                    type = ReadByte(br),
                    bone = ReadByte(br),
                    pad = ReadByte(br),
                    offset = ReadVector3BE(br),
                    radius = ReadFloatBE(br),
                    deflection = ReadFloatBE(br)
                };
                break;

            case EmitType.EntityBound:
                par.specific = new EntityBoundEmitter
                {
                    flags = ReadByte(br),
                    type = ReadByte(br),
                    pad1 = ReadByte(br),
                    pad2 = ReadByte(br),
                    expand = ReadFloatBE(br),
                    deflection = ReadFloatBE(br)
                };
                break;
        }

        br.BaseStream.Position = assetStart + 0x2C;

        par.attachToID = ReadUInt32BE(br);
        par.pos = ReadVector3BE(br);
        par.vel = ReadVector3BE(br);
        par.velAngleVariation = ReadFloatBE(br);
        par.cullMode = ReadUInt32BE(br);
        par.cullDistSqr = ReadFloatBE(br);

        return par;
    }

    public override object Serialize(object obj)
    {
        PARE pare = (PARE)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteByte(bw, pare.emitFlags);
        WriteByte(bw, (byte)pare.emitType);
        WriteInt16BE(bw, pare.pad);
        WriteUInt32BE(bw, pare.propID);

        switch (pare.emitType)
        {
            case EmitType.Circle:
                var circle = (CircleEmitter)pare.specific;
                WriteFloatBE(bw, circle.radius);
                WriteFloatBE(bw, circle.deflection);
                WriteVector3BE(bw, circle.dir);
                break;

            case EmitType.Sphere:
                var sphere = (SphereEmitter)pare.specific;
                WriteFloatBE(bw, sphere.radius);
                break;

            case EmitType.Rect:
                var rect = (RectEmitter)pare.specific;
                WriteFloatBE(bw, rect.xLen);
                WriteFloatBE(bw, rect.zLen);
                break;

            case EmitType.Line:
                var line = (LineEmitter)pare.specific;
                WriteVector3BE(bw, line.pos1);
                WriteVector3BE(bw, line.pos2);
                WriteFloatBE(bw, line.radius);
                break;

            case EmitType.Volume:
                var volume = (VolumeEmitter)pare.specific;
                WriteUInt32BE(bw, volume.volumeID);
                break;

            case EmitType.OffsetPoint:
                var offsetPoint = (OffsetPointEmitter)pare.specific;
                WriteVector3BE(bw, offsetPoint.offset);
                break;

            case EmitType.VCylEdge:
                var vcyl = (VCylEmitter)pare.specific;
                WriteFloatBE(bw, vcyl.height);
                WriteFloatBE(bw, vcyl.radius);
                WriteFloatBE(bw, vcyl.deflection);
                break;

            case EmitType.EntityBone:
                var bone = (EntityBoneEmitter)pare.specific;
                WriteByte(bw, bone.flags);
                WriteByte(bw, bone.type);
                WriteByte(bw, bone.bone);
                WriteByte(bw, bone.pad);
                WriteVector3BE(bw, bone.offset);
                WriteFloatBE(bw, bone.radius);
                WriteFloatBE(bw, bone.deflection);
                break;

            case EmitType.EntityBound:
                var bound = (EntityBoundEmitter)pare.specific;
                WriteByte(bw, bound.flags);
                WriteByte(bw, bound.type);
                WriteByte(bw, bound.pad1);
                WriteByte(bw, bound.pad2);
                WriteFloatBE(bw, bound.expand);
                WriteFloatBE(bw, bound.deflection);
                break;
        }

        while (ms.Length < 0x2C)
            WriteByte(bw, 0);

        WriteUInt32BE(bw, pare.attachToID);
        WriteVector3BE(bw, pare.pos);
        WriteVector3BE(bw, pare.vel);
        WriteFloatBE(bw, pare.velAngleVariation);
        WriteUInt32BE(bw, pare.cullMode);
        WriteFloatBE(bw, pare.cullDistSqr);

        return ms.ToArray();
    }
}

public class PARE
{
    public byte emitFlags { get; set; }
    public EmitType emitType { get; set; }
    public short pad { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint propID { get; set; }
    public object specific { get; set; }

    [JsonConverter(typeof(AssetIDConverter))]
    public uint attachToID { get; set; }

    public xVec3 pos { get; set; }
    public xVec3 vel { get; set; }
    public float velAngleVariation { get; set; }
    public uint cullMode { get; set; }
    public float cullDistSqr { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EmitType : byte
{
    Point = 0,
    CircleEdge = 1,
    Circle = 2,
    RectEdge = 3,
    Rect = 4,
    Line = 5,
    Volume = 6,
    SphereEdge = 7,
    Sphere = 8,
    OffsetPoint = 9,
    SphereEdge2 = 10,
    SphereEdge3 = 11,
    VCylEdge = 12,
    OCircleEdge = 13,
    OCircle = 14,
    EntityBone = 15,
    EntityBound = 16
}

public class CircleEmitter
{
    public float radius { get; set; }
    public float deflection { get; set; }
    public xVec3 dir { get; set; }
}

public class SphereEmitter
{
    public float radius { get; set; }
}

public class RectEmitter
{
    public float xLen { get; set; }
    public float zLen { get; set; }
}

public class LineEmitter
{
    public xVec3 pos1 { get; set; }
    public xVec3 pos2 { get; set; }
    public float radius { get; set; }
}

public class VolumeEmitter
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint volumeID { get; set; }
}

public class OffsetPointEmitter
{
    public xVec3 offset { get; set; }
}

public class VCylEmitter
{
    public float height { get; set; }
    public float radius { get; set; }
    public float deflection { get; set; }
}

public class EntityBoneEmitter
{
    public byte flags { get; set; }
    public byte type { get; set; }
    public byte bone { get; set; }
    public byte pad { get; set; }

    public xVec3 offset { get; set; }

    public float radius { get; set; }
    public float deflection { get; set; }
}

public class EntityBoundEmitter
{
    public byte flags { get; set; }
    public byte type { get; set; }
    public byte pad1 { get; set; }
    public byte pad2 { get; set; }

    public float expand { get; set; }
    public float deflection { get; set; }
}