using System;
using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class PLATParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        var plat = new PLAT
        {
            type = (PlatformType)ReadByte(br),
            pad = ReadByte(br),
            flags = ReadUInt16BE(br)
        };

        switch (plat.type)
        {
            case PlatformType.Conveyor:
                plat.specific = new ConveyorPlatform
                {
                    speed = ReadFloatBE(br)
                };
                break;

            case PlatformType.Falling:
                plat.specific = new FallingPlatform
                {
                    speed = ReadFloatBE(br),
                    bustModelID = ReadUInt32BE(br)
                };
                break;

            case PlatformType.FR:
                plat.specific = new FRPlatform
                {
                    fspeed = ReadFloatBE(br),
                    rspeed = ReadFloatBE(br),
                    retDelay = ReadFloatBE(br),
                    postRetDelay = ReadFloatBE(br)
                };
                break;

            case PlatformType.Breakaway:
                if (Program.CurrentGame == GameType.BFBB)
                {
                    plat.specific = new BreakawayPlatformBFBB
                    {
                        delay = ReadFloatBE(br),
                        bustModelID = ReadUInt32BE(br),
                        resetDelay = ReadFloatBE(br),
                        breakFlags = ReadUInt32BE(br)
                    };
                }
                else if (Program.CurrentGame == GameType.TSSM)
                {
                    plat.specific = new BreakawayPlatformTSSM
                    {
                        warningTime = ReadFloatBE(br),
                        collapsedIdleTime = ReadFloatBE(br),
                        breakFlags = ReadUInt32BE(br),
                        collisionOffTime = ReadFloatBE(br),
                    };
                }
                break;

            case PlatformType.Springboard:
                plat.specific = new SpringboardPlatform
                {
                    jmph = [ReadFloatBE(br), ReadFloatBE(br), ReadFloatBE(br)],
                    jmpbounce = ReadFloatBE(br),
                    animID = [ReadUInt32BE(br), ReadUInt32BE(br), ReadUInt32BE(br)],
                    jmpdir = [ReadFloatBE(br), ReadFloatBE(br), ReadFloatBE(br)],
                    springflags = ReadUInt32BE(br)
                };
                break;

            case PlatformType.Teeter:
                plat.specific = new TeeterPlatform
                {
                    initialTilt = ReadFloatBE(br),
                    maxTilt = ReadFloatBE(br),
                    invMass = ReadFloatBE(br)
                };
                break;

            case PlatformType.Paddle:
                plat.specific = new PaddlePlatform
                {
                    startOrient = ReadInt32BE(br),
                    countOrient = ReadInt32BE(br),
                    orientLoop = ReadFloatBE(br),
                    orient =
                    [
                        ReadFloatBE(br),
                        ReadFloatBE(br),
                        ReadFloatBE(br),
                        ReadFloatBE(br),
                        ReadFloatBE(br),
                        ReadFloatBE(br)
                    ],
                    paddleFlags = ReadUInt32BE(br),
                    rotateSpeed = ReadFloatBE(br),
                    accelTime = ReadFloatBE(br),
                    decelTime = ReadFloatBE(br),
                    hubRadius = ReadFloatBE(br)
                };
                break;
        }

        long motionOffset = (Program.CurrentGame == GameType.BFBB) ? 0x90 : 0x8C;
        br.BaseStream.Position = assetStart + motionOffset;
        plat.motion = ReadMotion(br);

        return plat;
    }

    public override object Serialize(object obj)
    {
        PLAT plat = (PLAT)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteByte(bw, (byte)plat.type);
        WriteByte(bw, plat.pad);
        WriteUInt16BE(bw, plat.flags);

        switch (plat.type)
        {
            case PlatformType.Conveyor:
                WriteFloatBE(bw, ((ConveyorPlatform)plat.specific).speed);
                break;

            case PlatformType.Falling:
                var falling = (FallingPlatform)plat.specific;
                WriteFloatBE(bw, falling.speed);
                WriteUInt32BE(bw, falling.bustModelID);
                break;

            case PlatformType.FR:
                var fr = (FRPlatform)plat.specific;
                WriteFloatBE(bw, fr.fspeed);
                WriteFloatBE(bw, fr.rspeed);
                WriteFloatBE(bw, fr.retDelay);
                WriteFloatBE(bw, fr.postRetDelay);
                break;

            case PlatformType.Breakaway:
                if (Program.CurrentGame == GameType.BFBB)
                {
                    var breakaway = (BreakawayPlatformBFBB)plat.specific;
                    WriteFloatBE(bw, breakaway.delay);
                    WriteUInt32BE(bw, breakaway.bustModelID);
                    WriteFloatBE(bw, breakaway.resetDelay);
                    WriteUInt32BE(bw, breakaway.breakFlags);
                }
                else if (Program.CurrentGame == GameType.TSSM)
                {
                    var breakaway = (BreakawayPlatformTSSM)plat.specific;
                    WriteFloatBE(bw, breakaway.warningTime);
                    WriteFloatBE(bw, breakaway.collapsedIdleTime);
                    WriteUInt32BE(bw, breakaway.breakFlags);
                    WriteFloatBE(bw, breakaway.collisionOffTime);
                }
                break;

            case PlatformType.Springboard:
                var sb = (SpringboardPlatform)plat.specific;
                foreach (float h in sb.jmph) WriteFloatBE(bw, h);
                WriteFloatBE(bw, sb.jmpbounce);
                foreach (uint id in sb.animID) WriteUInt32BE(bw, id);
                foreach (float d in sb.jmpdir) WriteFloatBE(bw, d);
                WriteUInt32BE(bw, sb.springflags);
                break;

            case PlatformType.Teeter:
                var teeter = (TeeterPlatform)plat.specific;
                WriteFloatBE(bw, teeter.initialTilt);
                WriteFloatBE(bw, teeter.maxTilt);
                WriteFloatBE(bw, teeter.invMass);
                break;

            case PlatformType.Paddle:
                var paddle = (PaddlePlatform)plat.specific;
                WriteInt32BE(bw, paddle.startOrient);
                WriteInt32BE(bw, paddle.countOrient);
                WriteFloatBE(bw, paddle.orientLoop);
                foreach (float f in paddle.orient)
                    WriteFloatBE(bw, f);
                WriteUInt32BE(bw, paddle.paddleFlags);
                WriteFloatBE(bw, paddle.rotateSpeed);
                WriteFloatBE(bw, paddle.accelTime);
                WriteFloatBE(bw, paddle.decelTime);
                WriteFloatBE(bw, paddle.hubRadius);
                break;
        }

        int padNeeded = 0x3C - (int)ms.Length;
        if (padNeeded > 0) 
            bw.Write(new byte[padNeeded]);

        WriteMotion(bw, plat.motion);

        return ms.ToArray();
    }
}

