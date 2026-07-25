using System.Text.Json.Serialization;

namespace HIP2Json;

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
