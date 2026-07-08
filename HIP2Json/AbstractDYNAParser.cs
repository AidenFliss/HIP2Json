using System;
using System.IO;
using System.Text;

namespace PortHeavyIronGameRewrite;

public abstract class AbstractDYNAParser
{
    public object ParseSafe(BinaryReader br, long assetStart, long dataStart, short version)
    {
        br.BaseStream.Position = dataStart;

        return Parse(br, assetStart, dataStart, version);
    }

    public abstract object Parse(BinaryReader br, long assetStart, long dataStart, short version);

    public abstract byte[] Serialize(object obj, short version);

    public abstract string GetFolderName();

    protected static void WriteFloatBE(BinaryWriter bw, float value)
    {
        byte[] b = BitConverter.GetBytes(value);
        Array.Reverse(b);
        bw.Write(b);
    }

    protected static void WriteFloatLE(BinaryWriter bw, float value)
    {
        byte[] b = BitConverter.GetBytes(value);
        bw.Write(b);
    }

    protected static void WriteInt32LE(BinaryWriter bw, int value)
    {
        byte[] b = BitConverter.GetBytes(value);
        bw.Write(b);
    }

    protected static void WriteUInt32BE(BinaryWriter bw, uint value)
    {
        byte[] b = BitConverter.GetBytes(value);
        Array.Reverse(b);
        bw.Write(b);
    }

    protected static void WriteUInt16BE(BinaryWriter bw, ushort value)
    {
        byte[] b = BitConverter.GetBytes(value);
        Array.Reverse(b);
        bw.Write(b);
    }

    protected static void WriteInt32BE(BinaryWriter bw, int value)
    {
        byte[] b = BitConverter.GetBytes(value);
        Array.Reverse(b);
        bw.Write(b);
    }

    protected static void WriteInt16BE(BinaryWriter bw, short value)
    {
        byte[] b = BitConverter.GetBytes(value);
        Array.Reverse(b);
        bw.Write(b);
    }

    protected static void WriteByte(BinaryWriter bw, byte value)
    {
        bw.Write(value);
    }

    protected static void WriteString(BinaryWriter bw, string value)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(value);
        bw.Write(bytes);
        bw.Write((byte)0);
    }

    protected static void WriteVector2BE(BinaryWriter bw, xVec2 vec)
    {
        WriteFloatBE(bw, vec.x);
        WriteFloatBE(bw, vec.y);
    }

    protected static void WriteVector3BE(BinaryWriter bw, xVec3 vec)
    {
        WriteFloatBE(bw, vec.x);
        WriteFloatBE(bw, vec.y);
        WriteFloatBE(bw, vec.z);
    }

    protected static void WriteColorBE(BinaryWriter bw, xColor color)
    {
        WriteByte(bw, (byte)color.r);
        WriteByte(bw, (byte)color.g);
        WriteByte(bw, (byte)color.b);
        WriteByte(bw, (byte)color.a);
    }

    protected static float ReadFloatBE(BinaryReader br)
    {
        byte[] b = br.ReadBytes(4);
        Array.Reverse(b);
        return BitConverter.ToSingle(b, 0);
    }

    protected static float ReadFloatLE(BinaryReader br)
    {
        byte[] b = br.ReadBytes(4);
        return BitConverter.ToSingle(b, 0);
    }

    protected static int ReadInt32BE(BinaryReader br)
    {
        byte[] b = br.ReadBytes(4);
        Array.Reverse(b);
        return BitConverter.ToInt32(b, 0);
    }

    protected static short ReadInt16BE(BinaryReader br)
    {
        byte[] b = br.ReadBytes(2);
        Array.Reverse(b);
        return BitConverter.ToInt16(b, 0);
    }

    protected static uint ReadUInt32BE(BinaryReader br)
    {
        byte[] b = br.ReadBytes(4);
        Array.Reverse(b);
        return BitConverter.ToUInt32(b, 0);
    }

    protected static byte ReadByte(BinaryReader br)
    {
        return br.ReadByte();
    }

    protected static bool ReadBoolean(BinaryReader br)
    {
        return br.ReadBoolean();
    }

    protected static void WriteBoolean(BinaryWriter bw, bool value)
    {
        bw.Write(value);
    }

    protected static char ReadChar(BinaryReader br)
    {
        return br.ReadChar();
    }

    protected static float ReadSingle(BinaryReader br)
    {
        return br.ReadSingle();
    }

    protected static xVec3 ReadVector3BE(BinaryReader br)
    {
        return new xVec3
        {
            x = ReadFloatBE(br),
            y = ReadFloatBE(br),
            z = ReadFloatBE(br)
        };
    }

    protected static xColor ReadColor(BinaryReader br)
    {
        return new xColor
        {
            r = ReadByte(br),
            g = ReadByte(br),
            b = ReadByte(br),
            a = ReadByte(br)
        };
    }
}