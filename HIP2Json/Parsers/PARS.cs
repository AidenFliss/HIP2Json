using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class PARSParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        PARS pars = new PARS
        {
            type = ReadInt32BE(br),
            parentParSysID = ReadUInt32BE(br),
            textureID = ReadUInt32BE(br),
            parFlags = ReadByte(br),
            priority = ReadByte(br),
            maxPar = ReadInt16BE(br),
            renderFunc = ReadByte(br),
            renderSrcBlendMode = ReadByte(br),
            renderDstBlendMode = ReadByte(br),
            cmdCount = ReadByte(br),
            cmdSize = ReadInt32BE(br),
        };

        int cmdCountRead = pars.cmdCount;
        List<ParticleCommand> commands = new List<ParticleCommand>(cmdCountRead);
        for (int i = 0; i < cmdCountRead; i++)
            commands.Add(ReadCommand(br));

        pars.commands = commands.ToArray();
        return pars;
    }

    public override object Serialize(object obj)
    {
        PARS pars = (PARS)obj;

        using (MemoryStream ms = new MemoryStream())
        {
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                WriteInt32BE(bw, pars.type);
                WriteUInt32BE(bw, pars.parentParSysID);
                WriteUInt32BE(bw, pars.textureID);
                WriteByte(bw, pars.parFlags);
                WriteByte(bw, pars.priority);
                WriteInt16BE(bw, pars.maxPar);
                WriteByte(bw, pars.renderFunc);
                WriteByte(bw, pars.renderSrcBlendMode);
                WriteByte(bw, pars.renderDstBlendMode);
                WriteByte(bw, (byte)(pars.commands?.Length ?? 0));
                WriteInt32BE(bw, pars.commands?.Sum(c => GetCommandSize(c.commandType)) ?? 0);

                if (pars.commands != null)
                {
                    foreach (ParticleCommand cmd in pars.commands)
                        WriteCommand(bw, cmd);
                }

                return ms.ToArray();
            }
        }
    }

    private static int GetCommandSize(ParticleCommandType commandType)
    {
        return commandType switch
        {
            ParticleCommandType.VelocityApply => 0x08,
            ParticleCommandType.Jet => 0x2C,
            ParticleCommandType.Move => 0x14,
            ParticleCommandType.MoveRandom => 0x14,
            ParticleCommandType.MoveRandomPar => 0x14,
            ParticleCommandType.Accelerate => 0x14,
            ParticleCommandType.RandomVelocityPar => 0x14,
            ParticleCommandType.ApplyCamMat => 0x14,
            ParticleCommandType.Unk5 => 0x0C,
            ParticleCommandType.Age => 0x0C,
            ParticleCommandType.ApplyWind => 0x0C,
            ParticleCommandType.SmokeAlpha => 0x0C,
            ParticleCommandType.Scale => 0x0C,
            ParticleCommandType.ClipVolumes => 0x0C,
            ParticleCommandType.AnimalMagentism => 0x0C,
            ParticleCommandType.DampenSpeed => 0x0C,
            ParticleCommandType.KillSlow => 0x10,
            ParticleCommandType.Follow => 0x10,
            ParticleCommandType.KillDistance => 0x10,
            ParticleCommandType.DamagePlayer => 0x10,
            ParticleCommandType.CollideFall => 0x10,
            ParticleCommandType.FallSticky => 0x10,
            ParticleCommandType.Scale3rdPolyReg => 0x18,
            ParticleCommandType.Alpha3rdPolyReg => 0x18,
            ParticleCommandType.TexAnim => 0x18,
            ParticleCommandType.AlphaInOut => 0x18,
            ParticleCommandType.SizeInOut => 0x18,
            ParticleCommandType.Tex => 0x24,
            ParticleCommandType.Custom => 0x1C,
            ParticleCommandType.OrbitPoint => 0x20,
            ParticleCommandType.PlayerCollision => 0x20,
            ParticleCommandType.RotPar => 0x20,
            ParticleCommandType.RotateAround => 0x20,
            ParticleCommandType.OrbitLine => 0x2C,
            ParticleCommandType.Shaper => 0x30,
            _ => 0,
        };
    }

    private static ParticleCommand ReadCommand(BinaryReader br)
    {
        ParticleCommandType commandType = (ParticleCommandType)ReadInt32BE(br);
        byte enabled = ReadByte(br);
        byte mode = ReadByte(br);
        br.ReadBytes(2);

        return new ParticleCommand
        {
            commandType = commandType,
            enabled = enabled,
            mode = mode,
            specific = ReadSpecific(br, commandType),
        };
    }

    private static ParticleCommandSpecific ReadSpecific(BinaryReader br, ParticleCommandType commandType)
    {
        switch (commandType)
        {
            case ParticleCommandType.Move:
                return new VectorSpecific { v = ReadVector3BE(br) };
            case ParticleCommandType.MoveRandom:
                return new VectorSpecific { v = ReadVector3BE(br) };
            case ParticleCommandType.MoveRandomPar:
                return new VectorSpecific { v = ReadVector3BE(br) };
            case ParticleCommandType.Accelerate:
                return new VectorSpecific { v = ReadVector3BE(br) };
            case ParticleCommandType.RandomVelocityPar:
                return new VectorSpecific { v = ReadVector3BE(br) };
            case ParticleCommandType.ApplyCamMat:
                return new VectorSpecific { v = ReadVector3BE(br) };
            case ParticleCommandType.Unk5:
                return new UnknownSpecific { unknown = br.ReadBytes(4) };
            case ParticleCommandType.Custom:
                return new CustomSpecific
                {
                    userID = ReadUInt32BE(br),
                    userValue1 = ReadFloatBE(br),
                    userValue2 = ReadFloatBE(br),
                    userValue3 = ReadFloatBE(br),
                    userValue4 = ReadFloatBE(br),
                };
            case ParticleCommandType.Age:
                return new FloatSpecific { value = ReadFloatBE(br) };
            case ParticleCommandType.ApplyWind:
                return new FloatSpecific { value = ReadFloatBE(br) };
            case ParticleCommandType.AnimalMagentism:
                return new FloatSpecific { value = ReadFloatBE(br) };
            case ParticleCommandType.DampenSpeed:
                return new FloatSpecific { value = ReadFloatBE(br) };
            case ParticleCommandType.SmokeAlpha:
                return new IntSpecific { value = ReadInt32BE(br) };
            case ParticleCommandType.Scale:
                return new IntSpecific { value = ReadInt32BE(br) };
            case ParticleCommandType.ClipVolumes:
                return new IntSpecific { value = ReadInt32BE(br) };
            case ParticleCommandType.KillSlow:
                return new KillSlowSpecific { speedLimitSqr = ReadFloatBE(br), killLessThan = ReadUInt32BE(br) };
            case ParticleCommandType.Follow:
                return new Float2Specific
                {
                    gravity = ReadFloatBE(br),
                    epsilon = ReadFloatBE(br),
                };
            case ParticleCommandType.KillDistance:
                return new KillDistanceSpecific { dSqr = ReadFloatBE(br), killGreaterThan = ReadUInt32BE(br) };
            case ParticleCommandType.DamagePlayer:
                return new DamagePlayerSpecific { damage = ReadInt32BE(br), granular = ReadInt32BE(br) };
            case ParticleCommandType.CollideFall:
                return new CollideFallSpecific { y = ReadFloatBE(br), bounce = ReadFloatBE(br) };
            case ParticleCommandType.FallSticky:
                return new CollideFallStickySpecific
                {
                    y = ReadFloatBE(br),
                    bounce = ReadFloatBE(br),
                    sticky = ReadFloatBE(br),
                };
            case ParticleCommandType.Scale3rdPolyReg:
                return new PolyRegSpecific
                {
                    constant = ReadFloatBE(br),
                    a = ReadFloatBE(br),
                    a2 = ReadFloatBE(br),
                    a3 = ReadFloatBE(br),
                };
            case ParticleCommandType.Alpha3rdPolyReg:
                return new PolyRegSpecific
                {
                    constant = ReadFloatBE(br),
                    a = ReadFloatBE(br),
                    a2 = ReadFloatBE(br),
                    a3 = ReadFloatBE(br),
                };
            case ParticleCommandType.TexAnim:
                return new TexAnimSpecific
                {
                    animMode = ReadByte(br),
                    animWrapMode = ReadByte(br),
                    padAnim = ReadByte(br),
                    throttleSpdLessThan = ReadByte(br),
                    throttleSpdSqr = ReadFloatBE(br),
                    throttleTime = ReadFloatBE(br),
                    throttleTimeElapsed = ReadFloatBE(br),
                };
            case ParticleCommandType.AlphaInOut:
                return new CustAlphaSpecific { custAlpha = ReadFloatArrayBE(br, 4) };
            case ParticleCommandType.SizeInOut:
                return new CustSizeSpecific { custSize = ReadFloatArrayBE(br, 4) };
            case ParticleCommandType.Tex:
                return new TexSpecific
                {
                    x1 = ReadFloatBE(br),
                    y1 = ReadFloatBE(br),
                    x2 = ReadFloatBE(br),
                    y2 = ReadFloatBE(br),
                    birthMode = ReadByte(br),
                    rows = ReadByte(br),
                    cols = ReadByte(br),
                    unitCount = ReadByte(br),
                    unitWidth = ReadFloatBE(br),
                    unitHeight = ReadFloatBE(br),
                };
            case ParticleCommandType.Jet:
                return new JetSpecific
                {
                    center = ReadVector3BE(br),
                    acc = ReadVector3BE(br),
                    gravity = ReadFloatBE(br),
                    epsilon = ReadFloatBE(br),
                    radiusSqr = ReadFloatBE(br),
                };
            case ParticleCommandType.OrbitPoint:
                return new OrbitPointSpecific
                {
                    center = ReadVector3BE(br),
                    gravity = ReadFloatBE(br),
                    epsilon = ReadFloatBE(br),
                    maxRadiusSqr = ReadFloatBE(br),
                };
            case ParticleCommandType.OrbitLine:
                return new OrbitLineSpecific
                {
                    p = ReadVector3BE(br),
                    axis = ReadVector3BE(br),
                    gravity = ReadFloatBE(br),
                    epsilon = ReadFloatBE(br),
                    maxRadiusSqr = ReadFloatBE(br),
                };
            case ParticleCommandType.PlayerCollision:
                return new BoundsSpecific { min = ReadVector3BE(br), max = ReadVector3BE(br) };
            case ParticleCommandType.RotPar:
                return new BoundsSpecific { min = ReadVector3BE(br), max = ReadVector3BE(br) };
            case ParticleCommandType.RotateAround:
                return new RotateAroundSpecific
                {
                    pos = ReadVector3BE(br),
                    unused1 = ReadFloatBE(br),
                    radiusGrowth = ReadFloatBE(br),
                    yaw = ReadFloatBE(br),
                };
            case ParticleCommandType.Shaper:
                return new ShaperSpecific
                {
                    custAlpha = ReadFloatArrayBE(br, 4),
                    custSize = ReadFloatArrayBE(br, 4),
                    dampSpeed = ReadFloatBE(br),
                    gravity = ReadFloatBE(br),
                };
            default:
                return null;
        }
    }

    private static void WriteCommand(BinaryWriter bw, ParticleCommand cmd)
    {
        WriteInt32BE(bw, (int)cmd.commandType);
        WriteByte(bw, cmd.enabled);
        WriteByte(bw, cmd.mode);
        bw.Write(new byte[2]);

        if (cmd.specific != null)
            WriteSpecific(bw, cmd.specific);
    }

    private static void WriteSpecific(BinaryWriter bw, ParticleCommandSpecific specific)
    {
        switch (specific)
        {
            case VectorSpecific vectorSpecific:
                WriteVector3BE(bw, vectorSpecific.v);
                break;
            case UnknownSpecific unknownSpecific:
                bw.Write(unknownSpecific.unknown);
                break;
            case CustomSpecific customSpecific:
                WriteUInt32BE(bw, customSpecific.userID);
                WriteFloatBE(bw, customSpecific.userValue1);
                WriteFloatBE(bw, customSpecific.userValue2);
                WriteFloatBE(bw, customSpecific.userValue3);
                WriteFloatBE(bw, customSpecific.userValue4);
                break;
            case FloatSpecific floatSpecific:
                WriteFloatBE(bw, floatSpecific.value);
                break;
            case IntSpecific intSpecific:
                WriteInt32BE(bw, intSpecific.value);
                break;
            case KillSlowSpecific killSlowSpecific:
                WriteFloatBE(bw, killSlowSpecific.speedLimitSqr);
                WriteUInt32BE(bw, killSlowSpecific.killLessThan);
                break;
            case Float2Specific float2Specific:
                WriteFloatBE(bw, float2Specific.gravity);
                WriteFloatBE(bw, float2Specific.epsilon);
                break;
            case KillDistanceSpecific killDistanceSpecific:
                WriteFloatBE(bw, killDistanceSpecific.dSqr);
                WriteUInt32BE(bw, killDistanceSpecific.killGreaterThan);
                break;
            case DamagePlayerSpecific damagePlayerSpecific:
                WriteInt32BE(bw, damagePlayerSpecific.damage);
                WriteInt32BE(bw, damagePlayerSpecific.granular);
                break;
            case CollideFallSpecific collideFallSpecific:
                WriteFloatBE(bw, collideFallSpecific.y);
                WriteFloatBE(bw, collideFallSpecific.bounce);
                break;
            case CollideFallStickySpecific collideFallStickySpecific:
                WriteFloatBE(bw, collideFallStickySpecific.y);
                WriteFloatBE(bw, collideFallStickySpecific.bounce);
                WriteFloatBE(bw, collideFallStickySpecific.sticky);
                break;
            case PolyRegSpecific polyRegSpecific:
                WriteFloatBE(bw, polyRegSpecific.constant);
                WriteFloatBE(bw, polyRegSpecific.a);
                WriteFloatBE(bw, polyRegSpecific.a2);
                WriteFloatBE(bw, polyRegSpecific.a3);
                break;
            case TexAnimSpecific texAnimSpecific:
                WriteByte(bw, texAnimSpecific.animMode);
                WriteByte(bw, texAnimSpecific.animWrapMode);
                WriteByte(bw, texAnimSpecific.padAnim);
                WriteByte(bw, texAnimSpecific.throttleSpdLessThan);
                WriteFloatBE(bw, texAnimSpecific.throttleSpdSqr);
                WriteFloatBE(bw, texAnimSpecific.throttleTime);
                WriteFloatBE(bw, texAnimSpecific.throttleTimeElapsed);
                break;
            case CustAlphaSpecific custAlphaSpecific:
                foreach (float value in custAlphaSpecific.custAlpha)
                    WriteFloatBE(bw, value);
                break;
            case CustSizeSpecific custSizeSpecific:
                foreach (float value in custSizeSpecific.custSize)
                    WriteFloatBE(bw, value);
                break;
            case TexSpecific texSpecific:
                WriteFloatBE(bw, texSpecific.x1);
                WriteFloatBE(bw, texSpecific.y1);
                WriteFloatBE(bw, texSpecific.x2);
                WriteFloatBE(bw, texSpecific.y2);
                WriteByte(bw, texSpecific.birthMode);
                WriteByte(bw, texSpecific.rows);
                WriteByte(bw, texSpecific.cols);
                WriteByte(bw, texSpecific.unitCount);
                WriteFloatBE(bw, texSpecific.unitWidth);
                WriteFloatBE(bw, texSpecific.unitHeight);
                break;
            case JetSpecific jetSpecific:
                WriteVector3BE(bw, jetSpecific.center);
                WriteVector3BE(bw, jetSpecific.acc);
                WriteFloatBE(bw, jetSpecific.gravity);
                WriteFloatBE(bw, jetSpecific.epsilon);
                WriteFloatBE(bw, jetSpecific.radiusSqr);
                break;
            case OrbitPointSpecific orbitPointSpecific:
                WriteVector3BE(bw, orbitPointSpecific.center);
                WriteFloatBE(bw, orbitPointSpecific.gravity);
                WriteFloatBE(bw, orbitPointSpecific.epsilon);
                WriteFloatBE(bw, orbitPointSpecific.maxRadiusSqr);
                break;
            case OrbitLineSpecific orbitLineSpecific:
                WriteVector3BE(bw, orbitLineSpecific.p);
                WriteVector3BE(bw, orbitLineSpecific.axis);
                WriteFloatBE(bw, orbitLineSpecific.gravity);
                WriteFloatBE(bw, orbitLineSpecific.epsilon);
                WriteFloatBE(bw, orbitLineSpecific.maxRadiusSqr);
                break;
            case BoundsSpecific boundsSpecific:
                WriteVector3BE(bw, boundsSpecific.min);
                WriteVector3BE(bw, boundsSpecific.max);
                break;
            case RotateAroundSpecific rotateAroundSpecific:
                WriteVector3BE(bw, rotateAroundSpecific.pos);
                WriteFloatBE(bw, rotateAroundSpecific.unused1);
                WriteFloatBE(bw, rotateAroundSpecific.radiusGrowth);
                WriteFloatBE(bw, rotateAroundSpecific.yaw);
                break;
            case ShaperSpecific shaperSpecific:
                foreach (float value in shaperSpecific.custAlpha)
                    WriteFloatBE(bw, value);
                foreach (float value in shaperSpecific.custSize)
                    WriteFloatBE(bw, value);
                WriteFloatBE(bw, shaperSpecific.dampSpeed);
                WriteFloatBE(bw, shaperSpecific.gravity);
                break;
        }
    }

    private static float[] ReadFloatArrayBE(BinaryReader br, int count)
    {
        float[] values = new float[count];
        for (int i = 0; i < count; i++)
            values[i] = ReadFloatBE(br);
        return values;
    }
}

