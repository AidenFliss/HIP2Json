using System.Text.Json.Serialization;

namespace HIP2Json;

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
    public float length { get; set; }
    public float range { get; set; }
    public float period { get; set; }
    public float phase { get; set; }
}
