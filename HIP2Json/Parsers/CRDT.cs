using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class CRDTParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        br.BaseStream.Position = dataStart;

        uint magic = ReadUInt32BE(br);
        uint version = ReadUInt32BE(br);
        uint crdID = ReadUInt32BE(br);
        uint state = ReadUInt32BE(br);
        float total_time = ReadFloatBE(br);
        uint total_size = ReadUInt32BE(br);

        byte[] data = br.ReadBytes((int)(total_size - 0x18));

        if (state == 3)
            Util.DecryptCRDT(ref data);
        
        int dumpLen = System.Math.Min(0x100, data.Length);

        for (int i = 0; i < dumpLen; i += 16)
        {
            StringBuilder sb = new();

            sb.Append($"{i:X4}: ");

            for (int j = 0; j < 16 && (i + j) < dumpLen; j++)
                sb.Append($"{data[i + j]:X2} ");

            Logger.LogInfo(sb.ToString());
        }

        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        List<CRDTSection> sections = new();

        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            long sectionStart = reader.BaseStream.Position;

            uint credits_size = ReadUInt32BE(reader);
            float len = ReadFloatBE(reader);
            uint flags = ReadUInt32BE(reader);

            float in_x = ReadFloatBE(reader);
            float in_y = ReadFloatBE(reader);
            float out_x = ReadFloatBE(reader);
            float out_y = ReadFloatBE(reader);

            float scroll_rate = ReadFloatBE(reader);
            float lifetime = ReadFloatBE(reader);

            float fin_start = ReadFloatBE(reader);
            float fin_end = ReadFloatBE(reader);
            float fout_start = ReadFloatBE(reader);
            float fout_end = ReadFloatBE(reader);

            uint num_presets = ReadUInt32BE(reader);

            CRDTPreset[] presets = new CRDTPreset[num_presets];

            for (uint i = 0; i < num_presets; i++)
            {
                CRDTPreset preset = new()
                {
                    num = ReadUInt16BE(reader),
                    align = ReadUInt16BE(reader),
                    delay = ReadFloatBE(reader),
                    innerspace = ReadFloatBE(reader)
                };

                if (preset.align == 4)
                {
                    preset.textureFront = ReadTexture(reader);
                    preset.textureBack = ReadTexture(reader);
                }
                else
                {
                    preset.textStyle = ReadTextBox(reader);
                    preset.backdropStyle = ReadTextBox(reader);
                }

                presets[i] = preset;
            }

            List<CRDTHunk> hunks = new();

            long sectionEnd = sectionStart + credits_size;

            while (reader.BaseStream.Position < sectionEnd)
            {
                uint hunk_size = ReadUInt32BE(reader);
                uint preset = ReadUInt32BE(reader);
                float t0 = ReadFloatBE(reader);
                float t1 = ReadFloatBE(reader);
                uint text1Offset = ReadUInt32BE(reader);
                ReadUInt32BE(reader);

                string text1 = "";

                if (text1Offset != 0)
                {
                    List<byte> bytes = new();

                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        byte b = reader.ReadByte();

                        if (b == 0)
                            break;

                        bytes.Add(b);
                    }

                    while ((reader.BaseStream.Position & 3) != 0)
                        reader.BaseStream.Position++;

                    text1 = Encoding.ASCII.GetString(bytes.ToArray());
                }

                hunks.Add(new CRDTHunk
                {
                    hunk_size = hunk_size,
                    preset = preset,
                    t0 = t0,
                    t1 = t1,
                    text1 = text1
                });
            }

            sections.Add(new CRDTSection
            {
                credits_size = credits_size,
                len = len,
                flags = flags,

                in_x = in_x,
                in_y = in_y,
                out_x = out_x,
                out_y = out_y,

                scroll_rate = scroll_rate,
                lifetime = lifetime,

                fin_start = fin_start,
                fin_end = fin_end,
                fout_start = fout_start,
                fout_end = fout_end,

                presets = presets,
                hunks = hunks.ToArray()
            });
        }

        return new CRDT
        {
            magic = magic,
            version = version,
            crdID = crdID,
            state = state,
            total_time = total_time,
            total_size = total_size,
            sections = sections.ToArray()
        };
    }

    public override byte[] Serialize(object obj)
    {
        CRDT crdt = (CRDT)obj;

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, crdt.magic);
        WriteUInt32BE(bw, crdt.version);
        WriteUInt32BE(bw, crdt.crdID);
        WriteUInt32BE(bw, crdt.state);
        WriteFloatBE(bw, crdt.total_time);
        WriteUInt32BE(bw, crdt.total_size);

        foreach (var section in crdt.sections)
        {
            long sectionStart = bw.BaseStream.Position;

            WriteUInt32BE(bw, 0);

            WriteFloatBE(bw, section.len);
            WriteUInt32BE(bw, section.flags);

            WriteFloatBE(bw, section.in_x);
            WriteFloatBE(bw, section.in_y);
            WriteFloatBE(bw, section.out_x);
            WriteFloatBE(bw, section.out_y);

            WriteFloatBE(bw, section.scroll_rate);
            WriteFloatBE(bw, section.lifetime);

            WriteFloatBE(bw, section.fin_start);
            WriteFloatBE(bw, section.fin_end);
            WriteFloatBE(bw, section.fout_start);
            WriteFloatBE(bw, section.fout_end);

            WriteUInt32BE(bw, (uint)section.presets.Length);

            foreach (var preset in section.presets)
            {
                WriteUInt16BE(bw, preset.num);
                WriteUInt16BE(bw, preset.align);
                WriteFloatBE(bw, preset.delay);
                WriteFloatBE(bw, preset.innerspace);

                if (preset.align == 4)
                {
                    WriteTexture(bw, preset.textureFront);
                    WriteTexture(bw, preset.textureBack);
                }
                else
                {
                    WriteTextBox(bw, preset.textStyle);
                    WriteTextBox(bw, preset.backdropStyle);
                }
            }

            foreach (var hunk in section.hunks)
            {
                long hunkStart = bw.BaseStream.Position;

                WriteUInt32BE(bw, 0);
                WriteUInt32BE(bw, hunk.preset);
                WriteFloatBE(bw, hunk.t0);
                WriteFloatBE(bw, hunk.t1);

                if (!string.IsNullOrEmpty(hunk.text1))
                {
                    WriteUInt32BE(bw, (uint)(bw.BaseStream.Position + 8));
                    WriteUInt32BE(bw, 0);

                    bw.Write(Encoding.ASCII.GetBytes(hunk.text1));
                    bw.Write((byte)0);

                    while ((bw.BaseStream.Position & 3) != 0)
                        bw.Write((byte)0);
                }
                else
                {
                    WriteUInt32BE(bw, 0);
                    WriteUInt32BE(bw, 0);
                }

                long end = bw.BaseStream.Position;

                bw.BaseStream.Position = hunkStart;
                WriteUInt32BE(bw, (uint)(end - hunkStart));

                bw.BaseStream.Position = end;
            }

            long sectionEnd = bw.BaseStream.Position;

            bw.BaseStream.Position = sectionStart;
            WriteUInt32BE(bw, (uint)(sectionEnd - sectionStart));

            bw.BaseStream.Position = sectionEnd;
        }

        long endPos = bw.BaseStream.Position;

        bw.BaseStream.Position = 0x14;
        WriteUInt32BE(bw, (uint)endPos);

        bw.BaseStream.Position = endPos;

        byte[] result = ms.ToArray();

        if (crdt.state == 3)
            Util.DecryptCRDT(ref result);

        return result;
    }

    private static CRDTTextBox ReadTextBox(BinaryReader br)
    {
        return new CRDTTextBox
        {
            font = (TextFont)ReadUInt32BE(br),
            color = ReadColorBE(br),
            charWidth = ReadFloatBE(br),
            charHeight = ReadFloatBE(br),
            spacingX = ReadFloatBE(br),
            spacingY = ReadFloatBE(br),
            maxWidth = ReadFloatBE(br),
            maxHeight = ReadFloatBE(br)
        };
    }

    private static CRDTTexture ReadTexture(BinaryReader br)
    {
        return new CRDTTexture
        {
            textureAssetID = ReadUInt32BE(br),
            color = ReadColorBE(br),
            posX = ReadFloatBE(br),
            posY = ReadFloatBE(br),
            width = ReadFloatBE(br),
            height = ReadFloatBE(br),
            texture = ReadUInt32BE(br),
            pad = ReadUInt32BE(br)
        };
    }

    private static void WriteTextBox(BinaryWriter bw, CRDTTextBox tb)
    {
        WriteUInt32BE(bw, (uint)tb.font);
        WriteColorBE(bw, tb.color);
        WriteFloatBE(bw, tb.charWidth);
        WriteFloatBE(bw, tb.charHeight);
        WriteFloatBE(bw, tb.spacingX);
        WriteFloatBE(bw, tb.spacingY);
        WriteFloatBE(bw, tb.maxWidth);
        WriteFloatBE(bw, tb.maxHeight);
    }

    private static void WriteTexture(BinaryWriter bw, CRDTTexture tex)
    {
        WriteUInt32BE(bw, tex.textureAssetID);
        WriteColorBE(bw, tex.color);
        WriteFloatBE(bw, tex.posX);
        WriteFloatBE(bw, tex.posY);
        WriteFloatBE(bw, tex.width);
        WriteFloatBE(bw, tex.height);
        WriteUInt32BE(bw, tex.texture);
        WriteUInt32BE(bw, tex.pad);
    }
}

