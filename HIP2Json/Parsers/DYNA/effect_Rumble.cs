using System.IO;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class effect_RumbleParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version, string dynaType)
    {
        return new effect_Rumble
        {
            time = ReadFloatBE(br),
            intensity = ReadFloatBE(br),
            id = ReadUInt32BE(br),
            priority = ReadByte(br),
            type = ReadByte(br),
            rumbleInPause = ReadByte(br),
            pad = ReadByte(br),
            param1 = ReadFloatBE(br),
            param2 = ReadFloatBE(br),
            shakeMagnitude = ReadFloatBE(br),
            shakeCycleMax = ReadFloatBE(br),
            shakeRotationMagnitude = ReadFloatBE(br),
            shakeY = ReadByte(br),
            pad0 = ReadByte(br),
            pad1 = ReadByte(br),
            pad2 = ReadByte(br),
        };
    }

    public override byte[] Serialize(object obj, short version, string dynaType)
    {
        effect_Rumble rumble = (effect_Rumble)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteFloatBE(bw, rumble.time);
        WriteFloatBE(bw, rumble.intensity);
        WriteUInt32BE(bw, rumble.id);
        WriteByte(bw, rumble.priority);
        WriteByte(bw, rumble.type);
        WriteByte(bw, rumble.rumbleInPause);
        WriteByte(bw, rumble.pad);
        WriteFloatBE(bw, rumble.param1);
        WriteFloatBE(bw, rumble.param2);
        WriteFloatBE(bw, rumble.shakeMagnitude);
        WriteFloatBE(bw, rumble.shakeCycleMax);
        WriteFloatBE(bw, rumble.shakeRotationMagnitude);
        WriteByte(bw, rumble.shakeY);
        WriteByte(bw, rumble.pad0);
        WriteByte(bw, rumble.pad1);
        WriteByte(bw, rumble.pad2);

        return ms.ToArray();
    }

    public override string GetFolderName() { return "Rumble"; }
}

public class effect_Rumble
{
    public float time { get; set; }
    public float intensity { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint id { get; set; }
    public byte priority { get; set; }
    public byte type { get; set; }
    public byte rumbleInPause { get; set; }
    public byte pad { get; set; }
    public float param1 { get; set; }
    public float param2 { get; set; }
    public float shakeMagnitude { get; set; }
    public float shakeCycleMax { get; set; }
    public float shakeRotationMagnitude { get; set; }
    public byte shakeY { get; set; }
    public byte pad0 { get; set; }
    public byte pad1 { get; set; }
    public byte pad2 { get; set; }
}