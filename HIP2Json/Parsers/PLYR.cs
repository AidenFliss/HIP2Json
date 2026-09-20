using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class PLYRParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        br.BaseStream.Seek(br.BaseStream.Length - 4, SeekOrigin.Begin);

        return new PLYR { lightKitID = ReadUInt32BE(br) };
    }

    public override long GetLinksOffset(BinaryReader br, byte linkCount)
    {
        return br.BaseStream.Length - (linkCount * 32L) - 4;
    }

    public override object Serialize(object obj)
    {
        PLYR plyr = (PLYR)obj;

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, plyr.lightKitID);

        return ms.ToArray();
    }
}

public class PLYR
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint lightKitID { get; set; }
}
