using System;
using System.IO;
using System.Text;

namespace HIP2Json;

public abstract class AbstractDYNAParser
{
    public object ParseSafe(BinaryReader br, long assetStart, long dataStart, short version, string dynaType)
    {
        br.BaseStream.Position = dataStart;

        return Parse(br, assetStart, dataStart, version, dynaType);
    }

    public abstract object Parse(BinaryReader br, long assetStart, long dataStart, short version, string dynaType);

    public abstract byte[] Serialize(object obj, short version, string dynaType);

    public abstract string GetFolderName();

    protected static void WriteFloatBE(BinaryWriter bw, float value)
    {
        byte[] b = BitConverter.GetBytes(value);
        if (Program.BigEndian) Array.Reverse(b);
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
        if (Program.BigEndian) Array.Reverse(b);
        bw.Write(b);
    }

    protected static ushort ReadUInt16BE(BinaryReader br)
    {
        byte[] b = br.ReadBytes(2);
        if (Program.BigEndian) Array.Reverse(b);
        return BitConverter.ToUInt16(b, 0);
    }

    protected static void WriteUInt16BE(BinaryWriter bw, ushort value)
    {
        byte[] b = BitConverter.GetBytes(value);
        if (Program.BigEndian) Array.Reverse(b);
        bw.Write(b);
    }

    protected static void WriteInt32BE(BinaryWriter bw, int value)
    {
        byte[] b = BitConverter.GetBytes(value);
        if (Program.BigEndian) Array.Reverse(b);
        bw.Write(b);
    }

    protected static void WriteInt16BE(BinaryWriter bw, short value)
    {
        byte[] b = BitConverter.GetBytes(value);
        if (Program.BigEndian) Array.Reverse(b);
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
        if (Program.BigEndian) Array.Reverse(b);
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
        if (Program.BigEndian) Array.Reverse(b);
        return BitConverter.ToInt32(b, 0);
    }

    protected static short ReadInt16BE(BinaryReader br)
    {
        byte[] b = br.ReadBytes(2);
        if (Program.BigEndian) Array.Reverse(b);
        return BitConverter.ToInt16(b, 0);
    }

    protected static uint ReadUInt32BE(BinaryReader br)
    {
        byte[] b = br.ReadBytes(4);
        if (Program.BigEndian) Array.Reverse(b);
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

    protected static xBaseAsset ReadBaseAsset(BinaryReader br)
    {
        return new xBaseAsset
        {
            id = ReadUInt32BE(br),
            baseType = ReadByte(br).ToString("X8"),
            linkCount = ReadByte(br),
            baseFlags = (BaseFlags)ReadUInt16BE(br)
        };
    }

    protected static void WriteBaseAsset(BinaryWriter bw, xBaseAsset value)
    {
        WriteUInt32BE(bw, value.id);
        WriteByte(bw, Convert.ToByte(value.baseType.Substring(2), 16));
        WriteByte(bw, value.linkCount);
        WriteUInt16BE(bw, (ushort)value.baseFlags);
    }

    protected static xEntAsset ReadEntAsset(BinaryReader br)
    {
        xEntAsset value = new xEntAsset
        {
            flags = (EntFlags)ReadByte(br),
            subtype = ReadByte(br),
            pflags = ReadByte(br),
            moreFlags = (EntFlagsMore)ReadByte(br),
        };

        if (Program.CurrentGame == GameType.BFBB)
        {
            br.ReadBytes(4);
        }

        value.surfaceID = ReadUInt32BE(br);
        value.ang = ReadVector3BE(br);
        value.pos = ReadVector3BE(br);
        value.scale = ReadVector3BE(br);
        value.redMult = ReadFloatBE(br);
        value.greenMult = ReadFloatBE(br);
        value.blueMult = ReadFloatBE(br);
        value.seeThru = ReadFloatBE(br);
        value.seeThruSpeed = ReadFloatBE(br);
        value.modelInfoID = ReadUInt32BE(br);
        value.animListID = ReadUInt32BE(br);

        return value;
    }

    protected static void WriteEntAsset(BinaryWriter bw, xEntAsset value)
    {
        WriteByte(bw, (byte)value.flags);
        WriteByte(bw, value.subtype);
        WriteByte(bw, value.pflags);
        WriteByte(bw, (byte)value.moreFlags);

        if (Program.CurrentGame == GameType.BFBB)
        {
            bw.Write(new byte[4]);
        }

        WriteUInt32BE(bw, value.surfaceID);
        WriteVector3BE(bw, value.ang);
        WriteVector3BE(bw, value.pos);
        WriteVector3BE(bw, value.scale);
        WriteFloatBE(bw, value.redMult);
        WriteFloatBE(bw, value.greenMult);
        WriteFloatBE(bw, value.blueMult);
        WriteFloatBE(bw, value.seeThru);
        WriteFloatBE(bw, value.seeThruSpeed);
        WriteUInt32BE(bw, value.modelInfoID);
        WriteUInt32BE(bw, value.animListID);
    }
}