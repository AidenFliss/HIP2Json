using System;
using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class game_object_VentParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version, string dynaType)
    {
        return new game_object_Vent
        {
            ventType = ReadUInt32BE(br),
            position = ReadVector3BE(br),
            rotation = ReadVector3BE(br),
            damageCorner1 = ReadVector3BE(br),
            damageCorner2 = ReadVector3BE(br),
            boulderInfluence = ReadFloatBE(br),
            flags = (VentFlags)ReadUInt32BE(br),
            idle_time = ReadFloatBE(br),
            warn_time = ReadFloatBE(br),
            damage_time = ReadFloatBE(br),
        };
    }

    public override byte[] Serialize(object obj, short version, string dynaType)
    {
        game_object_Vent vent = (game_object_Vent)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, vent.ventType);
        WriteVector3BE(bw, vent.position);
        WriteVector3BE(bw, vent.rotation);
        WriteVector3BE(bw, vent.damageCorner1);
        WriteVector3BE(bw, vent.damageCorner2);
        WriteFloatBE(bw, vent.boulderInfluence);
        WriteUInt32BE(bw, (uint)vent.flags);
        WriteFloatBE(bw, vent.idle_time);
        WriteFloatBE(bw, vent.warn_time);
        WriteFloatBE(bw, vent.damage_time);
        
        return ms.ToArray();
    }

    public override string GetFolderName() { return "Vent"; }
}

public class game_object_Vent
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint ventType { get; set; }
    public xVec3 position { get; set; }
    public xVec3 rotation { get; set; }
    public xVec3 damageCorner1 { get; set; }
    public xVec3 damageCorner2 { get; set; }
    public float boulderInfluence { get; set; }
    public VentFlags flags { get; set; }
    public float idle_time { get; set; }
    public float warn_time { get; set; }
    public float damage_time { get; set; }
}

[Flags]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VentFlags : uint
{
    None = 0,
    BreakBoulders = 0x1,
    Automatic = 0x2,
    DamageSpongeBall = 0x4
}