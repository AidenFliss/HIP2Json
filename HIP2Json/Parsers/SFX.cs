using System.IO;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class SFXParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        return new SFX
        {
            flagsSFX = ReadInt16BE(br),
            freq = ReadInt16BE(br),
            freqm = ReadFloatBE(br),
            soundAssetID = ReadUInt32BE(br),
            attachID = ReadUInt32BE(br),
            loopCount = ReadByte(br),
            priority = ReadByte(br),
            volume = ReadByte(br),
            pad = ReadByte(br),
            pos = ReadVector3BE(br),
            innerRadius = ReadFloatBE(br),
            outerRadius = ReadFloatBE(br),
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
        WriteByte(bw, sfx.pad);
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
    public byte pad { get; set; }
    public xVec3 pos { get; set; }
    public float innerRadius { get; set; }
    public float outerRadius { get; set; }
}