public class PLAT
{
    public PlatformType type { get; set; }
    public byte pad { get; set; }
    public ushort flags { get; set; }

    public PlatformSpecificData specific { get; set; }

    public xMotion motion { get; set; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ConveyorPlatform), "Conveyor")]
[JsonDerivedType(typeof(FallingPlatform), "Falling")]
[JsonDerivedType(typeof(FRPlatform), "FR")]
[JsonDerivedType(typeof(BreakawayPlatformBFBB), "BreakawayBFBB")]
[JsonDerivedType(typeof(BreakawayPlatformTSSM), "BreakawayTSSM")]
[JsonDerivedType(typeof(SpringboardPlatform), "Springboard")]
[JsonDerivedType(typeof(TeeterPlatform), "Teeter")]
[JsonDerivedType(typeof(PaddlePlatform), "Paddle")]
public abstract class PlatformSpecificData { }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PlatformType : byte
{
    ExtendRetract = 0,
    Orbit = 1,
    Spline = 2,
    MovePoint = 3,
    Mechanism = 4,
    Pendulum = 5,
    Conveyor = 6,
    Falling = 7,
    FR = 8,
    Breakaway = 9,
    Springboard = 10,
    Teeter = 11,
    Paddle = 12,
    FullyManipulable = 13
}

public class ConveyorPlatform : PlatformSpecificData
{
    public float speed { get; set; }
}

public class FallingPlatform : PlatformSpecificData
{
    public float speed { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint bustModelID { get; set; }
}

public class FRPlatform : PlatformSpecificData
{
    public float fspeed { get; set; }
    public float rspeed { get; set; }
    public float retDelay { get; set; }
    public float postRetDelay { get; set; }
}

public class BreakawayPlatformBFBB : PlatformSpecificData
{
    public float delay { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint bustModelID { get; set; }
    public float resetDelay { get; set; }
    public uint breakFlags { get; set; }
}

public class BreakawayPlatformTSSM : PlatformSpecificData
{
    public float warningTime { get; set; }
    public float collapsedIdleTime { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint breakFlags { get; set; }
    public float collisionOffTime { get; set; }
}

public class SpringboardPlatform : PlatformSpecificData
{
    public float[] jmph { get; set; } = new float[3];
    public float jmpbounce { get; set; }
    [JsonConverter(typeof(AssetIDArrayConverter))]
    public uint[] animID { get; set; } = new uint[3];
    public float[] jmpdir { get; set; } = new float[3];
    public uint springflags { get; set; }
}

public class TeeterPlatform : PlatformSpecificData
{
    public float initialTilt { get; set; }
    public float maxTilt { get; set; }
    public float invMass { get; set; }
}

public class PaddlePlatform : PlatformSpecificData
{
    public int startOrient { get; set; }
    public int countOrient { get; set; }
    public float orientLoop { get; set; }
    public float[] orient { get; set; }
    public uint paddleFlags { get; set; }
    public float rotateSpeed { get; set; }
    public float accelTime { get; set; }
    public float decelTime { get; set; }
    public float hubRadius { get; set; }
}