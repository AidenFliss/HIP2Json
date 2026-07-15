using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class game_object_text_boxParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version, string dynaType)
    {
        uint text = ReadUInt32BE(br);
        float pos_x = ReadFloatBE(br);
        float pox_y = ReadFloatBE(br);
        float bounds_w = ReadFloatBE(br);
        float bounds_h = ReadFloatBE(br);
        TextFont font = (TextFont)ReadInt32BE(br);
        float text_width = ReadFloatBE(br);
        float text_height = ReadFloatBE(br);
        float char_spacing_x = ReadFloatBE(br);
        float char_spacing_y = ReadFloatBE(br);
        xColor color = ReadColor(br);
        float inset_left = ReadFloatBE(br);
        float inset_top = ReadFloatBE(br);
        float inset_right = ReadFloatBE(br);
        float inset_bottom = ReadFloatBE(br);

        int xjustifyValue = ReadInt32BE(br);
        TextJustify xjustify = xjustifyValue switch
        {
            0 => TextJustify.Left,
            1 => TextJustify.Center,
            _ => TextJustify.Right
        };

        int expandValue = ReadInt32BE(br);
        TextExpandMode expand = expandValue switch
        {
            0 => TextExpandMode.Up,
            1 => TextExpandMode.Center,
            2 => TextExpandMode.Down,
            _ => TextExpandMode.Clip
        };

        float max_height = ReadFloatBE(br);
        BackdropType backdrop_type = (BackdropType)ReadUInt32BE(br);

        xColor backdrop_color;
        uint backdrop_texture;

        long remaining = br.BaseStream.Length - br.BaseStream.Position;

        if (remaining >= 16)
        {
            backdrop_color = ReadColor(br);
            backdrop_texture = ReadUInt32BE(br);
        }
        else
        {
            backdrop_color = new xColor();
            backdrop_texture = 0;
        }

        return new game_object_text_box
        {
            text = text,
            pos_x = pos_x,
            pox_y = pox_y,
            bounds_w = bounds_w,
            bounds_h = bounds_h,
            font = font,
            text_width = text_width,
            text_height = text_height,
            char_spacing_x = char_spacing_x,
            char_spacing_y = char_spacing_y,
            color = color,
            inset_left = inset_left,
            inset_top = inset_top,
            inset_right = inset_right,
            inset_bottom = inset_bottom,
            xjustify = xjustify,
            expand = expand,
            max_height = max_height,
            backdrop_type = backdrop_type,
            backdrop_color = backdrop_color,
            backdrop_texture = backdrop_texture
        };
    }

    public override byte[] Serialize(object obj, short version, string dynaType)
    {
        game_object_text_box textBox = (game_object_text_box)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, textBox.text);
        WriteFloatBE(bw, textBox.pos_x);
        WriteFloatBE(bw, textBox.pox_y);
        WriteFloatBE(bw, textBox.bounds_w);
        WriteFloatBE(bw, textBox.bounds_h);
        WriteInt32BE(bw, (int)textBox.font);
        WriteFloatBE(bw, textBox.text_width);
        WriteFloatBE(bw, textBox.text_height);
        WriteFloatBE(bw, textBox.char_spacing_x);
        WriteFloatBE(bw, textBox.char_spacing_y);
        WriteColorBE(bw, textBox.color);
        WriteFloatBE(bw, textBox.inset_left);
        WriteFloatBE(bw, textBox.inset_top);
        WriteFloatBE(bw, textBox.inset_right);
        WriteFloatBE(bw, textBox.inset_bottom);
        WriteInt32BE(bw, (int)textBox.xjustify);
        WriteInt32BE(bw, (int)textBox.expand);
        WriteFloatBE(bw, textBox.max_height);
        WriteUInt32BE(bw, (uint)textBox.backdrop_type);
        WriteColorBE(bw, textBox.backdrop_color);
        WriteUInt32BE(bw, textBox.backdrop_texture);

        return ms.ToArray();
    }

    public override string GetFolderName() { return "TextBox"; }
}

public class game_object_text_box
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint text { get; set; }
    public float pos_x { get; set; }
    public float pox_y { get; set; }
    public float bounds_w { get; set; }
    public float bounds_h { get; set; }
    public TextFont font { get; set; }
    public float text_width { get; set; }
    public float text_height { get; set; }
    public float char_spacing_x { get; set; }
    public float char_spacing_y { get; set; }
    public xColor color { get; set; }
    public float inset_left { get; set; }
    public float inset_top { get; set; }
    public float inset_right { get; set; }
    public float inset_bottom { get; set; }
    public TextJustify xjustify { get; set; }
    public TextExpandMode expand { get; set; }
    public float max_height { get; set; }
    public BackdropType backdrop_type { get; set; }
    public xColor backdrop_color { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint backdrop_texture { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TextFont : uint
{
    Default = 0,
    Arial = 1,
    System = 2,
    Numbers = 3
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TextJustify
{
    Left = 0,
    Center = 1,
    Right = 2
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TextExpandMode
{
    Up = 0,
    Center = 1,
    Down = 2,
    Clip = 3
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BackdropType : uint
{
    SolidColor = 0,
    Texture = 1,
    None = 100
}