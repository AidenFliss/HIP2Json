using System.IO;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class hud_meter_fontParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version)
    {
        return new hud_meter_font
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
            font_id = ReadUInt32BE(br),
            font_justify = ReadInt32BE(br),
            font_w = ReadInt32BE(br),
            font_h = ReadInt32BE(br),
            font_space = ReadInt32BE(br),
            font_drop_x = ReadFloatBE(br),
            font_drop_y = ReadFloatBE(br),
            font_c = ReadColor(br),
            font_drop_c = ReadColor(br),
            counter_mode = ReadByte(br),
            pad_0 = ReadByte(br),
            pad_1 = ReadByte(br),
            pad_2 = ReadByte(br),
        };
    }

    public override byte[] Serialize(object obj, short version)
    {
        hud_meter_font hudMeterFont = (hud_meter_font)obj;

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteVector3BE(bw, hudMeterFont.loc);
        WriteVector3BE(bw, hudMeterFont.size);
        WriteFloatBE(bw, hudMeterFont.start_value);
        WriteFloatBE(bw, hudMeterFont.min_value);
        WriteFloatBE(bw, hudMeterFont.max_value);
        WriteFloatBE(bw, hudMeterFont.increment_time);
        WriteFloatBE(bw, hudMeterFont.decrement_time);
        WriteUInt32BE(bw, hudMeterFont.sound_start_increment);
        WriteUInt32BE(bw, hudMeterFont.sound_increment);
        WriteUInt32BE(bw, hudMeterFont.sound_start_decrement);
        WriteUInt32BE(bw, hudMeterFont.sound_decrement);
        WriteUInt32BE(bw, hudMeterFont.font_id);
        WriteInt32BE(bw, hudMeterFont.font_justify);
        WriteInt32BE(bw, hudMeterFont.font_w);
        WriteInt32BE(bw, hudMeterFont.font_h);
        WriteInt32BE(bw, hudMeterFont.font_space);
        WriteFloatBE(bw, hudMeterFont.font_drop_x);
        WriteFloatBE(bw, hudMeterFont.font_drop_y);
        WriteColorBE(bw, hudMeterFont.font_c);
        WriteColorBE(bw, hudMeterFont.font_drop_c);
        WriteByte(bw, hudMeterFont.counter_mode);
        WriteByte(bw, hudMeterFont.pad_0);
        WriteByte(bw, hudMeterFont.pad_1);
        WriteByte(bw, hudMeterFont.pad_2);

        return ms.ToArray();
    }

    public override string GetFolderName() { return "HUDMeterFont"; }
}

public class hud_meter_font
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
    public uint font_id { get; set; }
    public int font_justify { get; set; }
    public int font_w { get; set; }
    public int font_h { get; set; }
    public int font_space { get; set; }
    public float font_drop_x { get; set; }
    public float font_drop_y { get; set; }
    public xColor font_c { get; set; }
    public xColor font_drop_c { get; set; }
    public byte counter_mode { get; set; }
    public byte pad_0 { get; set; }
    public byte pad_1 { get; set; }
    public byte pad_2 { get; set; }
}