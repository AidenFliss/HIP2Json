using System.IO;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class game_object_text_boxParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version)
    {
        var obj = new game_object_text_box
        {
            text = ReadUInt32BE(br),
            pos_x = ReadFloatBE(br),
            pox_y = ReadFloatBE(br),
            bounds_w = ReadFloatBE(br),
            bounds_h = ReadFloatBE(br),
            font = ReadInt32BE(br),
            text_width = ReadFloatBE(br),
            text_height = ReadFloatBE(br),
            char_spacing_x = ReadFloatBE(br),
            char_spacing_y = ReadFloatBE(br),
            color = ReadColor(br),
            inset_left = ReadFloatBE(br),
            inset_top = ReadFloatBE(br),
            inset_right = ReadFloatBE(br),
            inset_bottom = ReadFloatBE(br),
            xjustify = ReadInt32BE(br),
            expand = ReadInt32BE(br),
            max_height = ReadFloatBE(br),
            backdrop_type = ReadUInt32BE(br),
        };

        long remaining = br.BaseStream.Length - br.BaseStream.Position;

        if (remaining >= 16)
        {
            obj.backdrop_color = ReadColor(br);
            obj.backdrop_texture = ReadUInt32BE(br);
        }
        else
        {
            obj.backdrop_color = new xColor();
            obj.backdrop_texture = 0;
            return obj;
        }

        return obj;
    }

    public override byte[] Serialize(object obj, short version)
    {
        game_object_text_box textBox = (game_object_text_box)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, textBox.text);
        WriteFloatBE(bw, textBox.pos_x);
        WriteFloatBE(bw, textBox.pox_y);
        WriteFloatBE(bw, textBox.bounds_w);
        WriteFloatBE(bw, textBox.bounds_h);
        WriteInt32BE(bw, textBox.font);
        WriteFloatBE(bw, textBox.text_width);
        WriteFloatBE(bw, textBox.text_height);
        WriteFloatBE(bw, textBox.char_spacing_x);
        WriteFloatBE(bw, textBox.char_spacing_y);
        WriteColorBE(bw, textBox.color);
        WriteFloatBE(bw, textBox.inset_left);
        WriteFloatBE(bw, textBox.inset_top);
        WriteFloatBE(bw, textBox.inset_right);
        WriteFloatBE(bw, textBox.inset_bottom);
        WriteInt32BE(bw, textBox.xjustify);
        WriteInt32BE(bw, textBox.expand);
        WriteFloatBE(bw, textBox.max_height);
        WriteUInt32BE(bw, textBox.backdrop_type);
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
    public int font { get; set; }
    public float text_width { get; set; }
    public float text_height { get; set; }
    public float char_spacing_x { get; set; }
    public float char_spacing_y { get; set; }
    public xColor color { get; set; }
    public float inset_left { get; set; }
    public float inset_top { get; set; }
    public float inset_right { get; set; }
    public float inset_bottom { get; set; }
    public int xjustify { get; set; }
    public int expand { get; set; }
    public float max_height { get; set; }
    public uint backdrop_type { get; set; }
    public xColor backdrop_color { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint backdrop_texture { get; set; }
}