using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class UIMParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        UIM uim = new()
        {
            cmdCount = ReadByte(br),
            inn = ReadByte(br),
            pad0 = ReadByte(br),
            pad1 = ReadByte(br),
            cmdSize = ReadUInt32BE(br),
            totalTime = ReadFloatBE(br),
            loopTime = ReadFloatBE(br),
        };

        uim.commands = Enumerable.Range(0, uim.cmdCount)
            .Select(_ => ReadCommand(br))
            .ToArray();

        return uim;
    }

    public override object Serialize(object obj)
    {
        UIM uim = (UIM)obj;

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        uim.cmdCount = (byte)(uim.commands?.Length ?? 0);

        WriteByte(bw, uim.cmdCount);
        WriteByte(bw, uim.inn);
        WriteByte(bw, uim.pad0);
        WriteByte(bw, uim.pad1);
        WriteUInt32BE(bw, uim.cmdSize);
        WriteFloatBE(bw, uim.totalTime);
        WriteFloatBE(bw, uim.loopTime);

        if (uim.commands != null)
        {
            foreach (var command in uim.commands)
                WriteCommand(bw, command);
        }

        return ms.ToArray();
    }

    private object ReadCommand(BinaryReader br)
    {
        UIMCommandType type = (UIMCommandType)ReadUInt32BE(br);

        float startTime = ReadFloatBE(br);
        float endTime = ReadFloatBE(br);
        float accelTime = ReadFloatBE(br);
        float decelTime = ReadFloatBE(br);

        byte enabled = ReadByte(br);

        byte pad0 = ReadByte(br);
        byte pad1 = ReadByte(br);
        byte pad2 = ReadByte(br);

        switch (type)
        {
            case UIMCommandType.Move:
                return new UIMMoveCommand
                {
                    type = type,
                    startTime = startTime,
                    endTime = endTime,
                    accelTime = accelTime,
                    decelTime = decelTime,
                    enabled = enabled,
                    pad0 = pad0,
                    pad1 = pad1,
                    pad2 = pad2,

                    distX = ReadFloatBE(br),
                    distY = ReadFloatBE(br),
                };


            case UIMCommandType.Scale:
                return new UIMScaleCommand
                {
                    type = type,
                    startTime = startTime,
                    endTime = endTime,
                    accelTime = accelTime,
                    decelTime = decelTime,
                    enabled = enabled,
                    pad0 = pad0,
                    pad1 = pad1,
                    pad2 = pad2,

                    amountX = ReadFloatBE(br),
                    amountY = ReadFloatBE(br),

                    centerPivot = ReadByte(br),
                    centerPad0 = ReadByte(br),
                    centerPad1 = ReadByte(br),
                    centerPad2 = ReadByte(br),

                    centerOffsetX = ReadFloatBE(br),
                    centerOffsetY = ReadFloatBE(br),
                };


            case UIMCommandType.Rotate:
                return new UIMRotateCommand
                {
                    type = type,
                    startTime = startTime,
                    endTime = endTime,
                    accelTime = accelTime,
                    decelTime = decelTime,
                    enabled = enabled,
                    pad0 = pad0,
                    pad1 = pad1,
                    pad2 = pad2,

                    rotation = ReadFloatBE(br),
                    centerOffsetX = ReadFloatBE(br),
                    centerOffsetY = ReadFloatBE(br),
                };


            case UIMCommandType.Opacity:
                return new UIMOpacityCommand
                {
                    type = type,
                    startTime = startTime,
                    endTime = endTime,
                    accelTime = accelTime,
                    decelTime = decelTime,
                    enabled = enabled,
                    pad0 = pad0,
                    pad1 = pad1,
                    pad2 = pad2,

                    startOpacity = ReadByte(br),
                    endOpacity = ReadByte(br),
                    colorPad0 = ReadByte(br),
                    colorPad1 = ReadByte(br),
                };
            
            case UIMCommandType.AbsoluteScale:
                return new UIMAbsoluteScaleCommand
                {
                    type = type,
                    startTime = startTime,
                    endTime = endTime,
                    accelTime = accelTime,
                    decelTime = decelTime,
                    enabled = enabled,
                    pad0 = pad0,
                    pad1 = pad1,
                    pad2 = pad2,

                    startX = ReadFloatBE(br),
                    startY = ReadFloatBE(br),
                    endX = ReadFloatBE(br),
                    endY = ReadFloatBE(br),

                    centerPivot = ReadByte(br),
                    textScale = ReadByte(br),
                    scalePad0 = ReadByte(br),
                    scalePad1 = ReadByte(br),
                };


            case UIMCommandType.Brightness:
                return new UIMBrightnessCommand
                {
                    type = type,
                    startTime = startTime,
                    endTime = endTime,
                    accelTime = accelTime,
                    decelTime = decelTime,
                    enabled = enabled,
                    pad0 = pad0,
                    pad1 = pad1,
                    pad2 = pad2,

                    startBrightness = ReadByte(br),
                    endBrightness = ReadByte(br),
                    brightnessPad0 = ReadByte(br),
                    brightnessPad1 = ReadByte(br),
                };


            case UIMCommandType.Color:
                return new UIMColorCommand
                {
                    type = type,
                    startTime = startTime,
                    endTime = endTime,
                    accelTime = accelTime,
                    decelTime = decelTime,
                    enabled = enabled,
                    pad0 = pad0,
                    pad1 = pad1,
                    pad2 = pad2,

                    startRed = ReadByte(br),
                    startGreen = ReadByte(br),
                    startBlue = ReadByte(br),

                    endRed = ReadByte(br),
                    endGreen = ReadByte(br),
                    endBlue = ReadByte(br),

                    colorPad0 = ReadByte(br),
                    colorPad1 = ReadByte(br),
                };


            case UIMCommandType.UVScroll:
                return new UIMUVScrollCommand
                {
                    type = type,
                    startTime = startTime,
                    endTime = endTime,
                    accelTime = accelTime,
                    decelTime = decelTime,
                    enabled = enabled,
                    pad0 = pad0,
                    pad1 = pad1,
                    pad2 = pad2,

                    amountU = ReadFloatBE(br),
                    amountV = ReadFloatBE(br),
                };
            
            default:
                return null;
        }
    }


    private void WriteCommand(BinaryWriter bw, object command)
    {
        switch (command)
        {
            case UIMMoveCommand move:
                WriteUInt32BE(bw, (uint)move.type);

                WriteFloatBE(bw, move.startTime);
                WriteFloatBE(bw, move.endTime);
                WriteFloatBE(bw, move.accelTime);
                WriteFloatBE(bw, move.decelTime);

                WriteByte(bw, move.enabled);
                WriteByte(bw, move.pad0);
                WriteByte(bw, move.pad1);
                WriteByte(bw, move.pad2);

                WriteFloatBE(bw, move.distX);
                WriteFloatBE(bw, move.distY);
                break;


            case UIMScaleCommand scale:
                WriteUInt32BE(bw, (uint)scale.type);

                WriteFloatBE(bw, scale.startTime);
                WriteFloatBE(bw, scale.endTime);
                WriteFloatBE(bw, scale.accelTime);
                WriteFloatBE(bw, scale.decelTime);

                WriteByte(bw, scale.enabled);
                WriteByte(bw, scale.pad0);
                WriteByte(bw, scale.pad1);
                WriteByte(bw, scale.pad2);

                WriteFloatBE(bw, scale.amountX);
                WriteFloatBE(bw, scale.amountY);

                WriteByte(bw, scale.centerPivot);
                WriteByte(bw, scale.centerPad0);
                WriteByte(bw, scale.centerPad1);
                WriteByte(bw, scale.centerPad2);

                WriteFloatBE(bw, scale.centerOffsetX);
                WriteFloatBE(bw, scale.centerOffsetY);
                break;


            case UIMRotateCommand rotate:
                WriteUInt32BE(bw, (uint)rotate.type);

                WriteFloatBE(bw, rotate.startTime);
                WriteFloatBE(bw, rotate.endTime);
                WriteFloatBE(bw, rotate.accelTime);
                WriteFloatBE(bw, rotate.decelTime);

                WriteByte(bw, rotate.enabled);
                WriteByte(bw, rotate.pad0);
                WriteByte(bw, rotate.pad1);
                WriteByte(bw, rotate.pad2);

                WriteFloatBE(bw, rotate.rotation);
                WriteFloatBE(bw, rotate.centerOffsetX);
                WriteFloatBE(bw, rotate.centerOffsetY);
                break;


            case UIMOpacityCommand opacity:
                WriteUInt32BE(bw, (uint)opacity.type);

                WriteFloatBE(bw, opacity.startTime);
                WriteFloatBE(bw, opacity.endTime);
                WriteFloatBE(bw, opacity.accelTime);
                WriteFloatBE(bw, opacity.decelTime);

                WriteByte(bw, opacity.enabled);
                WriteByte(bw, opacity.pad0);
                WriteByte(bw, opacity.pad1);
                WriteByte(bw, opacity.pad2);

                WriteByte(bw, opacity.startOpacity);
                WriteByte(bw, opacity.endOpacity);
                WriteByte(bw, opacity.colorPad0);
                WriteByte(bw, opacity.colorPad1);
                break;

            case UIMAbsoluteScaleCommand absoluteScale:
                WriteUInt32BE(bw, (uint)absoluteScale.type);

                WriteFloatBE(bw, absoluteScale.startTime);
                WriteFloatBE(bw, absoluteScale.endTime);
                WriteFloatBE(bw, absoluteScale.accelTime);
                WriteFloatBE(bw, absoluteScale.decelTime);

                WriteByte(bw, absoluteScale.enabled);
                WriteByte(bw, absoluteScale.pad0);
                WriteByte(bw, absoluteScale.pad1);
                WriteByte(bw, absoluteScale.pad2);

                WriteFloatBE(bw, absoluteScale.startX);
                WriteFloatBE(bw, absoluteScale.startY);
                WriteFloatBE(bw, absoluteScale.endX);
                WriteFloatBE(bw, absoluteScale.endY);

                WriteByte(bw, absoluteScale.centerPivot);
                WriteByte(bw, absoluteScale.textScale);
                WriteByte(bw, absoluteScale.scalePad0);
                WriteByte(bw, absoluteScale.scalePad1);
                break;


            case UIMBrightnessCommand brightness:
                WriteUInt32BE(bw, (uint)brightness.type);

                WriteFloatBE(bw, brightness.startTime);
                WriteFloatBE(bw, brightness.endTime);
                WriteFloatBE(bw, brightness.accelTime);
                WriteFloatBE(bw, brightness.decelTime);

                WriteByte(bw, brightness.enabled);
                WriteByte(bw, brightness.pad0);
                WriteByte(bw, brightness.pad1);
                WriteByte(bw, brightness.pad2);

                WriteByte(bw, brightness.startBrightness);
                WriteByte(bw, brightness.endBrightness);
                WriteByte(bw, brightness.brightnessPad0);
                WriteByte(bw, brightness.brightnessPad1);
                break;


            case UIMColorCommand color:
                WriteUInt32BE(bw, (uint)color.type);

                WriteFloatBE(bw, color.startTime);
                WriteFloatBE(bw, color.endTime);
                WriteFloatBE(bw, color.accelTime);
                WriteFloatBE(bw, color.decelTime);

                WriteByte(bw, color.enabled);
                WriteByte(bw, color.pad0);
                WriteByte(bw, color.pad1);
                WriteByte(bw, color.pad2);

                WriteByte(bw, color.startRed);
                WriteByte(bw, color.startGreen);
                WriteByte(bw, color.startBlue);

                WriteByte(bw, color.endRed);
                WriteByte(bw, color.endGreen);
                WriteByte(bw, color.endBlue);

                WriteByte(bw, color.colorPad0);
                WriteByte(bw, color.colorPad1);
                break;


            case UIMUVScrollCommand uv:
                WriteUInt32BE(bw, (uint)uv.type);

                WriteFloatBE(bw, uv.startTime);
                WriteFloatBE(bw, uv.endTime);
                WriteFloatBE(bw, uv.accelTime);
                WriteFloatBE(bw, uv.decelTime);

                WriteByte(bw, uv.enabled);
                WriteByte(bw, uv.pad0);
                WriteByte(bw, uv.pad1);
                WriteByte(bw, uv.pad2);

                WriteFloatBE(bw, uv.amountU);
                WriteFloatBE(bw, uv.amountV);
                break;
        }
    }
}

