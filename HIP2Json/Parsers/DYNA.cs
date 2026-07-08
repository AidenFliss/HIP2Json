using System.IO;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class DYNAParser : AssetParser //god save me..
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        uint type = ReadUInt32BE(br);
        short version = ReadInt16BE(br);
        short handle = ReadInt16BE(br);

        DYNA dyna = new DYNA
        {
            type = type,
            version = version,
            handle = handle
        };

        string typeHex = dyna.type.ToString("X8");
        string typeName = Dictionaries.DYNA_TO_NAME_MAPPING[typeHex];

        if (ParserMaps.DYNAToParser.TryGetValue(typeName, out AbstractDYNAParser parser))
        {
            object parsed = parser.ParseSafe(br, assetStart, dataStart + 8, version);

            dyna.typeNameInternal = typeName;
            dyna.dynaSpecificData = parsed;
        }

        return dyna;
    }

    public override object Serialize(object obj)
    {
        DYNA dyna = (DYNA)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, dyna.type);
        WriteInt16BE(bw, dyna.version);
        WriteInt16BE(bw, dyna.handle);

        string typeHex = dyna.type.ToString("X8");

        if (ParserMaps.DYNAToParser.TryGetValue(dyna.typeNameInternal, out AbstractDYNAParser parser))
        {
            byte[] serializedBytes = parser.Serialize(dyna.dynaSpecificData, dyna.version);
            bw.Write(serializedBytes);
        }

        return ms.ToArray();
    }
}

public class DYNA
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint type { get; set; }
    public short version { get; set; }
    public short handle { get; set; }
    public string typeNameInternal { get; set; } //dont edit this, plz...
    public object dynaSpecificData { get; set; }
}