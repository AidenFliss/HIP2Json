using System.Text.Json.Serialization;

namespace HIP2Json;

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
