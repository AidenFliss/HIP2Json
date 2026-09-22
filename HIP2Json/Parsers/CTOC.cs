using System;
using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class CTOCParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        uint headerCount = ReadUInt32BE(br);

        CTOC ctoc = new() { headerCount = headerCount };

        if (Program.CurrentGame == GameType.BFBB)
        {
            ctoc.cutsceneInfoEntriesBFBB = new xCutsceneInfoBFBB[headerCount];

            for (int i = 0; i < headerCount; i++)
            {
                xCutsceneInfoBFBB entry = new();

                entry.magic = ReadUInt32BE(br);
                entry.assetID = ReadUInt32BE(br);
                entry.numData = ReadUInt32BE(br);
                entry.numTime = ReadUInt32BE(br);
                entry.maxModel = ReadUInt32BE(br);
                entry.maxBufEven = ReadUInt32BE(br);
                entry.maxBufOdd = ReadUInt32BE(br);
                entry.headerSize = ReadUInt32BE(br);
                entry.visCount = ReadUInt32BE(br);
                entry.visSize = ReadUInt32BE(br);
                entry.breakCount = ReadUInt32BE(br);
                br.ReadBytes(4);
                entry.soundLeft = br.ReadBytes(16);
                entry.soundRight = br.ReadBytes(16);

                entry.edata = new xCutsceneData[entry.numData];
                for (int j = 0; j < entry.numData; j++)
                {
                    xCutsceneData ed = new();
                    ed.dataType = ReadUInt32BE(br);
                    ed.assetID = ReadUInt32BE(br);
                    ed.chunkSize = ReadUInt32BE(br);
                    ed.fileOffset = ReadUInt32BE(br);
                    entry.edata[j] = ed;
                }

                entry.timeChunkOffs = new uint[entry.numTime + 1];
                for (int j = 0; j < entry.timeChunkOffs.Length; j++)
                {
                    entry.timeChunkOffs[j] = ReadUInt32BE(br);
                }

                entry.visibility = new uint[entry.visSize];
                for (int j = 0; j < entry.visibility.Length; j++)
                {
                    entry.visibility[j] = ReadUInt32BE(br);
                }

                entry.breakList = new xCutsceneBreak[entry.breakCount];
                for (int j = 0; j < entry.breakList.Length; j++)
                {
                    xCutsceneBreak b = new();
                    b.time = ReadFloatBE(br);
                    b.index = ReadInt32BE(br);
                    entry.breakList[j] = b;
                }

                ctoc.cutsceneInfoEntriesBFBB[i] = entry;
            }
        }
        else
        {
            ctoc.cutsceneInfoEntriesTSSM = new xCutsceneInfoTSSM[headerCount];

            for (int i = 0; i < headerCount; i++)
            {
                xCutsceneInfoTSSM entry = new();

                entry.magic = ReadUInt32BE(br);
                entry.assetID = ReadUInt32BE(br);
                entry.numData = ReadUInt32BE(br);
                entry.numTime = ReadUInt32BE(br);
                entry.maxModel = ReadUInt32BE(br);
                entry.maxBufEven = ReadUInt32BE(br);
                entry.maxBufOdd = ReadUInt32BE(br);
                entry.headerSize = ReadUInt32BE(br);
                entry.visCount = ReadUInt32BE(br);
                entry.visSize = ReadUInt32BE(br);
                entry.breakCount = ReadUInt32BE(br);
                br.ReadBytes(4);
                entry.uLeftSoundID = ReadUInt32BE(br);
                entry.uRightSoundID = ReadUInt32BE(br);
                entry.szLeftSound = br.ReadBytes(28);
                entry.szRightSound = br.ReadBytes(28);

                ctoc.cutsceneInfoEntriesTSSM[i] = entry;
            }

            byte[] trailingBytes = br.ReadBytes((int)(br.BaseStream.Length - br.BaseStream.Position));
            ctoc.trailing = Convert.ToBase64String(trailingBytes);
        }

        return ctoc;
    }

    public override object Serialize(object obj)
    {
        CTOC ctoc = (CTOC)obj;

        using MemoryStream ms = new();
        using BinaryWriter bw = new(ms);

        WriteUInt32BE(bw, ctoc.headerCount);

        if (Program.CurrentGame == GameType.BFBB)
        {
            foreach (xCutsceneInfoBFBB entry in ctoc.cutsceneInfoEntriesBFBB)
            {
                WriteUInt32BE(bw, entry.magic);
                WriteUInt32BE(bw, entry.assetID);
                WriteUInt32BE(bw, entry.numData);
                WriteUInt32BE(bw, entry.numTime);
                WriteUInt32BE(bw, entry.maxModel);
                WriteUInt32BE(bw, entry.maxBufEven);
                WriteUInt32BE(bw, entry.maxBufOdd);
                WriteUInt32BE(bw, entry.headerSize);
                WriteUInt32BE(bw, entry.visCount);
                WriteUInt32BE(bw, entry.visSize);
                WriteUInt32BE(bw, entry.breakCount);
                bw.Write(new byte[4]);
                bw.Write(entry.soundLeft);
                bw.Write(entry.soundRight);

                foreach (xCutsceneData ed in entry.edata)
                {
                    WriteUInt32BE(bw, ed.dataType);
                    WriteUInt32BE(bw, ed.assetID);
                    WriteUInt32BE(bw, ed.chunkSize);
                    WriteUInt32BE(bw, ed.fileOffset);
                }

                foreach (uint t in entry.timeChunkOffs)
                {
                    WriteUInt32BE(bw, t);
                }

                foreach (uint v in entry.visibility)
                {
                    WriteUInt32BE(bw, v);
                }

                foreach (xCutsceneBreak b in entry.breakList)
                {
                    WriteFloatBE(bw, b.time);
                    WriteInt32BE(bw, b.index);
                }
            }
        }
        else
        {
            foreach (xCutsceneInfoTSSM entry in ctoc.cutsceneInfoEntriesTSSM)
            {
                WriteUInt32BE(bw, entry.magic);
                WriteUInt32BE(bw, entry.assetID);
                WriteUInt32BE(bw, entry.numData);
                WriteUInt32BE(bw, entry.numTime);
                WriteUInt32BE(bw, entry.maxModel);
                WriteUInt32BE(bw, entry.maxBufEven);
                WriteUInt32BE(bw, entry.maxBufOdd);
                WriteUInt32BE(bw, entry.headerSize);
                WriteUInt32BE(bw, entry.visCount);
                WriteUInt32BE(bw, entry.visSize);
                WriteUInt32BE(bw, entry.breakCount);
                bw.Write(new byte[4]);
                WriteUInt32BE(bw, entry.uLeftSoundID);
                WriteUInt32BE(bw, entry.uRightSoundID);
                bw.Write(entry.szLeftSound);
                bw.Write(entry.szRightSound);
            }

            if (!string.IsNullOrEmpty(ctoc.trailing))
            {
                bw.Write(Convert.FromBase64String(ctoc.trailing));
            }
        }

        return ms.ToArray();
    }
}

