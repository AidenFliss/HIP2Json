using System.IO;

namespace HIP2Json;

public sealed class PLATParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        PlatformType type = (PlatformType)ReadByte(br);
        br.ReadBytes(1);
        ushort flags = ReadUInt16BE(br);
        var plat = new PLAT { type = type, flags = flags };

        switch (plat.type)
        {
            case PlatformType.Conveyor:
                plat.specific = new ConveyorPlatform { speed = ReadFloatBE(br) };
                break;

            case PlatformType.Falling:
                plat.specific = new FallingPlatform { speed = ReadFloatBE(br), bustModelID = ReadUInt32BE(br) };
                break;

            case PlatformType.FR:
                plat.specific = new FRPlatform
                {
                    fspeed = ReadFloatBE(br),
                    rspeed = ReadFloatBE(br),
                    retDelay = ReadFloatBE(br),
                    postRetDelay = ReadFloatBE(br),
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
                        breakFlags = ReadUInt32BE(br),
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
                    springflags = ReadUInt32BE(br),
                };
                break;

            case PlatformType.Teeter:
                plat.specific = new TeeterPlatform
                {
                    initialTilt = ReadFloatBE(br),
                    maxTilt = ReadFloatBE(br),
                    invMass = ReadFloatBE(br),
                };
                break;

            case PlatformType.Paddle:
                plat.specific = new PaddlePlatform
                {
                    startOrient = ReadInt32BE(br),
                    countOrient = ReadInt32BE(br),
                    orientLoop = ReadFloatBE(br),
                    orient = [ReadFloatBE(br), ReadFloatBE(br), ReadFloatBE(br), ReadFloatBE(br), ReadFloatBE(br), ReadFloatBE(br)],
                    paddleFlags = ReadUInt32BE(br),
                    rotateSpeed = ReadFloatBE(br),
                    accelTime = ReadFloatBE(br),
                    decelTime = ReadFloatBE(br),
                    hubRadius = ReadFloatBE(br),
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
        bw.Write(new byte[1]);
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
                foreach (float h in sb.jmph)
                    WriteFloatBE(bw, h);
                WriteFloatBE(bw, sb.jmpbounce);
                foreach (uint id in sb.animID)
                    WriteUInt32BE(bw, id);
                foreach (float d in sb.jmpdir)
                    WriteFloatBE(bw, d);
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
    public ushort flags { get; set; }

    public PlatformSpecificData specific { get; set; }

    public xMotion motion { get; set; }
}
