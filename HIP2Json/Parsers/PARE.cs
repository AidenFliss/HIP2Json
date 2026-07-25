using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class PAREParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        byte emitFlags = ReadByte(br);
        EmitType emitType = (EmitType)ReadByte(br);
        br.ReadBytes(2);
        uint propID = ReadUInt32BE(br);

        var par = new PARE
        {
            emitFlags = emitFlags,
            emitType = emitType,
            propID = propID,
            specific = ReadEmitter(br, emitType),
        };

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
        bw.Write(new byte[2]);
        WriteUInt32BE(bw, pare.propID);

        if (pare.specific != null)
            WriteEmitter(bw, pare.specific);

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

    private EmitterData ReadEmitter(BinaryReader br, EmitType emitType)
    {
        return emitType switch
        {
            EmitType.Circle => new CircleEmitter
            {
                radius = ReadFloatBE(br),
                deflection = ReadFloatBE(br),
                dir = ReadVector3BE(br),
            },
            EmitType.Sphere => new SphereEmitter { radius = ReadFloatBE(br) },
            EmitType.Rect => new RectEmitter { xLen = ReadFloatBE(br), zLen = ReadFloatBE(br) },
            EmitType.Line => new LineEmitter
            {
                pos1 = ReadVector3BE(br),
                pos2 = ReadVector3BE(br),
                radius = ReadFloatBE(br),
            },
            EmitType.Volume => new VolumeEmitter { volumeID = ReadUInt32BE(br) },
            EmitType.OffsetPoint => new OffsetPointEmitter { offset = ReadVector3BE(br) },
            EmitType.VCylEdge => new VCylEmitter
            {
                height = ReadFloatBE(br),
                radius = ReadFloatBE(br),
                deflection = ReadFloatBE(br),
            },
            EmitType.EntityBone => ReadEntityBone(br),
            EmitType.EntityBound => ReadEntityBound(br),
            _ => null,
        };
    }

    private EntityBoneEmitter ReadEntityBone(BinaryReader br)
    {
        byte flags = ReadByte(br);
        byte type = ReadByte(br);
        byte bone = ReadByte(br);
        br.ReadBytes(1);
        xVec3 offset = ReadVector3BE(br);
        float radius = ReadFloatBE(br);
        float deflection = ReadFloatBE(br);

        return new EntityBoneEmitter
        {
            flags = flags,
            type = type,
            bone = bone,
            offset = offset,
            radius = radius,
            deflection = deflection,
        };
    }

    private EntityBoundEmitter ReadEntityBound(BinaryReader br)
    {
        byte flags = ReadByte(br);
        byte type = ReadByte(br);
        br.ReadBytes(2);
        float expand = ReadFloatBE(br);
        float deflection = ReadFloatBE(br);

        return new EntityBoundEmitter
        {
            flags = flags,
            type = type,
            expand = expand,
            deflection = deflection,
        };
    }

    private void WriteEmitter(BinaryWriter bw, EmitterData emitter)
    {
        switch (emitter)
        {
            case CircleEmitter circle:
                WriteFloatBE(bw, circle.radius);
                WriteFloatBE(bw, circle.deflection);
                WriteVector3BE(bw, circle.dir);
                break;

            case SphereEmitter sphere:
                WriteFloatBE(bw, sphere.radius);
                break;

            case RectEmitter rect:
                WriteFloatBE(bw, rect.xLen);
                WriteFloatBE(bw, rect.zLen);
                break;

            case LineEmitter line:
                WriteVector3BE(bw, line.pos1);
                WriteVector3BE(bw, line.pos2);
                WriteFloatBE(bw, line.radius);
                break;

            case VolumeEmitter volume:
                WriteUInt32BE(bw, volume.volumeID);
                break;

            case OffsetPointEmitter offsetPoint:
                WriteVector3BE(bw, offsetPoint.offset);
                break;

            case VCylEmitter vcyl:
                WriteFloatBE(bw, vcyl.height);
                WriteFloatBE(bw, vcyl.radius);
                WriteFloatBE(bw, vcyl.deflection);
                break;

            case EntityBoneEmitter bone:
                WriteByte(bw, bone.flags);
                WriteByte(bw, bone.type);
                WriteByte(bw, bone.bone);
                bw.Write(new byte[1]);
                WriteVector3BE(bw, bone.offset);
                WriteFloatBE(bw, bone.radius);
                WriteFloatBE(bw, bone.deflection);
                break;

            case EntityBoundEmitter bound:
                WriteByte(bw, bound.flags);
                WriteByte(bw, bound.type);
                bw.Write(new byte[2]);
                WriteFloatBE(bw, bound.expand);
                WriteFloatBE(bw, bound.deflection);
                break;
        }
    }
}

public class PARE
{
    public byte emitFlags { get; set; }
    public EmitType emitType { get; set; }

    [JsonConverter(typeof(AssetIDConverter))]
    public uint propID { get; set; }
    public EmitterData specific { get; set; }

    [JsonConverter(typeof(AssetIDConverter))]
    public uint attachToID { get; set; }
    public xVec3 pos { get; set; }
    public xVec3 vel { get; set; }
    public float velAngleVariation { get; set; }
    public uint cullMode { get; set; }
    public float cullDistSqr { get; set; }
}
