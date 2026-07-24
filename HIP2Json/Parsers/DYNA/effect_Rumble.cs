using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class effect_RumbleParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version, string dynaType)
    {
        float time = ReadFloatBE(br);
        float intensity = ReadFloatBE(br);
        uint id = ReadUInt32BE(br);
        byte priority = ReadByte(br);
        byte type = ReadByte(br);
        byte rumbleInPause = ReadByte(br);
        br.ReadBytes(1);
        float param1 = ReadFloatBE(br);
        float param2 = ReadFloatBE(br);
        float shakeMagnitude = ReadFloatBE(br);
        float shakeCycleMax = ReadFloatBE(br);
        float shakeRotationMagnitude = ReadFloatBE(br);
        byte shakeY = ReadByte(br);
        br.ReadBytes(3);

        return new effect_Rumble
        {
            time = time,
            intensity = intensity,
            id = id,
            priority = priority,
            type = type,
            rumbleInPause = rumbleInPause,
            param1 = param1,
            param2 = param2,
            shakeMagnitude = shakeMagnitude,
            shakeCycleMax = shakeCycleMax,
            shakeRotationMagnitude = shakeRotationMagnitude,
            shakeY = shakeY
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
        bw.Write(new byte[1]);
        WriteFloatBE(bw, rumble.param1);
        WriteFloatBE(bw, rumble.param2);
        WriteFloatBE(bw, rumble.shakeMagnitude);
        WriteFloatBE(bw, rumble.shakeCycleMax);
        WriteFloatBE(bw, rumble.shakeRotationMagnitude);
        WriteByte(bw, rumble.shakeY);
        bw.Write(new byte[3]);

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
    public float param1 { get; set; }
    public float param2 { get; set; }
    public float shakeMagnitude { get; set; }
    public float shakeCycleMax { get; set; }
    public float shakeRotationMagnitude { get; set; }
    public byte shakeY { get; set; }
}