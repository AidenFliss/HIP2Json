using System.Text.Json.Serialization;

namespace HIP2Json;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CircleEmitter), "Circle")]
[JsonDerivedType(typeof(SphereEmitter), "Sphere")]
[JsonDerivedType(typeof(RectEmitter), "Rect")]
[JsonDerivedType(typeof(LineEmitter), "Line")]
[JsonDerivedType(typeof(VolumeEmitter), "Volume")]
[JsonDerivedType(typeof(OffsetPointEmitter), "OffsetPoint")]
[JsonDerivedType(typeof(VCylEmitter), "VCylEdge")]
[JsonDerivedType(typeof(EntityBoneEmitter), "EntityBone")]
[JsonDerivedType(typeof(EntityBoundEmitter), "EntityBound")]
public abstract class EmitterData
{
    [JsonIgnore]
    public EmitType Type { get; protected set; }
}

public class CircleEmitter : EmitterData
{
    public CircleEmitter() => Type = EmitType.Circle;

    public float radius { get; set; }
    public float deflection { get; set; }
    public xVec3 dir { get; set; }
}

public class SphereEmitter : EmitterData
{
    public SphereEmitter() => Type = EmitType.Sphere;

    public float radius { get; set; }
}

public class RectEmitter : EmitterData
{
    public RectEmitter() => Type = EmitType.Rect;

    public float xLen { get; set; }
    public float zLen { get; set; }
}

public class LineEmitter : EmitterData
{
    public LineEmitter() => Type = EmitType.Line;

    public xVec3 pos1 { get; set; }
    public xVec3 pos2 { get; set; }
    public float radius { get; set; }
}

public class VolumeEmitter : EmitterData
{
    public VolumeEmitter() => Type = EmitType.Volume;

    [JsonConverter(typeof(AssetIDConverter))]
    public uint volumeID { get; set; }
}

public class OffsetPointEmitter : EmitterData
{
    public OffsetPointEmitter() => Type = EmitType.OffsetPoint;

    public xVec3 offset { get; set; }
}

public class VCylEmitter : EmitterData
{
    public VCylEmitter() => Type = EmitType.VCylEdge;

    public float height { get; set; }
    public float radius { get; set; }
    public float deflection { get; set; }
}

public class EntityBoneEmitter : EmitterData
{
    public EntityBoneEmitter() => Type = EmitType.EntityBone;

    public byte flags { get; set; }
    public byte entityBoneType { get; set; }
    public byte bone { get; set; }
    public xVec3 offset { get; set; }
    public float radius { get; set; }
    public float deflection { get; set; }
}

public class EntityBoundEmitter : EmitterData
{
    public EntityBoundEmitter() => Type = EmitType.EntityBound;

    public byte flags { get; set; }
    public byte entityBoundType { get; set; }
    public float expand { get; set; }
    public float deflection { get; set; }
}
