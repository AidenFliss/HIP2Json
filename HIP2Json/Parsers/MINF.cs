using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class MINFParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        uint magic = ReadUInt32BE(br);
        uint numModelInst = ReadUInt32BE(br);
        uint animTableID = ReadUInt32BE(br);
        uint combatID = ReadUInt32BE(br);
        uint brainID = ReadUInt32BE(br);

        ModelInst[] modelInstances = new ModelInst[numModelInst];

        for (int i = 0; i < numModelInst; i++)
        {
            uint modelID = ReadUInt32BE(br);
            ushort flags = ReadUInt16BE(br);
            byte parent = br.ReadByte();
            byte bone = br.ReadByte();
            xVec3 right = ReadVector3BE(br);
            xVec3 up = ReadVector3BE(br);
            xVec3 at = ReadVector3BE(br);
            xVec3 pos = ReadVector3BE(br);

            modelInstances[i] = new ModelInst
            {
                modelID = modelID,
                flags = flags,
                parent = parent,
                bone = bone,
                right = right,
                up = up,
                at = at,
                pos = pos
            };
        }

        MinfParam[] parameters = new MinfParam[0];

        while (br.BaseStream.Position < br.BaseStream.Length)
        {
            uint type = ReadUInt32BE(br);
            byte length = br.ReadByte();

            int dataLength = ((length + 1) * 4) - 1;

            byte[] data = br.ReadBytes(dataLength);

            int end = Array.IndexOf(data, (byte)0);
            if (end < 0)
                end = data.Length;

            string value = Encoding.ASCII.GetString(data, 0, end);

            parameters = parameters.Append(new MinfParam
            {
                type = type,
                length = dataLength,
                value = value
            }).ToArray();
        }

        return new MINF
        {
            magic = magic,
            numModelInst = numModelInst,
            animTableID = animTableID,
            combatID = combatID,
            brainID = brainID,
            modelInstances = modelInstances,
            parameters = parameters
        };
    }

    public override object Serialize(object obj)
    {
        MINF minf = (MINF)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, minf.magic);
        WriteUInt32BE(bw, minf.numModelInst);
        WriteUInt32BE(bw, minf.animTableID);
        WriteUInt32BE(bw, minf.combatID);
        WriteUInt32BE(bw, minf.brainID);
        foreach (var modelInst in minf.modelInstances)
        {
            WriteUInt32BE(bw, modelInst.modelID);
            WriteUInt16BE(bw, modelInst.flags);
            WriteByte(bw, modelInst.parent);
            WriteByte(bw, modelInst.bone);
            WriteVector3BE(bw, modelInst.right);
            WriteVector3BE(bw, modelInst.up);
            WriteVector3BE(bw, modelInst.at);
            WriteVector3BE(bw, modelInst.pos);
        }
        foreach (var param in minf.parameters)
        {
            WriteUInt32BE(bw, param.type);

            byte[] valueBytes = Encoding.ASCII.GetBytes(param.value);

            int totalLength = valueBytes.Length + 1;
            int paddedLength = (totalLength + 3) & ~3;

            byte length = (byte)((paddedLength / 4) - 1);
            WriteByte(bw, length);

            bw.Write(valueBytes);

            for (int i = valueBytes.Length; i < paddedLength - 1; i++)
                WriteByte(bw, 0);

            WriteByte(bw, 0);
        }
        
        return ms.ToArray();
    }
}

public class MINF
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint magic { get; set; }
    public uint numModelInst { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint animTableID { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint combatID { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint brainID { get; set; }
    public ModelInst[] modelInstances { get; set; }
    public MinfParam[] parameters { get; set; }
}

public class ModelInst
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint modelID { get; set; }
    public ushort flags { get; set; }
    public byte parent { get; set; }
    public byte bone { get; set; }
    public xVec3 right { get; set; }
    public xVec3 up { get; set; }
    public xVec3 at { get; set; }
    public xVec3 pos { get; set; }
}

public class MinfParam
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint type { get; set; }
    public int length { get; set; }
    public string value { get; set; }
    public ushort pad { get; set; }
}