public enum ParticleCommandType
{
    Move = 0,
    MoveRandom = 1,
    Accelerate = 2,
    VelocityApply = 3,
    Jet = 4,
    Unk5 = 5,
    KillSlow = 6,
    Follow = 7,
    OrbitPoint = 8,
    OrbitLine = 9,
    MoveRandomPar = 10,
    Scale3rdPolyReg = 11,
    Tex = 12,
    TexAnim = 13,
    PlayerCollision = 14,
    RandomVelocityPar = 15,
    Custom = 16,
    KillDistance = 17,
    Age = 18,
    Alpha3rdPolyReg = 19,
    ApplyWind = 20,
    RotPar = 21,
    ApplyCamMat = 22,
    RotateAround = 23,
    SmokeAlpha = 24,
    Scale = 25,
    ClipVolumes = 26,
    AnimalMagentism = 27,
    DamagePlayer = 28,
    CollideFall = 29,
    Shaper = 30,
    AlphaInOut = 31,
    SizeInOut = 32,
    DampenSpeed = 33,
    FallSticky = 34,
}

public class PARS
{
    public int type { get; set; }

    [JsonConverter(typeof(AssetIDConverter))]
    public uint parentParSysID { get; set; }

    [JsonConverter(typeof(AssetIDConverter))]
    public uint textureID { get; set; }
    public byte parFlags { get; set; }
    public byte priority { get; set; }
    public short maxPar { get; set; }
    public byte renderFunc { get; set; }
    public byte renderSrcBlendMode { get; set; }
    public byte renderDstBlendMode { get; set; }
    public byte cmdCount { get; set; }
    public int cmdSize { get; set; }
    public ParticleCommand[] commands { get; set; }
}

