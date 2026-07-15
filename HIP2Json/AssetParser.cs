using System;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;

namespace HIP2Json;

public abstract class AssetParser
{
    public abstract object Parse(BinaryReader br, long assetStart, long dataStart);

    public abstract object Serialize(object obj);

    public virtual long GetLinksOffset(BinaryReader br, byte linkCount)
    {
        return br.BaseStream.Length - (linkCount * 32L);
    }

    protected static float ReadFloatBE(BinaryReader br)
    {
        byte[] b = br.ReadBytes(4);
        if (Program.BigEndian == true) Array.Reverse(b);
        return BitConverter.ToSingle(b, 0);
    }

    protected static float ReadFloatLE(BinaryReader br)
    {
        byte[] b = br.ReadBytes(4);
        return BitConverter.ToSingle(b, 0);
    }

    protected static int ReadInt32BE(BinaryReader br)
    {
        byte[] b = br.ReadBytes(4);
        if (Program.BigEndian == true) Array.Reverse(b);
        return BitConverter.ToInt32(b, 0);
    }

    protected static int ReadInt32LE(BinaryReader br)
    {
        byte[] b = br.ReadBytes(4);
        return BitConverter.ToInt32(b, 0);
    }

    protected static short ReadInt16BE(BinaryReader br)
    {
        byte[] b = br.ReadBytes(2);
        if (Program.BigEndian == true) Array.Reverse(b);
        return BitConverter.ToInt16(b, 0);
    }

    protected static ushort ReadUInt16BE(BinaryReader br)
    {
        byte[] b = br.ReadBytes(2);
        if (Program.BigEndian == true) Array.Reverse(b);
        return BitConverter.ToUInt16(b, 0);
    }

    protected static uint ReadUInt32BE(BinaryReader br)
    {
        byte[] b = br.ReadBytes(4);
        if (Program.BigEndian == true) Array.Reverse(b);
        return BitConverter.ToUInt32(b, 0);
    }

    protected static byte ReadByte(BinaryReader br)
    {
        return br.ReadByte();
    }

    protected static void WriteFloatBE(BinaryWriter bw, float value)
    {
        byte[] b = BitConverter.GetBytes(value);
        if (Program.BigEndian == true) Array.Reverse(b);
        bw.Write(b);
    }

    protected static void WriteFloatLE(BinaryWriter bw, float value)
    {
        byte[] b = BitConverter.GetBytes(value);
        bw.Write(b);
    }

    protected static void WriteInt32LE(BinaryWriter bw, int value)
    {
        byte[] b = BitConverter.GetBytes(value);
        bw.Write(b);
    }

    protected static void WriteUInt32BE(BinaryWriter bw, uint value)
    {
        byte[] b = BitConverter.GetBytes(value);
        if (Program.BigEndian == true) Array.Reverse(b);
        bw.Write(b);
    }

    protected static void WriteUInt16BE(BinaryWriter bw, ushort value)
    {
        byte[] b = BitConverter.GetBytes(value);
        if (Program.BigEndian == true) Array.Reverse(b);
        bw.Write(b);
    }

    protected static void WriteInt32BE(BinaryWriter bw, int value)
    {
        byte[] b = BitConverter.GetBytes(value);
        if (Program.BigEndian == true) Array.Reverse(b);
        bw.Write(b);
    }

    protected static void WriteInt16BE(BinaryWriter bw, short value)
    {
        byte[] b = BitConverter.GetBytes(value);
        if (Program.BigEndian == true) Array.Reverse(b);
        bw.Write(b);
    }

    protected static void WriteByte(BinaryWriter bw, byte value)
    {
        bw.Write(value);
    }

