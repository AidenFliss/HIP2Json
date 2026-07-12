using System.IO;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class effect_smoke_emitterParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version, string dynaType)
    {
        return new effect_smoke_emitter
        {
            flags = ReadUInt32BE(br),
            attach_to = ReadUInt32BE(br),
            loc = ReadVector3BE(br),
            dir = ReadVector3BE(br),
            scale = ReadVector3BE(br),
            texture = ReadUInt32BE(br),
            texture_rows = ReadUInt16BE(br),
            texture_columns = ReadUInt16BE(br),
            rate = ReadFloatBE(br),
            life_min = ReadFloatBE(br),
            life_max = ReadFloatBE(br),
            size_min = ReadFloatBE(br),
            size_max = ReadFloatBE(br),
            vel_min = ReadFloatBE(br),
            vel_max = ReadFloatBE(br),
            growth = ReadFloatBE(br),
            vel_dir = ReadVector3BE(br),
            vel_dir_vary = ReadFloatBE(br),
            wind = ReadFloatBE(br),
            color_birth = ReadColor(br),
            color_death = ReadColor(br),
        };
    }

    public override byte[] Serialize(object obj, short version, string dynaType)
    {
        effect_smoke_emitter smokeEmitter = (effect_smoke_emitter)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, smokeEmitter.flags);
        WriteUInt32BE(bw, smokeEmitter.attach_to);
        WriteVector3BE(bw, smokeEmitter.loc);
        WriteVector3BE(bw, smokeEmitter.dir);
        WriteVector3BE(bw, smokeEmitter.scale);
        WriteUInt32BE(bw, smokeEmitter.texture);
        WriteUInt16BE(bw, smokeEmitter.texture_rows);
        WriteUInt16BE(bw, smokeEmitter.texture_columns);
        WriteFloatBE(bw, smokeEmitter.rate);
        WriteFloatBE(bw, smokeEmitter.life_min);
        WriteFloatBE(bw, smokeEmitter.life_max);
        WriteFloatBE(bw, smokeEmitter.size_min);
        WriteFloatBE(bw, smokeEmitter.size_max);
        WriteFloatBE(bw, smokeEmitter.vel_min);
        WriteFloatBE(bw, smokeEmitter.vel_max);
        WriteFloatBE(bw, smokeEmitter.growth);
        WriteVector3BE(bw, smokeEmitter.vel_dir);
        WriteFloatBE(bw, smokeEmitter.vel_dir_vary);
        WriteFloatBE(bw, smokeEmitter.wind);
        WriteColorBE(bw, smokeEmitter.color_birth);
        WriteColorBE(bw, smokeEmitter.color_death);

        return ms.ToArray();
    }

    public override string GetFolderName() { return "SmokeEmitter"; }
}

public class effect_smoke_emitter
{
    public uint flags { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint attach_to { get; set; }
    public xVec3 loc { get; set; }
    public xVec3 dir { get; set; }
    public xVec3 scale { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint texture { get; set; }
    public ushort texture_rows { get; set; }
    public ushort texture_columns { get; set; }
    public float rate { get; set; }
    public float life_min { get; set; }
    public float life_max { get; set; }
    public float size_min { get; set; }
    public float size_max { get; set; }
    public float vel_min { get; set; }
    public float vel_max { get; set; }
    public float growth { get; set; }
    public xVec3 vel_dir { get; set; }
    public float vel_dir_vary { get; set; }
    public float wind { get; set; }
    public xColor color_birth { get; set; }
    public xColor color_death { get; set; }
}