public class UIM
{
    public byte cmdCount { get; set; }
    public byte inn { get; set; }
    public byte pad0 { get; set; }
    public byte pad1 { get; set; }
    public uint cmdSize { get; set; }
    public float totalTime { get; set; }
    public float loopTime { get; set; }
    public object[] commands { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UIMCommandType : uint
{
    Move = 0,
    Scale = 1,
    Rotate = 2,
    Opacity = 3,
    AbsoluteScale = 4,
    Brightness = 5,
    Color = 6,
    UVScroll = 7
}

public abstract class UIMCommand
{
    public UIMCommandType type { get; set; }
    public float startTime { get; set; }
    public float endTime { get; set; }
    public float accelTime { get; set; }
    public float decelTime { get; set; }
    public byte enabled { get; set; }
    public byte pad0 { get; set; }
    public byte pad1 { get; set; }
    public byte pad2 { get; set; }
}

public class UIMMoveCommand
{
    public UIMCommandType type { get; set; }
    public float startTime { get; set; }
    public float endTime { get; set; }
    public float accelTime { get; set; }
    public float decelTime { get; set; }
    public byte enabled { get; set; }
    public byte pad0 { get; set; }
    public byte pad1 { get; set; }
    public byte pad2 { get; set; }
    public float distX { get; set; }
    public float distY { get; set; }
}


public class UIMScaleCommand
{
    public UIMCommandType type { get; set; }
    public float startTime { get; set; }
    public float endTime { get; set; }
    public float accelTime { get; set; }
    public float decelTime { get; set; }
    public byte enabled { get; set; }
    public byte pad0 { get; set; }
    public byte pad1 { get; set; }
    public byte pad2 { get; set; }
    public float amountX { get; set; }
    public float amountY { get; set; }
    public byte centerPivot { get; set; }
    public byte centerPad0 { get; set; }
    public byte centerPad1 { get; set; }
    public byte centerPad2 { get; set; }
    public float centerOffsetX { get; set; }
    public float centerOffsetY { get; set; }
}


public class UIMRotateCommand
{
    public UIMCommandType type { get; set; }
    public float startTime { get; set; }
    public float endTime { get; set; }
    public float accelTime { get; set; }
    public float decelTime { get; set; }
    public byte enabled { get; set; }
    public byte pad0 { get; set; }
    public byte pad1 { get; set; }
    public byte pad2 { get; set; }
    public float rotation { get; set; }
    public float centerOffsetX { get; set; }
    public float centerOffsetY { get; set; }
}


public class UIMOpacityCommand
{
    public UIMCommandType type { get; set; }
    public float startTime { get; set; }
    public float endTime { get; set; }
    public float accelTime { get; set; }
    public float decelTime { get; set; }
    public byte enabled { get; set; }
    public byte pad0 { get; set; }
    public byte pad1 { get; set; }
    public byte pad2 { get; set; }
    public byte startOpacity { get; set; }
    public byte endOpacity { get; set; }
    public byte colorPad0 { get; set; }
    public byte colorPad1 { get; set; }
}


public class UIMAbsoluteScaleCommand
{
    public UIMCommandType type { get; set; }
    public float startTime { get; set; }
    public float endTime { get; set; }
    public float accelTime { get; set; }
    public float decelTime { get; set; }
    public byte enabled { get; set; }
    public byte pad0 { get; set; }
    public byte pad1 { get; set; }
    public byte pad2 { get; set; }
    public float startX { get; set; }
    public float startY { get; set; }
    public float endX { get; set; }
    public float endY { get; set; }
    public byte centerPivot { get; set; }
    public byte textScale { get; set; }
    public byte scalePad0 { get; set; }
    public byte scalePad1 { get; set; }
}


public class UIMBrightnessCommand
{
    public UIMCommandType type { get; set; }
    public float startTime { get; set; }
    public float endTime { get; set; }
    public float accelTime { get; set; }
    public float decelTime { get; set; }
    public byte enabled { get; set; }
    public byte pad0 { get; set; }
    public byte pad1 { get; set; }
    public byte pad2 { get; set; }
    public byte startBrightness { get; set; }
    public byte endBrightness { get; set; }
    public byte brightnessPad0 { get; set; }
    public byte brightnessPad1 { get; set; }
}


public class UIMColorCommand
{
    public UIMCommandType type { get; set; }
    public float startTime { get; set; }
    public float endTime { get; set; }
    public float accelTime { get; set; }
    public float decelTime { get; set; }
    public byte enabled { get; set; }
    public byte pad0 { get; set; }
    public byte pad1 { get; set; }
    public byte pad2 { get; set; }
    public byte startRed { get; set; }
    public byte startGreen { get; set; }
    public byte startBlue { get; set; }
    public byte endRed { get; set; }
    public byte endGreen { get; set; }
    public byte endBlue { get; set; }
    public byte colorPad0 { get; set; }
    public byte colorPad1 { get; set; }
}


public class UIMUVScrollCommand
{
    public UIMCommandType type { get; set; }

    public float startTime { get; set; }
    public float endTime { get; set; }
    public float accelTime { get; set; }
    public float decelTime { get; set; }
    public byte enabled { get; set; }

    public byte pad0 { get; set; }
    public byte pad1 { get; set; }
    public byte pad2 { get; set; }
    public float amountU { get; set; }
    public float amountV { get; set; }
}