using System.IO;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class effect_Rumble_Spherical_EmitterParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version)
    {
        return new effect_Rumble_Spherical_Emitter
        {
            effectID = ReadUInt32BE(br),
            radius = ReadFloatBE(br),
            position = ReadVector3BE(br),
            onlyRumbleOnY = ReadByte(br),
            fallOff = ReadByte(br),
            onlyOnFloor = ReadByte(br),
            unknown = ReadByte(br),
        };
    }

    public override byte[] Serialize(object obj, short version)
    {
        effect_Rumble_Spherical_Emitter rumbleSphericalEmitter = (effect_Rumble_Spherical_Emitter)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, rumbleSphericalEmitter.effectID);
        WriteFloatBE(bw, rumbleSphericalEmitter.radius);
        WriteVector3BE(bw, rumbleSphericalEmitter.position);
        WriteByte(bw, rumbleSphericalEmitter.onlyRumbleOnY);
        WriteByte(bw, rumbleSphericalEmitter.fallOff);
        WriteByte(bw, rumbleSphericalEmitter.onlyOnFloor);
        WriteByte(bw, rumbleSphericalEmitter.unknown);

        return ms.ToArray();
    }

    public override string GetFolderName() { return "RumbleSphericalEmitter"; }
}

public class effect_Rumble_Spherical_Emitter
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint effectID { get; set; }
    public float radius { get; set; }
    public xVec3 position { get; set; }
    public byte onlyRumbleOnY { get; set; }
    public byte fallOff { get; set; }
    public byte onlyOnFloor { get; set; }
    public byte unknown { get; set; }
}