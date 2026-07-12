using System;
using System.IO;

namespace PortHeavyIronGameRewrite;

public static class Util
{
    public static short ReadInt16(byte[] bytes, int start)
    {
        if (bytes.Length - start < 2) throw new EndOfStreamException();

        byte[] b = bytes[start..(start + 2)];
        if (Program.BigEndian) Array.Reverse(b);

        return BitConverter.ToInt16(b);
    }

    public static ushort ReadUInt16(byte[] bytes, int start)
    {
        if (bytes.Length - start < 2) throw new EndOfStreamException();

        byte[] b = bytes[start..(start + 2)];
        if (Program.BigEndian) Array.Reverse(b);

        return BitConverter.ToUInt16(b);
    }

    public static uint ReadUInt32(byte[] bytes, int start)
    {
        if (bytes.Length - start < 4) throw new EndOfStreamException();

        byte[] b = bytes[start..(start + 4)];
        if (Program.BigEndian) Array.Reverse(b);

        return BitConverter.ToUInt32(b);
    }

    public static float ReadFloat(byte[] bytes, int start)
    {
        uint u = ReadUInt32(bytes, start);
        return UIntToFloat(u);
    }

    public static float UIntToFloat(uint u)
    {
        return BitConverter.Int32BitsToSingle(unchecked((int)u));
    }

    public static xVec3 ReadVec3(BinaryReader br) =>
        new xVec3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

    public static xVec4 ReadVec4(BinaryReader br) =>
        new xVec4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

    public static xColor ReadColor(BinaryReader br) =>
        new xColor(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

    public static void WriteUInt32(BinaryWriter bw, uint v)
    {
        var b = BitConverter.GetBytes(v);
        if (Program.BigEndian == true) Array.Reverse(b);
        bw.Write(b);
    }

    public static void WriteUInt16(BinaryWriter bw, ushort v)
    {
        var b = BitConverter.GetBytes(v);
        if (Program.BigEndian == true) Array.Reverse(b);
        bw.Write(b);
    }

    public static void WriteFloat(BinaryWriter bw, float v)
    {
        var b = BitConverter.GetBytes(v);
        if (Program.BigEndian == true) Array.Reverse(b);
        bw.Write(b);
    }

    public static void DecryptCRDT(ref byte[] data)
    {
        byte last = 0;
        const string key = "xCMChunkHand";

        for (int i = 0; i < data.Length; i++)
        {
            last = (byte)(data[i] ^ last ^ key[(i + 0x18) % key.Length]);
            data[i] = last;
        }
    }
}