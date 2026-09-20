using System;
using System.IO;

namespace HIP2Json;

public sealed class SNDIParser : AssetParser
{
    private const int EntrySize = 100;
    private const int EntryTailOffset = 0x10;
    private const int HeaderSize = 0x10;

    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        byte[] data = br.ReadBytes((int)(br.BaseStream.Length - br.BaseStream.Position));

        if (data.Length < HeaderSize)
            return new SNDI
            {
                header = Convert.ToBase64String(data),
                entries = Array.Empty<SNDIEntry>(),
            };

        byte[] header = new byte[HeaderSize];
        Array.Copy(data, 0, header, 0, HeaderSize);

        SNDI sndi = new SNDI
        {
            header = Convert.ToBase64String(header),
            sndCount = Read32(header, 0),
            sndsCount = Read32(header, 8),
        };

        int entryCount = (data.Length - HeaderSize) / EntrySize;
        SNDIEntry[] entries = new SNDIEntry[entryCount];
        for (int i = 0; i < entryCount; i++)
        {
            int off = HeaderSize + i * EntrySize;
            byte[] raw = new byte[EntrySize];
            Array.Copy(data, off, raw, 0, EntrySize);

            byte[] tail = new byte[EntrySize - EntryTailOffset];
            Array.Copy(raw, EntryTailOffset, tail, 0, tail.Length);

            entries[i] = new SNDIEntry
            {
                id = Read32(raw, 0x00),
                assetID = Read32(raw, 0x04),
                unknown = Read16(raw, 0x08),
                sampleRate = Read16(raw, 0x0A),
                flags = Read32(raw, 0x0C),
                tail = Convert.ToBase64String(tail),
            };
        }

        sndi.entries = entries;
        return sndi;
    }

    public override object Serialize(object obj)
    {
        SNDI sndi = (SNDI)obj;

        byte[] header;
        if (string.IsNullOrEmpty(sndi.header))
            header = new byte[HeaderSize];
        else
        {
            header = Convert.FromBase64String(sndi.header);
            if (header.Length != HeaderSize)
                header = new byte[HeaderSize];
        }

        Write32(header, 0, sndi.sndCount);
        Write32(header, 8, sndi.sndsCount);

        using var ms = new MemoryStream();
        ms.Write(header, 0, header.Length);

        foreach (SNDIEntry entry in sndi.entries)
        {
            byte[] raw = new byte[EntrySize];
            Write32(raw, 0x00, entry.id);
            Write32(raw, 0x04, entry.assetID);
            Write16(raw, 0x08, entry.unknown);
            Write16(raw, 0x0A, entry.sampleRate);
            Write32(raw, 0x0C, entry.flags);

            byte[] tail = string.IsNullOrEmpty(entry.tail) ? new byte[EntrySize - EntryTailOffset] : Convert.FromBase64String(entry.tail);
            if (tail.Length != EntrySize - EntryTailOffset)
                tail = new byte[EntrySize - EntryTailOffset];
            Array.Copy(tail, 0, raw, EntryTailOffset, tail.Length);

            ms.Write(raw, 0, raw.Length);
        }

        return ms.ToArray();
    }

    private static uint Read32(byte[] data, int offset)
    {
        return (uint)((data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]);
    }

    private static ushort Read16(byte[] data, int offset)
    {
        return (ushort)((data[offset] << 8) | data[offset + 1]);
    }

    private static void Write32(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)(value >> 24);
        data[offset + 1] = (byte)(value >> 16);
        data[offset + 2] = (byte)(value >> 8);
        data[offset + 3] = (byte)value;
    }

    private static void Write16(byte[] data, int offset, ushort value)
    {
        data[offset] = (byte)(value >> 8);
        data[offset + 1] = (byte)value;
    }
}

public class SNDI
{
    public string header { get; set; }
    public uint sndCount { get; set; }
    public uint sndsCount { get; set; }
    public SNDIEntry[] entries { get; set; }
}

public class SNDIEntry
{
    public uint id { get; set; }
    public uint assetID { get; set; }
    public ushort unknown { get; set; }
    public ushort sampleRate { get; set; }
    public uint flags { get; set; }
    public string tail { get; set; }
}