public class ParticleCommand
{
    public ParticleCommandType commandType { get; set; }
    public byte enabled { get; set; }
    public byte mode { get; set; }
    public ParticleCommandSpecific specific { get; set; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(VectorSpecific), "Vector")]
[JsonDerivedType(typeof(UnknownSpecific), "Unknown")]
[JsonDerivedType(typeof(CustomSpecific), "Custom")]
[JsonDerivedType(typeof(FloatSpecific), "Float")]
[JsonDerivedType(typeof(IntSpecific), "Int")]
[JsonDerivedType(typeof(KillSlowSpecific), "KillSlow")]
[JsonDerivedType(typeof(Float2Specific), "Float2")]
[JsonDerivedType(typeof(KillDistanceSpecific), "KillDistance")]
[JsonDerivedType(typeof(DamagePlayerSpecific), "DamagePlayer")]
[JsonDerivedType(typeof(CollideFallSpecific), "CollideFall")]
[JsonDerivedType(typeof(CollideFallStickySpecific), "CollideFallSticky")]
[JsonDerivedType(typeof(PolyRegSpecific), "PolyReg")]
[JsonDerivedType(typeof(TexAnimSpecific), "TexAnim")]
[JsonDerivedType(typeof(CustAlphaSpecific), "CustAlpha")]
[JsonDerivedType(typeof(CustSizeSpecific), "CustSize")]
[JsonDerivedType(typeof(TexSpecific), "Tex")]
[JsonDerivedType(typeof(JetSpecific), "Jet")]
[JsonDerivedType(typeof(OrbitPointSpecific), "OrbitPoint")]
[JsonDerivedType(typeof(OrbitLineSpecific), "OrbitLine")]
[JsonDerivedType(typeof(BoundsSpecific), "Bounds")]
[JsonDerivedType(typeof(RotateAroundSpecific), "RotateAround")]
[JsonDerivedType(typeof(ShaperSpecific), "Shaper")]
public abstract class ParticleCommandSpecific
{
}