public class CTOC
{
    public uint headerCount { get; set; }
    public xCutsceneInfoBFBB[] cutsceneInfoEntriesBFBB;
    public xCutsceneInfoTSSM[] cutsceneInfoEntriesTSSM;
    public string trailing { get; set; }
}

public class xCutsceneInfoBFBB
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint magic { get; set; }

    [JsonConverter(typeof(AssetIDConverter))]
    public uint assetID { get; set; }
    public uint numData { get; set; }
    public uint numTime { get; set; }
    public uint maxModel { get; set; }
    public uint maxBufEven { get; set; }
    public uint maxBufOdd { get; set; }
    public uint headerSize { get; set; }
    public uint visCount { get; set; }
    public uint visSize { get; set; }
    public uint breakCount { get; set; }
    public byte[] soundLeft { get; set; }
    public byte[] soundRight { get; set; }
    public xCutsceneData[] edata { get; set; }
    public uint[] timeChunkOffs { get; set; }
    public uint[] visibility { get; set; }
    public xCutsceneBreak[] breakList { get; set; }
}

public class xCutsceneData
{
    public uint dataType { get; set; }

    [JsonConverter(typeof(AssetIDConverter))]
    public uint assetID { get; set; }
    public uint chunkSize { get; set; }
    public uint fileOffset { get; set; }
}

public class xCutsceneBreak
{
    public float time { get; set; }
    public int index { get; set; }
}

public class xCutsceneInfoTSSM
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint magic { get; set; }

    [JsonConverter(typeof(AssetIDConverter))]
    public uint assetID { get; set; }
    public uint numData { get; set; }
    public uint numTime { get; set; }
    public uint maxModel { get; set; }
    public uint maxBufEven { get; set; }
    public uint maxBufOdd { get; set; }
    public uint headerSize { get; set; }
    public uint visCount { get; set; }
    public uint visSize { get; set; }
    public uint breakCount { get; set; }
    public uint uLeftSoundID { get; set; }
    public uint uRightSoundID { get; set; }
    public byte[] szLeftSound { get; set; }
    public byte[] szRightSound { get; set; }
}