    protected static void WriteString(BinaryWriter bw, string value)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(value);
        bw.Write(bytes);
        bw.Write((byte)0);
    }

    protected static xVec2 ReadVector2BE(BinaryReader br)
    {
        return new xVec2
        {
            x = ReadFloatBE(br),
            y = ReadFloatBE(br)
        };
    }

    protected static void WriteVector2BE(BinaryWriter bw, xVec2 vec)
    {
        WriteFloatBE(bw, vec.x);
        WriteFloatBE(bw, vec.y);
    }

    protected static xVec3 ReadVector3BE(BinaryReader br)
    {
        return new xVec3
        {
            x = ReadFloatBE(br),
            y = ReadFloatBE(br),
            z = ReadFloatBE(br)
        };
    }

    protected static void WriteVector3BE(BinaryWriter bw, xVec3 vec)
    {
        WriteFloatBE(bw, vec.x);
        WriteFloatBE(bw, vec.y);
        WriteFloatBE(bw, vec.z);
    }

    protected static xColor ReadColorBE(BinaryReader br)
    {
        return new xColor
        {
            r = ReadByte(br),
            g = ReadByte(br),
            b = ReadByte(br),
            a = ReadByte(br),
        };
    }

    protected static void WriteColorBE(BinaryWriter bw, xColor color)
    {
        WriteByte(bw, (byte)color.r);
        WriteByte(bw, (byte)color.g);
        WriteByte(bw, (byte)color.b);
        WriteByte(bw, (byte)color.a);
    }

    protected static string UInt32ToASCII(uint value)
    {
        return Encoding.ASCII.GetString(new[]
        {
            (byte)(value >> 24),
            (byte)(value >> 16),
            (byte)(value >> 8),
            (byte)value
        });
    }

    protected static xMotion ReadMotion(BinaryReader br)
    {
        var motion = new xMotion
        {
            type = (MotionType)ReadByte(br),
            useBanking = ReadByte(br),
            flags = ReadUInt16BE(br)
        };

        switch (motion.type)
        {
            case MotionType.ExtendRetract:
                motion.specific = new ExtendRetractMotion
                {
                    retPos = ReadVector3BE(br),
                    extDPos = ReadVector3BE(br),
                    extTm = ReadFloatBE(br),
                    extWaitTm = ReadFloatBE(br),
                    retTm = ReadFloatBE(br),
                    retWaitTm = ReadFloatBE(br)
                };
                break;

            case MotionType.Orbit:
                motion.specific = new OrbitMotion
                {
                    center = ReadVector3BE(br),
                    w = ReadFloatBE(br),
                    h = ReadFloatBE(br),
                    period = ReadFloatBE(br)
                };
                break;

            case MotionType.Spline:
            {
                if (Program.CurrentGame == GameType.BFBB)
                {
                    motion.specific = new SplineMotion
                    {
                        unknown = ReadInt32BE(br)
                    };
                }
                else
                {
                    motion.specific = new SplineMotion
                    {
                        splineID = ReadUInt32BE(br),
                        speed = ReadFloatBE(br),
                        leanModifier = ReadFloatBE(br)
                    };
                }

                break;
            }

            case MotionType.MovePoint:
                motion.specific = new MovePointMotion
                {
                    flags = ReadUInt32BE(br),
                    mpID = ReadUInt32BE(br),
                    speed = ReadFloatBE(br)
                };
                break;

            case MotionType.Mechanism:
            {
                if (Program.CurrentGame == GameType.BFBB)
                {
                    motion.specific = new MechanismMotion
                    {
                        mechanismType = (MechanismType)ReadByte(br),
                        flags = ReadByte(br),
                        slideAxis = (Axis)ReadByte(br),
                        rotateAxis = (Axis)ReadByte(br),

                        slideDistance = ReadFloatBE(br),
                        slideTime = ReadFloatBE(br),
                        slideAccelTime = ReadFloatBE(br),
                        slideDecelTime = ReadFloatBE(br),

                        rotateDistance = ReadFloatBE(br),
                        rotateTime = ReadFloatBE(br),
                        rotateAccelTime = ReadFloatBE(br),
                        rotateDecelTime = ReadFloatBE(br),

                        returnDelay = ReadFloatBE(br),
                        postReturnDelay = ReadFloatBE(br)
                    };
                }
                else
                {
                    motion.specific = new MechanismMotion
                    {
                        mechanismType = (MechanismType)ReadByte(br),
                        flags = ReadByte(br),
                        slideAxis = (Axis)ReadByte(br),
                        rotateAxis = (Axis)ReadByte(br),

                        scaleAxis = ReadByte(br)
                    };

                    br.BaseStream.Position += 3;

                    var m = (MechanismMotion)motion.specific;

                    m.slideDistance = ReadFloatBE(br);
                    m.slideTime = ReadFloatBE(br);
                    m.slideAccelTime = ReadFloatBE(br);
                    m.slideDecelTime = ReadFloatBE(br);

                    m.rotateDistance = ReadFloatBE(br);
                    m.rotateTime = ReadFloatBE(br);
                    m.rotateAccelTime = ReadFloatBE(br);
                    m.rotateDecelTime = ReadFloatBE(br);

                    m.returnDelay = ReadFloatBE(br);
                    m.postReturnDelay = ReadFloatBE(br);

                    m.scaleAmount = ReadFloatBE(br);
                    m.scaleDuration = ReadFloatBE(br);
                }

                break;
            }

            case MotionType.Pendulum:
                motion.specific = new PendulumMotion
                {
                    flags = ReadByte(br),
                    plane = ReadByte(br),
                    pad = ReadUInt16BE(br),

                    length = ReadFloatBE(br),
                    range = ReadFloatBE(br),
                    period = ReadFloatBE(br),
                    phase = ReadFloatBE(br)
                };
                break;
        }

        return motion;
    }

    protected static void WriteMotion(BinaryWriter bw, xMotion motion)
    {
        long motionStart = bw.BaseStream.Position;

        WriteByte(bw, (byte)motion.type);
        WriteByte(bw, motion.useBanking);
        WriteUInt16BE(bw, motion.flags);

        switch (motion.type)
        {
            case MotionType.ExtendRetract:
            {
                var m = (ExtendRetractMotion)motion.specific;
                WriteVector3BE(bw, m.retPos);
                WriteVector3BE(bw, m.extDPos);
                WriteFloatBE(bw, m.extTm);
                WriteFloatBE(bw, m.extWaitTm);
                WriteFloatBE(bw, m.retTm);
                WriteFloatBE(bw, m.retWaitTm);
                break;
            }

            case MotionType.Orbit:
            {
                var m = (OrbitMotion)motion.specific;
                WriteVector3BE(bw, m.center);
                WriteFloatBE(bw, m.w);
                WriteFloatBE(bw, m.h);
                WriteFloatBE(bw, m.period);
                break;
            }

            case MotionType.Spline:
            {
                var m = (SplineMotion)motion.specific;
                if (Program.CurrentGame == GameType.BFBB)
                {
                    WriteInt32BE(bw, m.unknown);
                }
                else
                {
                    WriteUInt32BE(bw, m.splineID);
                    WriteFloatBE(bw, m.speed);
                    WriteFloatBE(bw, m.leanModifier);
                }
                break;
            }

            case MotionType.MovePoint:
            {
                var m = (MovePointMotion)motion.specific;
                WriteUInt32BE(bw, m.flags);
                WriteUInt32BE(bw, m.mpID);
                WriteFloatBE(bw, m.speed);
                break;
            }

            case MotionType.Mechanism:
            {
                var m = (MechanismMotion)motion.specific;
                WriteByte(bw, (byte)m.mechanismType);
                WriteByte(bw, m.flags);
                WriteByte(bw, (byte)m.slideAxis);
                WriteByte(bw, (byte)m.rotateAxis);

                if (Program.CurrentGame == GameType.TSSM)
                {
                    WriteByte(bw, m.scaleAxis);
                    bw.Write(new byte[2]);
                }

                WriteFloatBE(bw, m.slideDistance);
                WriteFloatBE(bw, m.slideTime);
                WriteFloatBE(bw, m.slideAccelTime);
                WriteFloatBE(bw, m.slideDecelTime);

                WriteFloatBE(bw, m.rotateDistance);
                WriteFloatBE(bw, m.rotateTime);
                WriteFloatBE(bw, m.rotateAccelTime);
                WriteFloatBE(bw, m.rotateDecelTime);

                WriteFloatBE(bw, m.returnDelay);
                WriteFloatBE(bw, m.postReturnDelay);

                if (Program.CurrentGame == GameType.TSSM)
                {
                    WriteFloatBE(bw, m.scaleAmount);
                    WriteFloatBE(bw, m.scaleDuration);
                }
                break;
            }

            case MotionType.Pendulum:
            {
                var m = (PendulumMotion)motion.specific;
                WriteByte(bw, m.flags);
                WriteByte(bw, m.plane);
                WriteUInt16BE(bw, m.pad);

                WriteFloatBE(bw, m.length);
                WriteFloatBE(bw, m.range);
                WriteFloatBE(bw, m.period);
                WriteFloatBE(bw, m.phase);
                break;
            }
        }

        //fix: ensure correct padding for a fixed length
        long expectedSize = (Program.CurrentGame == GameType.BFBB) ? 0x30 : 0x3C;
        long bytesWritten = bw.BaseStream.Position - motionStart;
        
        if (bytesWritten < expectedSize)
        {
            int padNeeded = (int)(expectedSize - bytesWritten);
            bw.Write(new byte[padNeeded]);
        }
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MotionType : byte
{
    ExtendRetract = 0,
    Orbit = 1,
    Spline = 2,
    MovePoint = 3,
    Mechanism = 4,
    Pendulum = 5,
    None = 6
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MechanismType : byte
{
    Slide = 0,
    Rotate = 1,
    SlideAndRotate = 2,
    SlideThenRotate = 3,
    RotateThenSlide = 4
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Axis : byte
{
    X = 0,
    Y = 1,
    Z = 2
}

public class ExtendRetractMotion : MotionSpecificData
{
    public xVec3 retPos { get; set; }
    public xVec3 extDPos { get; set; }
    public float extTm { get; set; }
    public float extWaitTm { get; set; }
    public float retTm { get; set; }
    public float retWaitTm { get; set; }
}

public class OrbitMotion : MotionSpecificData
{
    public xVec3 center { get; set; }
    public float w { get; set; }
    public float h { get; set; }
    public float period { get; set; }
}

public class SplineMotion : MotionSpecificData
{
    public int unknown { get; set; } //bfbb only
    [JsonConverter(typeof(AssetIDConverter))] //movie momento
    public uint splineID { get; set; }
    public float speed { get; set; }
    public float leanModifier { get; set; }
}

public class MovePointMotion : MotionSpecificData
{
    public uint flags { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint mpID { get; set; }
    public float speed { get; set; }
}

public class MechanismMotion : MotionSpecificData
{
    public MechanismType mechanismType { get; set; }
    public byte flags { get; set; }
    public Axis slideAxis { get; set; }
    public Axis rotateAxis { get; set; }
    //movie movie movie movie movie movie
    public byte scaleAxis { get; set; }
    public float slideDistance { get; set; }
    public float slideTime { get; set; }
    public float slideAccelTime { get; set; }
    public float slideDecelTime { get; set; }
    public float rotateDistance { get; set; }
    public float rotateTime { get; set; }
    public float rotateAccelTime { get; set; }
    public float rotateDecelTime { get; set; }
    public float returnDelay { get; set; }
    public float postReturnDelay { get; set; }
    //motion video only
    public float scaleAmount { get; set; }
    public float scaleDuration { get; set; }
}

public class PendulumMotion : MotionSpecificData
{
    public byte flags { get; set; }
    public byte plane { get; set; }
    public ushort pad { get; set; }
    public float length { get; set; }
    public float range { get; set; }
    public float period { get; set; }
    public float phase { get; set; }
}