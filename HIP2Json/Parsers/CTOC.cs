using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class CTOCParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        uint headerCount = ReadUInt32BE(br);

        CTOC ctoc = new()
        {
            headerCount = headerCount
        };

        if (Program.CurrentGame == GameType.BFBB)
            ctoc.cutsceneInfoEntriesBFBB = new xCutsceneInfoBFBB[headerCount];
        else
            ctoc.cutsceneInfoEntriesTSSM = new xCutsceneInfoTSSM[headerCount];

        for (int i = 0; i < headerCount; i++)
        {
            uint magic = ReadUInt32BE(br);
            uint assetID = ReadUInt32BE(br);
            uint numData = ReadUInt32BE(br);
            uint numTime = ReadUInt32BE(br);
            uint maxModel = ReadUInt32BE(br);
            uint maxBufEven = ReadUInt32BE(br);
            uint maxBufOdd = ReadUInt32BE(br);
            uint headerSize = ReadUInt32BE(br);
            uint visCount = ReadUInt32BE(br);
            uint visSize = ReadUInt32BE(br);
            uint breakCount = ReadUInt32BE(br);
            uint pad = ReadUInt32BE(br);

            if (Program.CurrentGame == GameType.BFBB)
            {
                ctoc.cutsceneInfoEntriesBFBB[i] = new xCutsceneInfoBFBB
                {
                    magic = magic,
                    assetID = assetID,
                    numData = numData,
                    numTime = numTime,
                    maxModel = maxModel,
                    maxBufEven = maxBufEven,
                    maxBufOdd = maxBufOdd,
                    headerSize = headerSize,
                    visCount = visCount,
                    visSize = visSize,
                    breakCount = breakCount,
                    pad = pad,
                    soundLeft = Enumerable.Range(0, 16)
                        .Select(_ => ReadByte(br))
                        .ToArray(),
                    soundRight = Enumerable.Range(0, 16)
                        .Select(_ => ReadByte(br))
                        .ToArray()
                };
            }
            else
            {
                ctoc.cutsceneInfoEntriesTSSM[i] = new xCutsceneInfoTSSM
                {
                    magic = magic,
                    assetID = assetID,
                    numData = numData,
                    numTime = numTime,
                    maxModel = maxModel,
                    maxBufEven = maxBufEven,
                    maxBufOdd = maxBufOdd,
                    headerSize = headerSize,
                    visCount = visCount,
                    visSize = visSize,
                    breakCount = breakCount,
                    pad = pad,
                    uLeftSoundID = ReadUInt32BE(br),
                    uRightSoundID = ReadUInt32BE(br),
                    szLeftSound = Enumerable.Range(0, 28)
                        .Select(_ => ReadByte(br))
                        .ToArray(),
                    szRightSound = Enumerable.Range(0, 28)
                        .Select(_ => ReadByte(br))
                        .ToArray()
                };
            }
        }

        return ctoc;
    }

    public override object Serialize(object obj)
    {
        CTOC ctoc = (CTOC)obj;

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, ctoc.headerCount);

        if (Program.CurrentGame == GameType.BFBB)
        {
            foreach (var entry in ctoc.cutsceneInfoEntriesBFBB)
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
                WriteUInt32BE(bw, entry.pad);

                foreach (char c in entry.soundLeft)
                    WriteByte(bw, (byte)c);

                foreach (char c in entry.soundRight)
                    WriteByte(bw, (byte)c);
            }
        }
        else
        {
            foreach (var entry in ctoc.cutsceneInfoEntriesTSSM)
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
                WriteUInt32BE(bw, entry.pad);
                WriteUInt32BE(bw, entry.uLeftSoundID);
                WriteUInt32BE(bw, entry.uRightSoundID);

                foreach (char c in entry.szLeftSound)
                    WriteByte(bw, (byte)c);

                foreach (char c in entry.szRightSound)
                    WriteByte(bw, (byte)c);
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
    public uint pad { get; set; }
    public byte[] soundLeft { get; set; }
    public byte[] soundRight { get; set; }
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
    public uint pad { get; set; }
    public uint uLeftSoundID { get; set; }
    public uint uRightSoundID { get; set; }
    public byte[] szLeftSound { get; set; }
    public byte[] szRightSound { get; set; }
}