namespace HIP2Json;

public class AnimFile
{
    public uint fileFlags { get; set; }
    public float duration { get; set; }
    public float timeOffset { get; set; }
    public ushort[] numAnims { get; set; }
    public uint rawData { get; set; }
    public int physics { get; set; }
    public int startPose { get; set; }
    public int endPose { get; set; }
}

public class AnimState
{
    public uint stateID { get; set; }
    public uint fileIndex { get; set; }
    public uint effectCount { get; set; }
    public uint effectOffset { get; set; }
    public float speed { get; set; }
    public uint subStateID { get; set; }
    public uint subStateCount { get; set; }
}

public class AnimEffect
{
    public uint stateID { get; set; }
    public float startTime { get; set; }
    public float endTime { get; set; }
    public uint flags { get; set; }
    public uint effectType { get; set; }
    public uint userDataSize { get; set; }
}