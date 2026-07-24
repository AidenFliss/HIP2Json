using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class UIMParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        byte cmdCount = ReadByte(br);
        byte inn = ReadByte(br);
        br.ReadBytes(2);
        uint cmdSize = ReadUInt32BE(br);
        float totalTime = ReadFloatBE(br);
        float loopTime = ReadFloatBE(br);

        UIMCommand[] commands = Enumerable.Range(0, cmdCount)
            .Select(_ => ReadCommand(br))
            .ToArray();

        return new UIM
        {
            cmdCount = cmdCount,
            inn = inn,
            cmdSize = cmdSize,
            totalTime = totalTime,
            loopTime = loopTime,
            commands = commands
        };
    }

    public override object Serialize(object obj)
    {
        UIM uim = (UIM)obj;

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        uim.cmdCount = (byte)(uim.commands?.Length ?? 0);

        WriteByte(bw, uim.cmdCount);
        WriteByte(bw, uim.inn);
        bw.Write(new byte[2]);
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

    private UIMCommand ReadCommand(BinaryReader br)
    {
        UIMCommandType type = (UIMCommandType)ReadUInt32BE(br);

        float startTime = ReadFloatBE(br);
        float endTime = ReadFloatBE(br);
        float accelTime = ReadFloatBE(br);
        float decelTime = ReadFloatBE(br);

        byte enabled = ReadByte(br);

        br.ReadBytes(3);

        switch (type)
        {
            case UIMCommandType.Move:
                float distX = ReadFloatBE(br);
                float distY = ReadFloatBE(br);

                return new UIMMoveCommand
                {
                    type = type,
                    startTime = startTime,
                    endTime = endTime,
                    accelTime = accelTime,
                    decelTime = decelTime,
                    enabled = enabled,
                    distX = distX,
                    distY = distY
                };

            case UIMCommandType.Scale:
                float amountX = ReadFloatBE(br);
                float amountY = ReadFloatBE(br);

                byte centerPivot = ReadByte(br);

                br.ReadBytes(3);

                float centerOffsetX = ReadFloatBE(br);
                float centerOffsetY = ReadFloatBE(br);

                return new UIMScaleCommand
                {
                    type = type,
                    startTime = startTime,
                    endTime = endTime,
                    accelTime = accelTime,
                    decelTime = decelTime,
                    enabled = enabled,
                    amountX = amountX,
                    amountY = amountY,
                    centerPivot = centerPivot,
                    centerOffsetX = centerOffsetX,
                    centerOffsetY = centerOffsetY
                };

            case UIMCommandType.Rotate:
                float rotation = ReadFloatBE(br);
                float rotCenterOffsetX = ReadFloatBE(br);
                float rotCenterOffsetY = ReadFloatBE(br);

                return new UIMRotateCommand
                {
                    type = type,
                    startTime = startTime,
                    endTime = endTime,
                    accelTime = accelTime,
                    decelTime = decelTime,
                    enabled = enabled,
                    rotation = rotation,
                    centerOffsetX = rotCenterOffsetX,
                    centerOffsetY = rotCenterOffsetY
                };

            case UIMCommandType.Opacity:
                byte startOpacity = ReadByte(br);
                byte endOpacity = ReadByte(br);

                br.ReadBytes(2);

                return new UIMOpacityCommand
                {
                    type = type,
                    startTime = startTime,
                    endTime = endTime,
                    accelTime = accelTime,
                    decelTime = decelTime,
                    enabled = enabled,
                    startOpacity = startOpacity,
                    endOpacity = endOpacity
                };

            case UIMCommandType.AbsoluteScale:
                float startX = ReadFloatBE(br);
                float startY = ReadFloatBE(br);
                float endX = ReadFloatBE(br);
                float endY = ReadFloatBE(br);

                byte absCenterPivot = ReadByte(br);
                byte textScale = ReadByte(br);

                br.ReadBytes(2);

                return new UIMAbsoluteScaleCommand
                {
                    type = type,
                    startTime = startTime,
                    endTime = endTime,
                    accelTime = accelTime,
                    decelTime = decelTime,
                    enabled = enabled,
                    startX = startX,
                    startY = startY,
                    endX = endX,
                    endY = endY,
                    centerPivot = absCenterPivot,
                    textScale = textScale
                };

            case UIMCommandType.Brightness:
                byte startBrightness = ReadByte(br);
                byte endBrightness = ReadByte(br);

                br.ReadBytes(2);

                return new UIMBrightnessCommand
                {
                    type = type,
                    startTime = startTime,
                    endTime = endTime,
                    accelTime = accelTime,
                    decelTime = decelTime,
                    enabled = enabled,
                    startBrightness = startBrightness,
                    endBrightness = endBrightness
                };

            case UIMCommandType.Color:
                byte startRed = ReadByte(br);
                byte startGreen = ReadByte(br);
                byte startBlue = ReadByte(br);

                byte endRed = ReadByte(br);
                byte endGreen = ReadByte(br);
                byte endBlue = ReadByte(br);

                br.ReadBytes(2);

                return new UIMColorCommand
                {
                    type = type,
                    startTime = startTime,
                    endTime = endTime,
                    accelTime = accelTime,
                    decelTime = decelTime,
                    enabled = enabled,
                    startRed = startRed,
                    startGreen = startGreen,
                    startBlue = startBlue,
                    endRed = endRed,
                    endGreen = endGreen,
                    endBlue = endBlue
                };

            case UIMCommandType.UVScroll:
                float amountU = ReadFloatBE(br);
                float amountV = ReadFloatBE(br);

                return new UIMUVScrollCommand
                {
                    type = type,
                    startTime = startTime,
                    endTime = endTime,
                    accelTime = accelTime,
                    decelTime = decelTime,
                    enabled = enabled,
                    amountU = amountU,
                    amountV = amountV
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
                bw.Write(new byte[3]);

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
                bw.Write(new byte[3]);

                WriteFloatBE(bw, scale.amountX);
                WriteFloatBE(bw, scale.amountY);

                WriteByte(bw, scale.centerPivot);
                bw.Write(new byte[3]);

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
                bw.Write(new byte[3]);

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
                bw.Write(new byte[3]);

                WriteByte(bw, opacity.startOpacity);
                WriteByte(bw, opacity.endOpacity);
                bw.Write(new byte[2]);
                break;

            case UIMAbsoluteScaleCommand absoluteScale:
                WriteUInt32BE(bw, (uint)absoluteScale.type);

                WriteFloatBE(bw, absoluteScale.startTime);
                WriteFloatBE(bw, absoluteScale.endTime);
                WriteFloatBE(bw, absoluteScale.accelTime);
                WriteFloatBE(bw, absoluteScale.decelTime);

                WriteByte(bw, absoluteScale.enabled);
                bw.Write(new byte[3]);

                WriteFloatBE(bw, absoluteScale.startX);
                WriteFloatBE(bw, absoluteScale.startY);
                WriteFloatBE(bw, absoluteScale.endX);
                WriteFloatBE(bw, absoluteScale.endY);

                WriteByte(bw, absoluteScale.centerPivot);
                WriteByte(bw, absoluteScale.textScale);
                bw.Write(new byte[2]);
                break;


            case UIMBrightnessCommand brightness:
                WriteUInt32BE(bw, (uint)brightness.type);

                WriteFloatBE(bw, brightness.startTime);
                WriteFloatBE(bw, brightness.endTime);
                WriteFloatBE(bw, brightness.accelTime);
                WriteFloatBE(bw, brightness.decelTime);

                WriteByte(bw, brightness.enabled);
                bw.Write(new byte[3]);

                WriteByte(bw, brightness.startBrightness);
                WriteByte(bw, brightness.endBrightness);
                bw.Write(new byte[2]);
                break;


            case UIMColorCommand color:
                WriteUInt32BE(bw, (uint)color.type);

                WriteFloatBE(bw, color.startTime);
                WriteFloatBE(bw, color.endTime);
                WriteFloatBE(bw, color.accelTime);
                WriteFloatBE(bw, color.decelTime);

                WriteByte(bw, color.enabled);
                bw.Write(new byte[3]);

                WriteByte(bw, color.startRed);
                WriteByte(bw, color.startGreen);
                WriteByte(bw, color.startBlue);

                WriteByte(bw, color.endRed);
                WriteByte(bw, color.endGreen);
                WriteByte(bw, color.endBlue);

                bw.Write(new byte[2]);
                break;


            case UIMUVScrollCommand uv:
                WriteUInt32BE(bw, (uint)uv.type);

                WriteFloatBE(bw, uv.startTime);
                WriteFloatBE(bw, uv.endTime);
                WriteFloatBE(bw, uv.accelTime);
                WriteFloatBE(bw, uv.decelTime);

                WriteByte(bw, uv.enabled);
                bw.Write(new byte[3]);

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
    public uint cmdSize { get; set; }
    public float totalTime { get; set; }
    public float loopTime { get; set; }
    public UIMCommand[] commands { get; set; }
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

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(UIMMoveCommand), "Move")] 
[JsonDerivedType(typeof(UIMScaleCommand), "Scale")]
[JsonDerivedType(typeof(UIMRotateCommand), "Rotate")]
[JsonDerivedType(typeof(UIMOpacityCommand), "Opacity")]
[JsonDerivedType(typeof(UIMAbsoluteScaleCommand), "AbsoluteScale")]
[JsonDerivedType(typeof(UIMBrightnessCommand), "Brightness")]
[JsonDerivedType(typeof(UIMColorCommand), "Color")]
[JsonDerivedType(typeof(UIMUVScrollCommand), "UVScroll")]
public abstract class UIMCommand
{
    [JsonIgnore]
    public UIMCommandType type { get; set; }
    public float startTime { get; set; }
    public float endTime { get; set; }
    public float accelTime { get; set; }
    public float decelTime { get; set; }
    public byte enabled { get; set; }
}

public class UIMMoveCommand : UIMCommand
{
    public UIMMoveCommand() => type = UIMCommandType.Move;

    public float distX { get; set; }
    public float distY { get; set; }
}

public class UIMScaleCommand : UIMCommand
{
    public UIMScaleCommand() => type = UIMCommandType.Scale;

    public float amountX { get; set; }
    public float amountY { get; set; }
    public byte centerPivot { get; set; }
    public float centerOffsetX { get; set; }
    public float centerOffsetY { get; set; }
}

public class UIMRotateCommand : UIMCommand
{
    public UIMRotateCommand() => type = UIMCommandType.Rotate;

    public float rotation { get; set; }
    public float centerOffsetX { get; set; }
    public float centerOffsetY { get; set; }
}

public class UIMOpacityCommand : UIMCommand
{
    public UIMOpacityCommand() => type = UIMCommandType.Opacity;

    public byte startOpacity { get; set; }
    public byte endOpacity { get; set; }
}

public class UIMAbsoluteScaleCommand : UIMCommand
{
    public UIMAbsoluteScaleCommand() => type = UIMCommandType.AbsoluteScale;

    public float startX { get; set; }
    public float startY { get; set; }
    public float endX { get; set; }
    public float endY { get; set; }
    public byte centerPivot { get; set; }
    public byte textScale { get; set; }
}

public class UIMBrightnessCommand : UIMCommand
{
    public UIMBrightnessCommand() => type = UIMCommandType.Brightness;

    public byte startBrightness { get; set; }
    public byte endBrightness { get; set; }
}

public class UIMColorCommand : UIMCommand
{
    public UIMColorCommand() => type = UIMCommandType.Color;

    public byte startRed { get; set; }
    public byte startGreen { get; set; }
    public byte startBlue { get; set; }
    public byte endRed { get; set; }
    public byte endGreen { get; set; }
    public byte endBlue { get; set; }
}

public class UIMUVScrollCommand : UIMCommand
{
    public UIMUVScrollCommand() => type = UIMCommandType.UVScroll;

    public float amountU { get; set; }
    public float amountV { get; set; }
}