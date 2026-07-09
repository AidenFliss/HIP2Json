using System;

public static class Crc32Mpeg2 //based off of https://web.archive.org/web/20230520235758/https://opensource.apple.com/source/CommonCrypto/CommonCrypto-60074/libcn/crc32-mpeg-2.c.auto.html
{
    private static readonly uint[] Table = CreateTable();

    private static uint[] CreateTable()
    {
        uint[] table = new uint[256];

        for (uint i = 0; i < 256; i++)
        {
            uint crc = i << 24;

            for (int j = 0; j < 8; j++)
            {
                if ((crc & 0x80000000) != 0)
                    crc = (crc << 1) ^ 0x04C11DB7;
                else
                    crc <<= 1;
            }

            table[i] = crc;
        }

        return table;
    }

    public static uint Compute(ReadOnlySpan<byte> data)
    {
        uint crc = 0xFFFFFFFF;

        foreach (byte b in data)
        {
            crc = (crc << 8) ^ Table[((crc >> 24) ^ b) & 0xFF];
        }

        return crc;
    }
}