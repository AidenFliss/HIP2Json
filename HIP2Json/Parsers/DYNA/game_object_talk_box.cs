using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class game_object_talk_boxParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version, string dynaType)
    {
        return new game_object_talk_box
        {
            dialog_box = ReadUInt32BE(br),
            prompt_box = ReadUInt32BE(br),
            quit_box = ReadUInt32BE(br),
            trap = ReadByte(br),
            pause = ReadByte(br),
            allow_quit = ReadByte(br),
            trigger_pads = ReadByte(br),
            page = ReadByte(br),
            show = ReadByte(br),
            hide = ReadByte(br),
            audio_effect = ReadByte(br),
            teleport = ReadUInt32BE(br),
            auto_wait_type_time = ReadByte(br),
            auto_wait_type_prompt = ReadByte(br),
            auto_wait_type_sound = ReadByte(br),
            auto_wait_type_event = ReadByte(br),
            auto_wait_delay = ReadFloatBE(br),
            auto_wait_which_event = ReadInt32BE(br),
            prompt_skip = ReadUInt32BE(br),
            prompt_noskip = ReadUInt32BE(br),
            prompt_quit = ReadUInt32BE(br),
            prompt_noquit = ReadUInt32BE(br),
            prompt_yesno = ReadUInt32BE(br)
        };
    }

    public override byte[] Serialize(object obj, short version, string dynaType)
    {
        game_object_talk_box talkBox = (game_object_talk_box)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, talkBox.dialog_box);
        WriteUInt32BE(bw, talkBox.prompt_box);
        WriteUInt32BE(bw, talkBox.quit_box);
        WriteByte(bw, talkBox.trap);
        WriteByte(bw, talkBox.pause);
        WriteByte(bw, talkBox.allow_quit);
        WriteByte(bw, talkBox.trigger_pads);
        WriteByte(bw, talkBox.page);
        WriteByte(bw, talkBox.show);
        WriteByte(bw, talkBox.hide);
        WriteByte(bw, talkBox.audio_effect);
        WriteUInt32BE(bw, talkBox.teleport);
        WriteByte(bw, talkBox.auto_wait_type_time);
        WriteByte(bw, talkBox.auto_wait_type_prompt);
        WriteByte(bw, talkBox.auto_wait_type_sound);
        WriteByte(bw, talkBox.auto_wait_type_event);
        WriteFloatBE(bw, talkBox.auto_wait_delay);
        WriteInt32BE(bw, talkBox.auto_wait_which_event);
        WriteUInt32BE(bw, talkBox.prompt_skip);
        WriteUInt32BE(bw, talkBox.prompt_noskip);
        WriteUInt32BE(bw, talkBox.prompt_quit);
        WriteUInt32BE(bw, talkBox.prompt_noquit);
        WriteUInt32BE(bw, talkBox.prompt_yesno);

        return ms.ToArray();
    }

    public override string GetFolderName() { return "TalkBox"; }
}

public class game_object_talk_box
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint dialog_box { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint prompt_box { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint quit_box { get; set; }
    public byte trap { get; set; }
    public byte pause { get; set; }
    public byte allow_quit { get; set; }
    public byte trigger_pads { get; set; }
    public byte page { get; set; }
    public byte show { get; set; }
    public byte hide { get; set; }
    public byte audio_effect { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint teleport { get; set; }
    public byte auto_wait_type_time { get; set; }
    public byte auto_wait_type_prompt { get; set; }
    public byte auto_wait_type_sound { get; set; }
    public byte auto_wait_type_event { get; set; }
    public float auto_wait_delay { get; set; }
    public int auto_wait_which_event { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint prompt_skip { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint prompt_noskip { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint prompt_quit { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint prompt_noquit { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint prompt_yesno { get; set; }
}