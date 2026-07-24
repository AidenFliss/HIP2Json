using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class SFXParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        short flagsSFX = ReadInt16BE(br);
        short freq = ReadInt16BE(br);
        float freqm = ReadFloatBE(br);
        uint soundAssetID = ReadUInt32BE(br);
        uint attachID = ReadUInt32BE(br);
        byte loopCount = ReadByte(br);
        byte priority = ReadByte(br);
        byte volume = ReadByte(br);
        br.ReadBytes(1);
        xVec3 pos = ReadVector3BE(br);
        float innerRadius = ReadFloatBE(br);
        float outerRadius = ReadFloatBE(br);

        return new SFX
        {
            flagsSFX = flagsSFX,
            freq = freq,
            freqm = freqm,
            soundAssetID = soundAssetID,
            attachID = attachID,
            loopCount = loopCount,
            priority = priority,
            volume = volume,
            pos = pos,
            innerRadius = innerRadius,
            outerRadius = outerRadius
        };
    }

    public override object Serialize(object obj)
    {
        SFX sfx = (SFX)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteInt16BE(bw, sfx.flagsSFX);
        WriteInt16BE(bw, sfx.freq);
        WriteFloatBE(bw, sfx.freqm);
        WriteUInt32BE(bw, sfx.soundAssetID);
        WriteUInt32BE(bw, sfx.attachID);
        WriteByte(bw, sfx.loopCount);
        WriteByte(bw, sfx.priority);
        WriteByte(bw, sfx.volume);
        bw.Write(new byte[1]);
        WriteVector3BE(bw, sfx.pos);
        WriteFloatBE(bw, sfx.innerRadius);
        WriteFloatBE(bw, sfx.outerRadius);

        return ms.ToArray();
    }
}

public class SFX
{
    public short flagsSFX { get; set; }
    public short freq { get; set; }
    public float freqm { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint soundAssetID { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint attachID { get; set; }
    public byte loopCount { get; set; }
    public byte priority { get; set; }
    public byte volume { get; set; }
    public xVec3 pos { get; set; }
    public float innerRadius { get; set; }
    public float outerRadius { get; set; }
}