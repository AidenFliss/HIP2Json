using System.IO;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class PARSParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        return new PARS
        {
            type = ReadInt32BE(br),
            parentParSysID = ReadUInt32BE(br),
            textureID = ReadUInt32BE(br),
            parFlags = ReadByte(br),
            priority = ReadByte(br),
            maxPar = ReadInt16BE(br),
            renderFunc = ReadByte(br),
            renderSrcBlendMode = ReadByte(br),
            renderDstBlendMode = ReadByte(br),
            cmdCount = ReadByte(br),
            cmdSize = ReadInt32BE(br)
        };
    }

    public override object Serialize(object obj)
    {
        PARS pars = (PARS)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteInt32BE(bw, pars.type);
        WriteUInt32BE(bw, pars.parentParSysID);
        WriteUInt32BE(bw, pars.textureID);
        WriteByte(bw, pars.parFlags);
        WriteByte(bw, pars.priority);
        WriteInt16BE(bw, pars.maxPar);
        WriteByte(bw, pars.renderFunc);
        WriteByte(bw, pars.renderSrcBlendMode);
        WriteByte(bw, pars.renderDstBlendMode);
        WriteByte(bw, pars.cmdCount);
        WriteInt32BE(bw, pars.cmdSize);

        return ms.ToArray();
    }
}

public class PARS
{
    public int type { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint parentParSysID { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint textureID { get; set; }
    public byte parFlags { get; set; }
    public byte priority { get; set; }
    public short maxPar { get; set; }
    public byte renderFunc { get; set; }
    public byte renderSrcBlendMode { get; set; }
    public byte renderDstBlendMode { get; set; }
    public byte cmdCount { get; set; }
    public int cmdSize { get; set; }
}