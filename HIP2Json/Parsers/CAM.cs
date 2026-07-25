using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class CAMParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        var cam = new CAM
        {
            pos = ReadVector3BE(br),
            forward = ReadVector3BE(br),
            up = ReadVector3BE(br),
            left = ReadVector3BE(br),
            viewOffset = ReadVector3BE(br),

            offsetStartFrames = ReadInt16BE(br),
            offsetEndFrames = ReadInt16BE(br),

            fov = ReadFloatBE(br),
            transitionTime = ReadFloatBE(br),
            transitionType = (TransitionType)ReadInt32BE(br),

            flags = ReadUInt32BE(br),

            fadeUp = ReadFloatBE(br),
            fadeDown = ReadFloatBE(br),
        };

        long specificPos = br.BaseStream.Position;

        br.BaseStream.Position = 0x78;

        cam.validFlags = ReadUInt32BE(br);
        cam.marker1 = ReadUInt32BE(br);
        cam.marker2 = ReadUInt32BE(br);
        cam.camType = (CamType)ReadByte(br);

        br.BaseStream.Position = specificPos;
        cam.specific = ReadCamSpecific(br, cam.camType);

        return cam;
    }

    public override object Serialize(object obj)
    {
        CAM cam = (CAM)obj;

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteVector3BE(bw, cam.pos);
        WriteVector3BE(bw, cam.forward);
        WriteVector3BE(bw, cam.up);
        WriteVector3BE(bw, cam.left);
        WriteVector3BE(bw, cam.viewOffset);

        WriteInt16BE(bw, cam.offsetStartFrames);
        WriteInt16BE(bw, cam.offsetEndFrames);

        WriteFloatBE(bw, cam.fov);
        WriteFloatBE(bw, cam.transitionTime);
        WriteInt32BE(bw, (int)cam.transitionType);

        WriteUInt32BE(bw, cam.flags);

        WriteFloatBE(bw, cam.fadeUp);
        WriteFloatBE(bw, cam.fadeDown);

        if (cam.specific != null)
            WriteCamSpecific(bw, cam.specific);

        while (ms.Length < 0x78)
            bw.Write((byte)0);

        WriteUInt32BE(bw, cam.validFlags);
        WriteUInt32BE(bw, cam.marker1);
        WriteUInt32BE(bw, cam.marker2);
        WriteByte(bw, (byte)cam.camType);

        return ms.ToArray();
    }

    private CamSpecificData ReadCamSpecific(BinaryReader br, CamType camType)
    {
        return camType switch
        {
            CamType.Follow => new FollowCam
            {
                rotation = ReadFloatBE(br),
                distance = ReadFloatBE(br),
                height = ReadFloatBE(br),
                rubberBand = ReadFloatBE(br),
                startSpeed = ReadFloatBE(br),
                endSpeed = ReadFloatBE(br),
            },
            CamType.Shoulder => new ShoulderCam
            {
                distance = ReadFloatBE(br),
                height = ReadFloatBE(br),
                realignSpeed = ReadFloatBE(br),
                realignDelay = ReadFloatBE(br),
            },
            CamType.Static => new StaticCam { unused = ReadUInt32BE(br) },
            CamType.Path => new PathCam
            {
                assetID = ReadUInt32BE(br),
                timeEnd = ReadFloatBE(br),
                timeDelay = ReadFloatBE(br),
            },
            CamType.StaticFollow => new StaticFollowCam { rubberBand = ReadFloatBE(br) },
            _ => null,
        };
    }

    private void WriteCamSpecific(BinaryWriter bw, CamSpecificData specific)
    {
        switch (specific)
        {
            case FollowCam follow:
                WriteFloatBE(bw, follow.rotation);
                WriteFloatBE(bw, follow.distance);
                WriteFloatBE(bw, follow.height);
                WriteFloatBE(bw, follow.rubberBand);
                WriteFloatBE(bw, follow.startSpeed);
                WriteFloatBE(bw, follow.endSpeed);
                break;

            case ShoulderCam shoulder:
                WriteFloatBE(bw, shoulder.distance);
                WriteFloatBE(bw, shoulder.height);
                WriteFloatBE(bw, shoulder.realignSpeed);
                WriteFloatBE(bw, shoulder.realignDelay);
                break;

            case StaticCam staticCam:
                WriteUInt32BE(bw, staticCam.unused);
                break;

            case PathCam path:
                WriteUInt32BE(bw, path.assetID);
                WriteFloatBE(bw, path.timeEnd);
                WriteFloatBE(bw, path.timeDelay);
                break;

            case StaticFollowCam staticFollow:
                WriteFloatBE(bw, staticFollow.rubberBand);
                break;
        }
    }
}

public class CAM
{
    public xVec3 pos { get; set; }
    public xVec3 forward { get; set; }
    public xVec3 up { get; set; }
    public xVec3 left { get; set; }
    public xVec3 viewOffset { get; set; }

    public short offsetStartFrames { get; set; }
    public short offsetEndFrames { get; set; }

    public float fov { get; set; }
    public float transitionTime { get; set; }
    public TransitionType transitionType { get; set; }

    public uint flags { get; set; }

    public float fadeUp { get; set; }
    public float fadeDown { get; set; }

    public uint validFlags { get; set; }

    [JsonConverter(typeof(AssetIDConverter))]
    public uint marker1 { get; set; }

    [JsonConverter(typeof(AssetIDConverter))]
    public uint marker2 { get; set; }

    public CamType camType { get; set; }

    public CamSpecificData specific { get; set; }
}
