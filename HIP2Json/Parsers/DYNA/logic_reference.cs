using System.IO;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class logic_referenceParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version)
    {
        return new logic_reference
        {
            initial = ReadUInt32BE(br),
        };
    }

    public override byte[] Serialize(object obj, short version)
    {
        logic_reference logicReference = (logic_reference)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, logicReference.initial);
        
        return ms.ToArray();
    }

    public override string GetFolderName() { return "LogicReference"; }
}

public class logic_reference
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint initial { get; set; }
}