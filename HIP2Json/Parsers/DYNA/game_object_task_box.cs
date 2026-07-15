using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class game_object_task_boxParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version, string dynaType)
    {
        return new game_object_task_box
        {
            persistent = ReadBoolean(br),
            loop = ReadBoolean(br),
            enable = ReadBoolean(br),
            talk_box = ReadUInt32BE(br),
            next_task = ReadUInt32BE(br),
            stages = Enumerable.Range(0, 6)
                .Select(_ => ReadUInt32BE(br))
                .ToArray(),
        };
    }

    public override byte[] Serialize(object obj, short version, string dynaType)
    {
        game_object_task_box taskBox = (game_object_task_box)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteBoolean(bw, taskBox.persistent);
        WriteBoolean(bw, taskBox.loop);
        WriteBoolean(bw, taskBox.enable);
        WriteBoolean(bw, taskBox.retry);
        WriteUInt32BE(bw, taskBox.talk_box);
        WriteUInt32BE(bw, taskBox.next_task);
        for (int i = 0; i < 6; i++)
            WriteUInt32BE(bw, taskBox.stages[i]);

        return ms.ToArray();
    }

    public override string GetFolderName() { return "TaskBox"; }
}

public class game_object_task_box
{
    public bool persistent { get; set; }
    public bool loop { get; set; }
    public bool enable { get; set; }
    public bool retry { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint talk_box { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint next_task { get; set; }
    [JsonConverter(typeof(AssetIDArrayConverter))]
    public uint[] stages { get; set; }
}