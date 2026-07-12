using System.IO;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class hud_meter_fontParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version, string dynaType)
    {
        xVec3 loc = ReadVector3BE(br);
        xVec3 size = ReadVector3BE(br);
        float start_value = ReadFloatBE(br);
        float min_value = ReadFloatBE(br);
        float max_value = ReadFloatBE(br);
        float increment_time = ReadFloatBE(br);
        float decrement_time = ReadFloatBE(br);
        uint sound_start_increment = ReadUInt32BE(br);
        uint sound_increment = ReadUInt32BE(br);
        uint sound_start_decrement = ReadUInt32BE(br);
        uint sound_decrement = ReadUInt32BE(br);
        TextFont font_id = (TextFont)ReadUInt32BE(br);
        int font_justify = ReadInt32BE(br);
        float font_w = ReadFloatBE(br);
        float font_h = ReadFloatBE(br);
        float font_space = ReadFloatBE(br);
        float font_drop_x = ReadFloatBE(br);
        float font_drop_y = ReadFloatBE(br);
        xColor font_c = ReadColor(br);
        xColor font_drop_c = ReadColor(br);

        byte counter_mode = 0;
        byte pad_0 = 0;
        byte pad_1 = 0;
        byte pad_2 = 0;
        if (version == 3)
        {
            counter_mode = ReadByte(br);
            pad_0 = ReadByte(br);
            pad_1 = ReadByte(br);
            pad_2 = ReadByte(br);
        }

        return new hud_meter_font
        {
            loc = loc,
            size = size,
            start_value = start_value,
            min_value = min_value,
            max_value = max_value,
            increment_time = increment_time,
            decrement_time = decrement_time,
            sound_start_increment = sound_start_increment,
            sound_increment = sound_increment,
            sound_start_decrement = sound_start_decrement,
            sound_decrement = sound_decrement,
            font_id = font_id,
            font_justify = font_justify,
            font_w = font_w,
            font_h = font_h,
            font_space = font_space,
            font_drop_x = font_drop_x,
            font_drop_y = font_drop_y,
            font_c = font_c,
            font_drop_c = font_drop_c,
            counter_mode = counter_mode,
            pad_0 = pad_0,
            pad_1 = pad_1,
            pad_2 = pad_2,
        };
    }

    public override byte[] Serialize(object obj, short version, string dynaType)
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
        WriteUInt32BE(bw, (uint)hudMeterFont.font_id);
        WriteInt32BE(bw, hudMeterFont.font_justify);
        WriteFloatBE(bw, hudMeterFont.font_w);
        WriteFloatBE(bw, hudMeterFont.font_h);
        WriteFloatBE(bw, hudMeterFont.font_space);
        WriteFloatBE(bw, hudMeterFont.font_drop_x);
        WriteFloatBE(bw, hudMeterFont.font_drop_y);
        WriteColorBE(bw, hudMeterFont.font_c);
        WriteColorBE(bw, hudMeterFont.font_drop_c);

        if (version == 3)
        {
            WriteByte(bw, hudMeterFont.counter_mode);
            WriteByte(bw, hudMeterFont.pad_0);
            WriteByte(bw, hudMeterFont.pad_1);
            WriteByte(bw, hudMeterFont.pad_2);
        }

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
    public TextFont font_id { get; set; }
    public int font_justify { get; set; }
    public float font_w { get; set; }
    public float font_h { get; set; }
    public float font_space { get; set; }
    public float font_drop_x { get; set; }
    public float font_drop_y { get; set; }
    public xColor font_c { get; set; }
    public xColor font_drop_c { get; set; }
    public byte counter_mode { get; set; }
    public byte pad_0 { get; set; }
    public byte pad_1 { get; set; }
    public byte pad_2 { get; set; }
}