public class VectorSpecific : ParticleCommandSpecific
{
    public xVec3 v { get; set; }
}

public class UnknownSpecific : ParticleCommandSpecific
{
    public byte[] unknown { get; set; }
}

public class CustomSpecific : ParticleCommandSpecific
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint userID { get; set; }
    public float userValue1 { get; set; }
    public float userValue2 { get; set; }
    public float userValue3 { get; set; }
    public float userValue4 { get; set; }
}

public class FloatSpecific : ParticleCommandSpecific
{
    public float value { get; set; }
}

public class IntSpecific : ParticleCommandSpecific
{
    public int value { get; set; }
}

public class KillSlowSpecific : ParticleCommandSpecific
{
    public float speedLimitSqr { get; set; }
    public uint killLessThan { get; set; }
}

public class Float2Specific : ParticleCommandSpecific
{
    public float gravity { get; set; }
    public float epsilon { get; set; }
}

public class KillDistanceSpecific : ParticleCommandSpecific
{
    public float dSqr { get; set; }
    public uint killGreaterThan { get; set; }
}

public class DamagePlayerSpecific : ParticleCommandSpecific
{
    public int damage { get; set; }
    public int granular { get; set; }
}

public class CollideFallSpecific : ParticleCommandSpecific
{
    public float y { get; set; }
    public float bounce { get; set; }
}

