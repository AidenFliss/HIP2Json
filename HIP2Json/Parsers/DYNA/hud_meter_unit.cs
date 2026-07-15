using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class hud_meter_unitParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version, string dynaType)
    {
        return new hud_meter_unit
        {
            loc = ReadVector3BE(br),
            size = ReadVector3BE(br),
            start_value = ReadFloatBE(br),
            min_value = ReadFloatBE(br),
            max_value = ReadFloatBE(br),
            increment_time = ReadFloatBE(br),
            decrement_time = ReadFloatBE(br),
            sound_start_increment = ReadUInt32BE(br),
            sound_increment = ReadUInt32BE(br),
            sound_start_decrement = ReadUInt32BE(br),
            sound_decrement = ReadUInt32BE(br),
            model_0_id = ReadUInt32BE(br),
            model_0_loc = ReadVector3BE(br),
            model_0_size = ReadVector3BE(br),
            model_1_id = ReadUInt32BE(br),
            model_1_loc = ReadVector3BE(br),
            model_1_size = ReadVector3BE(br),
            fill_forward = (MeterFillDirection)ReadUInt32BE(br)
        };
    }

    public override byte[] Serialize(object obj, short version, string dynaType)
    {
        hud_meter_unit hudMeterUnit = (hud_meter_unit)obj;

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteVector3BE(bw, hudMeterUnit.loc);
        WriteVector3BE(bw, hudMeterUnit.size);
        WriteFloatBE(bw, hudMeterUnit.start_value);
        WriteFloatBE(bw, hudMeterUnit.min_value);
        WriteFloatBE(bw, hudMeterUnit.max_value);
        WriteFloatBE(bw, hudMeterUnit.increment_time);
        WriteFloatBE(bw, hudMeterUnit.decrement_time);
        WriteUInt32BE(bw, hudMeterUnit.sound_start_increment);
        WriteUInt32BE(bw, hudMeterUnit.sound_increment);
        WriteUInt32BE(bw, hudMeterUnit.sound_start_decrement);
        WriteUInt32BE(bw, hudMeterUnit.sound_decrement);
        WriteUInt32BE(bw, hudMeterUnit.model_0_id);
        WriteVector3BE(bw, hudMeterUnit.model_0_loc);
        WriteVector3BE(bw, hudMeterUnit.model_0_size);
        WriteUInt32BE(bw, hudMeterUnit.model_1_id);
        WriteVector3BE(bw, hudMeterUnit.model_1_loc);
        WriteVector3BE(bw, hudMeterUnit.model_1_size);
        WriteUInt32BE(bw, (uint)hudMeterUnit.fill_forward);

        return ms.ToArray();
    }

    public override string GetFolderName() { return "HUDMeterUnit"; }
}

public class hud_meter_unit
{
    public xVec3 loc { get; set; }
    public xVec3 size { get; set; }
    public float start_value { get; set; }
    public float min_value { get; set; }
    public float max_value { get; set; }
    public float increment_time { get; set; }
    public float decrement_time { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint sound_start_increment { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint sound_increment { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint sound_start_decrement { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint sound_decrement { get; set; }
    public uint model_0_id { get; set; }
    public xVec3 model_0_loc { get; set; }
    public xVec3 model_0_size { get; set; }
    public uint model_1_id { get; set; }
    public xVec3 model_1_loc { get; set; }
    public xVec3 model_1_size { get; set; }
    public MeterFillDirection fill_forward { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MeterFillDirection : uint
{
    RightToLeft = 0,
    LeftToRight = 1
}