using System;
using System.IO;

namespace PortHeavyIronGameRewrite;

public static class Util
{
    public static short ReadInt16(byte[] bytes, int start, bool s_BigEndian)
    {
        if (bytes.Length - start < 2) throw new EndOfStreamException();
        if (s_BigEndian)
            return (short)(bytes[start] << 8 | bytes[start + 1]);
        else
            return (short)(bytes[start] | (bytes[start + 1] << 8));
    }

    public static ushort ReadUInt16(byte[] bytes, int start, bool s_BigEndian)
    {
        if (bytes.Length - start < 2) throw new EndOfStreamException();
        if (s_BigEndian)
            return (ushort)(bytes[start] << 8 | bytes[start + 1]);
        else
            return (ushort)(bytes[start] | (bytes[start + 1] << 8));
    }

    public static uint ReadUInt32(byte[] bytes, int start, bool s_BigEndian)
    {
        if (bytes.Length - start < 4) throw new EndOfStreamException();
        if (s_BigEndian)
            return (uint)(bytes[start] << 24 | bytes[start + 1] << 16 | bytes[start + 2] << 8 | bytes[start + 3]);
        else
            return (uint)(bytes[start] | (bytes[start + 1] << 8) | (bytes[start + 2] << 16) | (bytes[start + 3] << 24));
    }

    public static float ReadFloat(byte[] bytes, int start, bool s_BigEndian)
    {
        uint u = ReadUInt32(bytes, start, s_BigEndian);
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

    public static void WriteUInt32(BinaryWriter bw, uint v, bool bigEndian)
    {
        var b = BitConverter.GetBytes(v);
        if (Program.BigEndian == true) Array.Reverse(b);
        bw.Write(b);
    }

    public static void WriteUInt16(BinaryWriter bw, ushort v, bool bigEndian)
    {
        var b = BitConverter.GetBytes(v);
        if (Program.BigEndian == true) Array.Reverse(b);
        bw.Write(b);
    }

    public static void WriteFloat(BinaryWriter bw, float v, bool bigEndian)
    {
        var b = BitConverter.GetBytes(v);
        if (Program.BigEndian == true) Array.Reverse(b);
        bw.Write(b);
    }
}