using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

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
            long start = br.BaseStream.Position;

            uint type = ReadUInt32BE(br);
            byte wordLen = br.ReadByte();

            byte s0 = br.ReadByte();
            byte s1 = br.ReadByte();
            byte s2 = br.ReadByte();

            int byteLen = wordLen * 4;
            byte[] payload = br.ReadBytes(byteLen);

            byte[] full = new byte[3 + payload.Length];
            full[0] = s0;
            full[1] = s1;
            full[2] = s2;
            Array.Copy(payload, 0, full, 3, payload.Length);

            int end = full.Length;
            while (end > 0 && full[end - 1] == 0)
                end--;

            string value = Encoding.ASCII.GetString(full, 0, end);

            parameters = parameters.Append(new MinfParam
            {
                type = type,
                length = wordLen,
                value = value
            }).ToArray();

            long pos = br.BaseStream.Position;
            int pad = (int)((4 - (pos % 4)) % 4);
            br.BaseStream.Position += pad;
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
            WriteByte(bw, (byte)param.length);

            byte[] valueBytes = Encoding.ASCII.GetBytes(param.value);
            uint payloadLength = param.length * 4;
            byte[] payload = new byte[payloadLength];

            Array.Copy(valueBytes, 0, payload, 0, Math.Min(valueBytes.Length, payloadLength));

            bw.Write(payload);

            long pos = bw.BaseStream.Position;
            int pad = (int)((4 - (pos % 4)) % 4);
            for (int i = 0; i < pad; i++)
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
    public uint length { get; set; }
    public string value { get; set; }
    public ushort pad { get; set; }
}