using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class RANMParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        int version = ReadInt32BE(br);
        int numRows = ReadInt32BE(br);

        asset_entry[] entries = new asset_entry[numRows];

        for (int i = 0; i < numRows; i++)
        {
            entries[i] = new asset_entry
            {
                model_static = ReadUInt32BE(br),
                model_bound = ReadUInt32BE(br),
                lod_dist = ReadFloatBE(br),
                anim_idle = ReadUInt32BE(br),
                anim_move_through = ReadUInt32BE(br),
                anim_hit = ReadUInt32BE(br),
                sound_idle = ReadUInt32BE(br),
                sound_move_through = ReadUInt32BE(br),
                sound_hit = ReadUInt32BE(br),
                model_burnt = ReadUInt32BE(br),
                anim_burnt = ReadUInt32BE(br),
                burn_fuel = ReadFloatBE(br),
                burn_flame_size = ReadFloatBE(br),
                burn_emit_scale = ReadFloatBE(br),
                move_through_radius = ReadFloatBE(br),
            };
        }

        return new RANM
        {
            version = version,
            numRows = numRows,
            entries = entries
        };
    }

    public override object Serialize(object obj)
    {
        RANM ranm = (RANM)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteInt32BE(bw, ranm.version);
        WriteInt32BE(bw, ranm.numRows);

        foreach (var entry in ranm.entries)
        {
            WriteUInt32BE(bw, entry.model_static);
            WriteUInt32BE(bw, entry.model_bound);
            WriteFloatBE(bw, entry.lod_dist);
            WriteUInt32BE(bw, entry.anim_idle);
            WriteUInt32BE(bw, entry.anim_move_through);
            WriteUInt32BE(bw, entry.anim_hit);
            WriteUInt32BE(bw, entry.sound_idle);
            WriteUInt32BE(bw, entry.sound_move_through);
            WriteUInt32BE(bw, entry.sound_hit);
            WriteUInt32BE(bw, entry.model_burnt);
            WriteUInt32BE(bw, entry.anim_burnt);
            WriteFloatBE(bw, entry.burn_fuel);
            WriteFloatBE(bw, entry.burn_flame_size);
            WriteFloatBE(bw, entry.burn_emit_scale);
            WriteFloatBE(bw, entry.move_through_radius);
        }

        return ms.ToArray();
    }
}

public class RANM
{
    public int version { get; set; }
    public int numRows { get; set; }
    public asset_entry[] entries { get; set; }
}

public class asset_entry
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint model_static { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint model_bound { get; set; }
    public float lod_dist { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint anim_idle { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint anim_move_through { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint anim_hit { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint sound_idle { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint sound_move_through { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint sound_hit { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint model_burnt { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint anim_burnt { get; set; }
    public float burn_fuel { get; set; }
    public float burn_flame_size { get; set; }
    public float burn_emit_scale { get; set; }
    public float move_through_radius { get; set; }
}