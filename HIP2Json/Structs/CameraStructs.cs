using System.Text.Json.Serialization;

namespace HIP2Json;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(FollowCam), "Follow")]
[JsonDerivedType(typeof(ShoulderCam), "Shoulder")]
[JsonDerivedType(typeof(StaticCam), "Static")]
[JsonDerivedType(typeof(PathCam), "Path")]
[JsonDerivedType(typeof(StaticFollowCam), "StaticFollow")]
public abstract class CamSpecificData
{
    [JsonIgnore]
    public CamType Type { get; protected set; }
}

public class FollowCam : CamSpecificData
{
    public FollowCam() => Type = CamType.Follow;

    public float rotation { get; set; }
    public float distance { get; set; }
    public float height { get; set; }
    public float rubberBand { get; set; }
    public float startSpeed { get; set; }
    public float endSpeed { get; set; }
}

public class ShoulderCam : CamSpecificData
{
    public ShoulderCam() => Type = CamType.Shoulder;

    public float distance { get; set; }
    public float height { get; set; }
    public float realignSpeed { get; set; }
    public float realignDelay { get; set; }
}

public class StaticCam : CamSpecificData
{
    public StaticCam() => Type = CamType.Static;

    public uint unused { get; set; }
}

public class PathCam : CamSpecificData
{
    public PathCam() => Type = CamType.Path;

    [JsonConverter(typeof(AssetIDConverter))]
    public uint assetID { get; set; }

    public float timeEnd { get; set; }
    public float timeDelay { get; set; }
}

public class StaticFollowCam : CamSpecificData
{
    public StaticFollowCam() => Type = CamType.StaticFollow;

    public float rubberBand { get; set; }
}