public class CRDT
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint magic { get; set; }
    public uint version { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint crdID { get; set; }
    public uint state { get; set; }
    public float total_time { get; set; }
    public uint total_size { get; set; }
    public CRDTSection[] sections { get; set; }
}

public class CRDTSection
{
    public uint credits_size { get; set; }
    public float len { get; set; }
    public uint flags { get; set; }
    public float in_x { get; set; }
    public float in_y { get; set; }
    public float out_x { get; set; }
    public float out_y { get; set; }
    public float scroll_rate { get; set; }
    public float lifetime { get; set; }
    public float fin_start { get; set; }
    public float fin_end { get; set; }
    public float fout_start { get; set; }
    public float fout_end { get; set; }
    public CRDTPreset[] presets { get; set; }
    public CRDTHunk[] hunks { get; set; }
}

public class CRDTPreset
{
    public ushort num { get; set; }
    public ushort align { get; set; }
    public float delay { get; set; }
    public float innerspace { get; set; }
    public CRDTTextBox textStyle { get; set; }
    public CRDTTextBox backdropStyle { get; set; }
    public CRDTTexture textureFront { get; set; }
    public CRDTTexture textureBack { get; set; }
}

public class CRDTHunk
{
    public uint hunk_size { get; set; }
    public uint preset { get; set; }
    public float t0 { get; set; }
    public float t1 { get; set; }
    public string text1 { get; set; }
}

public class CRDTTextBox
{
    public TextFont font { get; set; }
    public xColor color { get; set; }
    public float charWidth { get; set; }
    public float charHeight { get; set; }
    public float spacingX { get; set; }
    public float spacingY { get; set; }
    public float maxWidth { get; set; }
    public float maxHeight { get; set; }
}

public class CRDTTexture
{
    public uint textureAssetID { get; set; }
    public xColor color { get; set; }
    public float posX { get; set; }
    public float posY { get; set; }
    public float width { get; set; }
    public float height { get; set; }
    public uint texture { get; set; }
    public uint pad { get; set; }
}