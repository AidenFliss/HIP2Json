using System.IO;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class game_object_VentTypeParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version)
    {
        return new game_object_VentType
        {
            idle_particle_type = ReadUInt32BE(br),
            idle_sound_group = ReadUInt32BE(br),
            warn_particle_type = ReadUInt32BE(br),
            warn_sound_group = ReadUInt32BE(br),
            damage_particle_type = ReadUInt32BE(br),
            damage_sound_group = ReadUInt32BE(br),
        };
    }

    public override byte[] Serialize(object obj, short version)
    {
        game_object_VentType ventType = (game_object_VentType)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, ventType.idle_particle_type);
        WriteUInt32BE(bw, ventType.idle_sound_group);
        WriteUInt32BE(bw, ventType.warn_particle_type);
        WriteUInt32BE(bw, ventType.warn_sound_group);
        WriteUInt32BE(bw, ventType.damage_particle_type);
        WriteUInt32BE(bw, ventType.damage_sound_group);
        
        return ms.ToArray();
    }

    public override string GetFolderName() { return "VentType"; }
}

public class game_object_VentType
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint idle_particle_type { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint idle_sound_group { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint warn_particle_type { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint warn_sound_group { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint damage_particle_type { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint damage_sound_group { get; set; }
}