public class CollideFallStickySpecific : ParticleCommandSpecific
{
    public float y { get; set; }
    public float bounce { get; set; }
    public float sticky { get; set; }
}

public class PolyRegSpecific : ParticleCommandSpecific
{
    public float constant { get; set; }
    public float a { get; set; }
    public float a2 { get; set; }
    public float a3 { get; set; }
}

public class TexAnimSpecific : ParticleCommandSpecific
{
    public byte animMode { get; set; }
    public byte animWrapMode { get; set; }
    public byte padAnim { get; set; }
    public byte throttleSpdLessThan { get; set; }
    public float throttleSpdSqr { get; set; }
    public float throttleTime { get; set; }
    public float throttleTimeElapsed { get; set; }
}

public class CustAlphaSpecific : ParticleCommandSpecific
{
    public float[] custAlpha { get; set; }
}

public class CustSizeSpecific : ParticleCommandSpecific
{
    public float[] custSize { get; set; }
}

public class TexSpecific : ParticleCommandSpecific
{
    public float x1 { get; set; }
    public float y1 { get; set; }
    public float x2 { get; set; }
    public float y2 { get; set; }
    public byte birthMode { get; set; }
    public byte rows { get; set; }
    public byte cols { get; set; }
    public byte unitCount { get; set; }
    public float unitWidth { get; set; }
    public float unitHeight { get; set; }
}

