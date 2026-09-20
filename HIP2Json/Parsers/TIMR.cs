using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class TIMRParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        float seconds = ReadFloatBE(br);
        bool shortForm = br.BaseStream.Position + 4 > br.BaseStream.Length;
        float randomRange = shortForm ? 0f : ReadFloatBE(br);
        return new TIMR { seconds = seconds, randomRange = randomRange, ShortForm = shortForm };
    }

    public override object Serialize(object obj)
    {
        TIMR timr = (TIMR)obj;

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteFloatBE(bw, timr.seconds);
        WriteFloatBE(bw, timr.randomRange);

        return ms.ToArray();
    }
}

public class TIMR
{
    public float seconds { get; set; }
    public float randomRange { get; set; }

    [JsonIgnore]
    public bool ShortForm { get; set; }
}
