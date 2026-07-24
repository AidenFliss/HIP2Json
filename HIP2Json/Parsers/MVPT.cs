using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class MVPTParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        xVec3 pos = ReadVector3BE(br);
        ushort wt = ReadUInt16BE(br);
        byte on = ReadByte(br);
        byte bezIndex = ReadByte(br);
        byte flg_props = ReadByte(br);
        br.ReadBytes(1);
        ushort numPoints = ReadUInt16BE(br);
        float delay = ReadFloatBE(br);
        float zoneRadius = ReadFloatBE(br);
        float arenaRadius = ReadFloatBE(br);

        return new MVPT
        {
            pos = pos,
            wt = wt,
            on = (OnType)on,
            bezIndex = bezIndex,
            flg_props = flg_props,
            numPoints = numPoints,
            delay = delay,
            zoneRadius = zoneRadius,
            arenaRadius = arenaRadius,
            points = Enumerable.Range(0, numPoints)
                .Select(_ => ReadUInt32BE(br))
                .ToArray(),
        };
    }

    public override object Serialize(object obj)
    {
        MVPT mvpt = (MVPT)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteVector3BE(bw, mvpt.pos);
        WriteUInt16BE(bw, mvpt.wt);
        WriteByte(bw, (byte)mvpt.on);
        WriteByte(bw, mvpt.bezIndex);
        WriteByte(bw, mvpt.flg_props);
        bw.Write(new byte[1]);
        WriteUInt16BE(bw, mvpt.numPoints);
        WriteFloatBE(bw, mvpt.delay);
        WriteFloatBE(bw, mvpt.zoneRadius);
        WriteFloatBE(bw, mvpt.arenaRadius);
        foreach (var point in mvpt.points)
        {
            WriteUInt32BE(bw, point);
        }
        
        return ms.ToArray();
    }
}

public class MVPT
{
    public xVec3 pos { get; set; }
    public ushort wt { get; set; }
    public OnType on { get; set; }
    public byte bezIndex { get; set; }
    public byte flg_props { get; set; }
    public ushort numPoints { get; set; }
    public float delay { get; set; }
    public float zoneRadius { get; set; }
    public float arenaRadius { get; set; }
    [JsonConverter(typeof(AssetIDArrayConverter))]
    public uint[] points { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OnType : byte
{
    Arena = 0,
    Zone = 1
}