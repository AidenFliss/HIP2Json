using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class SCRPParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        float scriptStartTime = ReadFloatBE(br);
        int eventCount = ReadInt32BE(br);

        byte loop;
        byte pad0, pad1, pad2;
        if (Program.CurrentGame == GameType.TSSM)
        {
            loop = ReadByte(br);
            pad0 = ReadByte(br);
            pad1 = ReadByte(br);
            pad2 = ReadByte(br);
        }

        br.BaseStream.Seek(0x14, SeekOrigin.Begin);

        xScriptEventAsset[] events = new xScriptEventAsset[eventCount];

        for (int i = 0; i < eventCount; i++)
        {
            xScriptEventAsset evnt = new xScriptEventAsset();
            evnt.time = ReadFloatBE(br);
            evnt.widget = ReadUInt32BE(br);

            if (Program.CurrentGame == GameType.BFBB)
                evnt.paramEventBFBB = (EventBFBB)(ushort)ReadUInt32BE(br);
            if (Program.CurrentGame == GameType.TSSM)
                evnt.paramEventTSSM = (EventTSSM)(ushort)ReadUInt32BE(br);
            evnt.param = Enumerable.Range(0, 4)
                    .Select(_ => ReadUInt32BE(br))
                    .ToArray();
            evnt.paramWidget = ReadUInt32BE(br);

            events[i] = evnt;
        }

        return new SCRP
        {
            scriptStartTime = scriptStartTime,
            eventCount = eventCount,
            events = events,
        };
    }

    public override object Serialize(object obj)
    {
        SCRP scrp = (SCRP)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteFloatBE(bw, scrp.scriptStartTime);
        WriteInt32BE(bw, scrp.eventCount);

        if (Program.CurrentGame == GameType.TSSM)
        {
            WriteByte(bw, scrp.loop);
            WriteByte(bw, scrp.pad0);
            WriteByte(bw, scrp.pad1);
            WriteByte(bw, scrp.pad2);
        }

        foreach (var evnt in scrp.events)
        {
            WriteFloatBE(bw, evnt.time);
            WriteUInt32BE(bw, evnt.widget);
            if (Program.CurrentGame == GameType.BFBB)
                WriteUInt32BE(bw, (ushort)evnt.paramEventBFBB);
            if (Program.CurrentGame == GameType.TSSM)
                WriteUInt32BE(bw, (ushort)evnt.paramEventTSSM);
            foreach (var param in evnt.param)
            {
                WriteUInt32BE(bw, param);
            }
            WriteUInt32BE(bw, evnt.paramWidget);
        }

        return ms.ToArray();
    }
}

public class SCRP
{
    public float scriptStartTime { get; set; }
    public int eventCount { get; set; }
    public byte loop { get; set; }
    public byte pad0 { get; set; }
    public byte pad1 { get; set; }
    public byte pad2 { get; set; }
    public xScriptEventAsset[] events { get; set; }
}

public class xScriptEventAsset
{
    public float time { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint widget { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EventBFBB paramEventBFBB { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EventTSSM paramEventTSSM { get; set; }
    [JsonConverter(typeof(AssetIDArrayConverter))]
    public uint[] param { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint paramWidget { get; set; }
}