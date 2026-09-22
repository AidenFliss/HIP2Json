using System;
using System.IO;

namespace HIP2Json;

public sealed class SNDIParser : AssetParser
{
    private const int EntrySize = 100;
    private const int EntryTailOffset = 0x10;
    private const int HeaderSize = 0x10;

    private const int Ps2EntrySize = 0x30;
    private const int Ps2HeaderSize = 0x08;

    private static bool IsPS2 => Program.CurrentPlatform == GamePlatform.PS2;

    private static int EntrySizeOf => IsPS2 ? Ps2EntrySize : EntrySize;
    private static int EntryTailOffsetOf => IsPS2 ? 0 : EntryTailOffset;
    private static int HeaderSizeOf => IsPS2 ? Ps2HeaderSize : HeaderSize;

    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        byte[] data = br.ReadBytes((int)(br.BaseStream.Length - br.BaseStream.Position));

        int headerSize = HeaderSizeOf;

        if (data.Length < headerSize)
            return new SNDI
            {
                header = Convert.ToBase64String(data),
                entries = Array.Empty<SNDIEntry>(),
            };

        byte[] header = new byte[headerSize];
        Array.Copy(data, 0, header, 0, headerSize);

        SNDI sndi = new SNDI
        {
            header = Convert.ToBase64String(header),
            sndCount = Read32(header, 0),
            sndsCount = Read32(header, IsPS2 ? 4 : 8),
        };

        int entrySize = EntrySizeOf;
        int entryCount = (data.Length - headerSize) / entrySize;
        SNDIEntry[] entries = new SNDIEntry[entryCount];
        for (int i = 0; i < entryCount; i++)
        {
            int off = headerSize + i * entrySize;
            byte[] raw = new byte[entrySize];
            Array.Copy(data, off, raw, 0, entrySize);

            int tailOffset = EntryTailOffsetOf;
            byte[] tail = new byte[entrySize - tailOffset];
            Array.Copy(raw, tailOffset, tail, 0, tail.Length);

            if (IsPS2)
            {
                uint ps2Id = Read32(raw, 0x08);
                entries[i] = new SNDIEntry
                {
                    id = ps2Id,
                    assetID = ps2Id,
                    unknown = 0,
                    sampleRate = (ushort)Read32(raw, 0x10),
                    flags = Read32(raw, 0x04),
                    tail = Convert.ToBase64String(tail),
                };
            }
            else
            {
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
        }

        int consumed = headerSize + entryCount * entrySize;
        sndi.trailing = consumed < data.Length ? Convert.ToBase64String(data, consumed, data.Length - consumed) : string.Empty;

        sndi.entries = entries;
        return sndi;
    }

    public override object Serialize(object obj)
    {
        SNDI sndi = (SNDI)obj;

        int headerSize = HeaderSizeOf;
        int entrySize = EntrySizeOf;
        int tailOffset = EntryTailOffsetOf;

        byte[] header;
        if (string.IsNullOrEmpty(sndi.header))
            header = new byte[headerSize];
        else
        {
            header = Convert.FromBase64String(sndi.header);
            if (header.Length != headerSize)
                header = new byte[headerSize];
        }

        Write32(header, 0, sndi.sndCount);
        Write32(header, IsPS2 ? 4 : 8, sndi.sndsCount);

        using var ms = new MemoryStream();
        ms.Write(header, 0, header.Length);

        foreach (SNDIEntry entry in sndi.entries)
        {
            byte[] raw = new byte[entrySize];

            if (IsPS2)
            {
                byte[] tail = string.IsNullOrEmpty(entry.tail) ? new byte[entrySize] : Convert.FromBase64String(entry.tail);
                if (tail.Length != entrySize)
                    tail = new byte[entrySize];
                Array.Copy(tail, 0, raw, 0, entrySize);

                Write32(raw, 0x08, entry.id);
                Write32(raw, 0x10, entry.sampleRate);
                Write32(raw, 0x04, entry.flags);
            }
            else
            {
                Write32(raw, 0x00, entry.id);
                Write32(raw, 0x04, entry.assetID);
                Write16(raw, 0x08, entry.unknown);
                Write16(raw, 0x0A, entry.sampleRate);
                Write32(raw, 0x0C, entry.flags);

                byte[] tail = string.IsNullOrEmpty(entry.tail) ? new byte[entrySize - EntryTailOffset] : Convert.FromBase64String(entry.tail);
                if (tail.Length != EntrySize - EntryTailOffset)
                    tail = new byte[EntrySize - EntryTailOffset];
                Array.Copy(tail, 0, raw, EntryTailOffset, tail.Length);
            }

            ms.Write(raw, 0, raw.Length);
        }

        if (!string.IsNullOrEmpty(sndi.trailing))
            ms.Write(Convert.FromBase64String(sndi.trailing));

        return ms.ToArray();
    }

    private static uint Read32(byte[] data, int offset)
    {
        if (IsPS2)
            return (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));

        return (uint)((data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]);
    }

    private static ushort Read16(byte[] data, int offset)
    {
        return (ushort)((data[offset] << 8) | data[offset + 1]);
    }

    private static void Write32(byte[] data, int offset, uint value)
    {
        if (IsPS2)
        {
            data[offset] = (byte)value;
            data[offset + 1] = (byte)(value >> 8);
            data[offset + 2] = (byte)(value >> 16);
            data[offset + 3] = (byte)(value >> 24);
            return;
        }

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
    public string trailing { get; set; }
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