public class JetSpecific : ParticleCommandSpecific
{
    public xVec3 center { get; set; }
    public xVec3 acc { get; set; }
    public float gravity { get; set; }
    public float epsilon { get; set; }
    public float radiusSqr { get; set; }
}

public class OrbitPointSpecific : ParticleCommandSpecific
{
    public xVec3 center { get; set; }
    public float gravity { get; set; }
    public float epsilon { get; set; }
    public float maxRadiusSqr { get; set; }
}

public class OrbitLineSpecific : ParticleCommandSpecific
{
    public xVec3 p { get; set; }
    public xVec3 axis { get; set; }
    public float gravity { get; set; }
    public float epsilon { get; set; }
    public float maxRadiusSqr { get; set; }
}

public class BoundsSpecific : ParticleCommandSpecific
{
    public xVec3 min { get; set; }
    public xVec3 max { get; set; }
}

public class RotateAroundSpecific : ParticleCommandSpecific
{
    public xVec3 pos { get; set; }
    public float unused1 { get; set; }
    public float radiusGrowth { get; set; }
    public float yaw { get; set; }
}

public class ShaperSpecific : ParticleCommandSpecific
{
    public float[] custAlpha { get; set; }
    public float[] custSize { get; set; }
    public float dampSpeed { get; set; }
    public float gravity { get; set; }
}