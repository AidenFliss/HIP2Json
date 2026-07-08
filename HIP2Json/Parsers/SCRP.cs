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

        br.BaseStream.Seek(0x14, SeekOrigin.Begin);

        xScriptEventAsset[] events = new xScriptEventAsset[eventCount];

        for (int i = 0; i < eventCount; i++)
        {
            xScriptEventAsset evnt = new xScriptEventAsset();
            evnt.time = ReadFloatBE(br);
            evnt.widget = ReadUInt32BE(br);
            evnt.paramEvent = (EventBFBB)(ushort)ReadInt16BE(br);
            evnt.param = Enumerable.Range(0, 3)
                    .Select(_ => ReadFloatBE(br))
                    .ToArray();
            evnt.paramWidget = ReadUInt32BE(br);
            evnt.enabled = ReadInt32BE(br);

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
        foreach (var evnt in scrp.events)
        {
            WriteFloatBE(bw, evnt.time);
            WriteUInt32BE(bw, evnt.widget);
            WriteInt16BE(bw, (short)evnt.paramEvent);
            foreach (var param in evnt.param)
            {
                WriteFloatBE(bw, param);
            }
            WriteUInt32BE(bw, evnt.paramWidget);
            WriteInt32BE(bw, evnt.enabled);
        }

        return ms.ToArray();
    }
}

public class SCRP
{
    public float scriptStartTime { get; set; }
    public int eventCount { get; set; }
    public xScriptEventAsset[] events { get; set; }
}

public class xScriptEventAsset
{
    public float time { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint widget { get; set; }
    public EventBFBB paramEvent { get; set; }
    public float[] param { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint paramWidget { get; set; }
    public int enabled { get; set; }
}