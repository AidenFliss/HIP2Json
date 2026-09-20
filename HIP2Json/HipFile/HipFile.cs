using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HIP2Json;

public enum Game
{
    Unknown,
    Scooby,
    BFBB,
    Incredibles,
    ROTU,
    RatProto,
}

public enum Platform
{
    Unknown,
    PS2,
    GameCube,
    Xbox,
}

public enum AHDRFlags
{
    SOURCE_FILE = 1,
    SOURCE_VIRTUAL = 2,
    READ_TRANSFORM = 4,
    WRITE_TRANSFORM = 8,
}

public enum LayerType_BFBB
{
    DEFAULT = 0,
    TEXTURE = 1,
    BSP = 2,
    MODEL = 3,
    ANIMATION = 4,
    VRAM = 5,
    SRAM = 6,
    SNDTOC = 7,
    CUTSCENE = 8,
    CUTSCENETOC = 9,
    JSPINFO = 10,
}

public enum LayerType_TSSM
{
    DEFAULT = 0,
    TEXTURE = 1,
    TEXTURE_STRM = 2,
    BSP = 3,
    MODEL = 4,
    ANIMATION = 5,
    VRAM = 6,
    SRAM = 7,
    SNDTOC = 8,
    CUTSCENE = 9,
    CUTSCENETOC = 10,
    JSPINFO = 11,
}

internal static class HipByte
{
    public static int Switch(int value) => BitConverter.ToInt32(BitConverter.GetBytes(value).Reverse().ToArray(), 0);

    public static uint Switch(uint value) => BitConverter.ToUInt32(BitConverter.GetBytes(value).Reverse().ToArray(), 0);

    public static void AddBigEndian(List<byte> listBytes, int value) => listBytes.AddRange(BitConverter.GetBytes(value).Reverse());

    public static void AddBigEndian(List<byte> listBytes, uint value) => listBytes.AddRange(BitConverter.GetBytes(value).Reverse());

    public static string ReadMagic(BinaryReader binaryReader) => new string(binaryReader.ReadChars(4));

    public static string ReadString(BinaryReader binaryReader)
    {
        List<char> charList = new List<char>();
        do
            charList.Add((char)binaryReader.ReadByte());
        while (charList.Last() != '\0');
        charList.Remove('\0');

        if (charList.Count % 2 == 0)
            binaryReader.BaseStream.Position += 1;

        return new string(charList.ToArray());
    }

    public static void AddString(List<byte> listBytes, string writeString)
    {
        foreach (char i in writeString)
            listBytes.Add((byte)i);

        if (writeString.Length % 2 == 0)
            listBytes.AddRange(new byte[] { 0, 0 });
        if (writeString.Length % 2 == 1)
            listBytes.AddRange(new byte[] { 0 });
    }
}

public abstract class HipSection
{
    public int SectionSize;

    public void SetBytes(Game game, Platform platform, ref List<byte> listBytes)
    {
        int position = listBytes.Count;
        listBytes.AddRange(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 });

        WriteBody(game, platform, listBytes);

        SectionSize = listBytes.Count - position - 8;

        string magic = GetMagic();
        byte[] sizeBytes = BitConverter.GetBytes(SectionSize);

        listBytes[position + 0] = (byte)magic[0];
        listBytes[position + 1] = (byte)magic[1];
        listBytes[position + 2] = (byte)magic[2];
        listBytes[position + 3] = (byte)magic[3];
        listBytes[position + 4] = sizeBytes[3];
        listBytes[position + 5] = sizeBytes[2];
        listBytes[position + 6] = sizeBytes[1];
        listBytes[position + 7] = sizeBytes[0];
    }

    protected abstract string GetMagic();

    protected abstract void WriteBody(Game game, Platform platform, List<byte> listBytes);
}

public class Section_HIPA : HipSection
{
    public Section_HIPA() { }

    public Section_HIPA(BinaryReader binaryReader)
    {
        SectionSize = HipByte.Switch(binaryReader.ReadInt32());
    }

    protected override string GetMagic() => "HIPA";

    protected override void WriteBody(Game game, Platform platform, List<byte> listBytes) { }
}

public class Section_PACK : HipSection
{
    public Section_PVER PVER;
    public Section_PFLG PFLG;
    public Section_PCNT PCNT;
    public Section_PCRT PCRT;
    public Section_PMOD PMOD;
    public Section_PLAT PLAT;

    public Section_PACK() { }

    public Section_PACK(BinaryReader binaryReader, out Game game, out Platform platform)
    {
        SectionSize = HipByte.Switch(binaryReader.ReadInt32());
        long startSectionPosition = binaryReader.BaseStream.Position;

        if (HipByte.ReadMagic(binaryReader) != "PVER") throw new Exception();
        PVER = new Section_PVER(binaryReader, out game);

        if (HipByte.ReadMagic(binaryReader) != "PFLG") throw new Exception();
        PFLG = new Section_PFLG(binaryReader, game, out game);

        if (HipByte.ReadMagic(binaryReader) != "PCNT") throw new Exception();
        PCNT = new Section_PCNT(binaryReader);

        if (HipByte.ReadMagic(binaryReader) != "PCRT") throw new Exception();
        PCRT = new Section_PCRT(binaryReader);

        if (HipByte.ReadMagic(binaryReader) != "PMOD") throw new Exception();
        PMOD = new Section_PMOD(binaryReader);

        if (binaryReader.BaseStream.Position == startSectionPosition + SectionSize)
        {
            platform = Platform.Unknown;
            return;
        }

        if (HipByte.ReadMagic(binaryReader) != "PLAT") throw new Exception();
        PLAT = new Section_PLAT(binaryReader, game, out platform);
    }

    protected override string GetMagic() => "PACK";

    protected override void WriteBody(Game game, Platform platform, List<byte> listBytes)
    {
        PVER.SetBytes(game, platform, ref listBytes);
        PFLG.SetBytes(game, platform, ref listBytes);
        PCNT.SetBytes(game, platform, ref listBytes);
        PCRT.SetBytes(game, platform, ref listBytes);
        PMOD.SetBytes(game, platform, ref listBytes);
        if (PLAT != null)
            PLAT.SetBytes(game, platform, ref listBytes);
    }
}

public class Section_PVER : HipSection
{
    public int subVersion;
    public int clientVersion;
    public int compatible;

    public Section_PVER(int subVersion, int clientVersion, int compatible)
    {
        this.subVersion = subVersion;
        this.clientVersion = clientVersion;
        this.compatible = compatible;
    }

    public Section_PVER(BinaryReader binaryReader, out Game game)
    {
        SectionSize = HipByte.Switch(binaryReader.ReadInt32());
        subVersion = HipByte.Switch(binaryReader.ReadInt32());
        clientVersion = HipByte.Switch(binaryReader.ReadInt32());
        compatible = HipByte.Switch(binaryReader.ReadInt32());

        if (clientVersion == 262150)
            game = Game.Scooby;
        else if (clientVersion == 655375)
            game = Game.Incredibles;
        else
            game = Game.Unknown;
    }

    protected override string GetMagic() => "PVER";

    protected override void WriteBody(Game game, Platform platform, List<byte> listBytes)
    {
        HipByte.AddBigEndian(listBytes, subVersion);
        HipByte.AddBigEndian(listBytes, clientVersion);
        HipByte.AddBigEndian(listBytes, compatible);
    }
}

public class Section_PFLG : HipSection
{
    public int flags;

    public Section_PFLG(int flags)
    {
        this.flags = flags;
    }

    public Section_PFLG(BinaryReader binaryReader, Game game, out Game outGame)
    {
        SectionSize = HipByte.Switch(binaryReader.ReadInt32());
        flags = HipByte.Switch(binaryReader.ReadInt32());

        if (flags != 0x2E && game >= Game.Incredibles)
            outGame = Game.BFBB;
        else
            outGame = game;
    }

    protected override string GetMagic() => "PFLG";

    protected override void WriteBody(Game game, Platform platform, List<byte> listBytes)
    {
        HipByte.AddBigEndian(listBytes, flags);
    }
}

public class Section_PCNT : HipSection
{
    public int AHDRCount;
    public int LHDRCount;
    public int sizeOfLargestSourceFileAsset;
    public int sizeOfLargestLayer;
    public int sizeOfLargestSourceVirtualAsset;

    public Section_PCNT(int AHDRCount, int LHDRCount, int sizeOfLargestSourceFileAsset, int sizeOfLargestLayer, int sizeOfLargestSourceVirtualAsset)
    {
        this.AHDRCount = AHDRCount;
        this.LHDRCount = LHDRCount;
        this.sizeOfLargestSourceFileAsset = sizeOfLargestSourceFileAsset;
        this.sizeOfLargestLayer = sizeOfLargestLayer;
        this.sizeOfLargestSourceVirtualAsset = sizeOfLargestSourceVirtualAsset;
    }

    public Section_PCNT(BinaryReader binaryReader)
    {
        SectionSize = HipByte.Switch(binaryReader.ReadInt32());
        AHDRCount = HipByte.Switch(binaryReader.ReadInt32());
        LHDRCount = HipByte.Switch(binaryReader.ReadInt32());
        sizeOfLargestSourceFileAsset = HipByte.Switch(binaryReader.ReadInt32());
        sizeOfLargestLayer = HipByte.Switch(binaryReader.ReadInt32());
        sizeOfLargestSourceVirtualAsset = HipByte.Switch(binaryReader.ReadInt32());
    }

    protected override string GetMagic() => "PCNT";

    protected override void WriteBody(Game game, Platform platform, List<byte> listBytes)
    {
        HipByte.AddBigEndian(listBytes, AHDRCount);
        HipByte.AddBigEndian(listBytes, LHDRCount);
        HipByte.AddBigEndian(listBytes, sizeOfLargestSourceFileAsset);
        HipByte.AddBigEndian(listBytes, sizeOfLargestLayer);
        HipByte.AddBigEndian(listBytes, sizeOfLargestSourceVirtualAsset);
    }
}

public class Section_PCRT : HipSection
{
    public int fileDate;
    public string dateString;

    public Section_PCRT(int fileDate, string dateString)
    {
        this.fileDate = fileDate;
        this.dateString = dateString;
    }

    public Section_PCRT(BinaryReader binaryReader)
    {
        SectionSize = HipByte.Switch(binaryReader.ReadInt32());
        fileDate = HipByte.Switch(binaryReader.ReadInt32());
        dateString = HipByte.ReadString(binaryReader);
    }

    protected override string GetMagic() => "PCRT";

    protected override void WriteBody(Game game, Platform platform, List<byte> listBytes)
    {
        HipByte.AddBigEndian(listBytes, fileDate);
        HipByte.AddString(listBytes, dateString);
    }
}

public class Section_PMOD : HipSection
{
    public int modDate;

    public Section_PMOD(int modDate)
    {
        this.modDate = modDate;
    }

    public Section_PMOD(BinaryReader binaryReader)
    {
        SectionSize = HipByte.Switch(binaryReader.ReadInt32());
        modDate = HipByte.Switch(binaryReader.ReadInt32());
    }

    protected override string GetMagic() => "PMOD";

    protected override void WriteBody(Game game, Platform platform, List<byte> listBytes)
    {
        HipByte.AddBigEndian(listBytes, modDate);
    }
}

public class Section_PLAT : HipSection
{
    public string targetPlatform;
    public string targetPlatformName;
    public string regionFormat;
    public string language;
    public string targetGame;

    public Section_PLAT() { }

    public Section_PLAT(BinaryReader binaryReader, Game game, out Platform platform)
    {
        SectionSize = HipByte.Switch(binaryReader.ReadInt32());

        if (game == Game.BFBB)
        {
            targetPlatform = HipByte.ReadString(binaryReader);
            targetPlatformName = HipByte.ReadString(binaryReader);
            regionFormat = HipByte.ReadString(binaryReader);
            language = HipByte.ReadString(binaryReader);
            targetGame = HipByte.ReadString(binaryReader);
        }
        else if (game >= Game.Incredibles)
        {
            targetPlatform = HipByte.ReadString(binaryReader);
            language = HipByte.ReadString(binaryReader);
            regionFormat = HipByte.ReadString(binaryReader);
            targetGame = HipByte.ReadString(binaryReader);
        }
        else
            throw new Exception("PLAT reading error: unsupported PLAT version");

        platform = GetPlatform();
    }

    protected override string GetMagic() => "PLAT";

    protected override void WriteBody(Game game, Platform platform, List<byte> listBytes)
    {
        if (game == Game.BFBB)
        {
            HipByte.AddString(listBytes, targetPlatform);
            HipByte.AddString(listBytes, targetPlatformName);
            HipByte.AddString(listBytes, regionFormat);
            HipByte.AddString(listBytes, language);
            HipByte.AddString(listBytes, targetGame);
        }
        else if (game >= Game.Incredibles)
        {
            HipByte.AddString(listBytes, targetPlatform);
            HipByte.AddString(listBytes, language);
            HipByte.AddString(listBytes, regionFormat);
            HipByte.AddString(listBytes, targetGame);
        }
        else
            throw new Exception("PLAT writing error");
    }

    public Platform GetPlatform()
    {
        if (targetPlatform == "XB" || targetPlatformName == "Xbox" || targetPlatform == "BX")
            return Platform.Xbox;
        if (targetPlatform == "GC" || targetPlatformName == "GameCube")
            return Platform.GameCube;
        if (targetPlatform == "P2" || targetPlatform == "PS2" || targetPlatformName == "PlayStation 2")
            return Platform.PS2;
        return Platform.Unknown;
    }
}

public class Section_DICT : HipSection
{
    public Section_ATOC ATOC;
    public Section_LTOC LTOC;

    public Section_DICT()
    {
        ATOC = new Section_ATOC();
        LTOC = new Section_LTOC();
    }

    public Section_DICT(BinaryReader binaryReader, Platform platform)
    {
        SectionSize = HipByte.Switch(binaryReader.ReadInt32());

        if (HipByte.ReadMagic(binaryReader) != "ATOC") throw new Exception();
        ATOC = new Section_ATOC(binaryReader, platform);

        if (HipByte.ReadMagic(binaryReader) != "LTOC") throw new Exception();
        LTOC = new Section_LTOC(binaryReader);

        foreach (var AHDR in ATOC.AHDRList)
            if (AHDR.assetType == AssetType.JSP)
            {
                var containingLayer = LTOC.LHDRList.Where(LHDR => LHDR.assetIDlist.Contains(AHDR.assetID)).FirstOrDefault();
                if (containingLayer != null && containingLayer.layerType > 9)
                    AHDR.assetType = AssetType.JSPInfo;
            }
    }

    protected override string GetMagic() => "DICT";

    protected override void WriteBody(Game game, Platform platform, List<byte> listBytes)
    {
        ATOC.SetBytes(game, platform, ref listBytes);
        LTOC.SetBytes(game, platform, ref listBytes);
    }
}

public class Section_ATOC : HipSection
{
    public static bool noAHDR;

    public Section_AINF AINF;
    public List<Section_AHDR> AHDRList;

    public Section_ATOC()
    {
        AINF = new Section_AINF(0);
        AHDRList = new List<Section_AHDR>();
    }

    public Section_ATOC(BinaryReader binaryReader, Platform platform)
    {
        SectionSize = HipByte.Switch(binaryReader.ReadInt32());
        long startSectionPosition = binaryReader.BaseStream.Position;

        if (HipByte.ReadMagic(binaryReader) != "AINF") throw new Exception();
        AINF = new Section_AINF(binaryReader);

        AHDRList = new List<Section_AHDR>();
        while (binaryReader.BaseStream.Position < startSectionPosition + SectionSize)
        {
            if (HipByte.ReadMagic(binaryReader) != "AHDR") throw new Exception();
            AHDRList.Add(new Section_AHDR(binaryReader, platform));
        }

        noAHDR = AHDRList.Count == 0;
    }

    protected override string GetMagic() => "ATOC";

    protected override void WriteBody(Game game, Platform platform, List<byte> listBytes)
    {
        AINF.SetBytes(game, platform, ref listBytes);

        foreach (Section_AHDR i in AHDRList)
            i.SetBytes(game, platform, ref listBytes);

        noAHDR = AHDRList.Count == 0;
    }

    public Section_AHDR GetFromAssetID(uint assetID)
    {
        foreach (var AHDR in AHDRList)
            if (AHDR.assetID == assetID)
                return AHDR;
        return null;
    }

    public void SortAHDRList()
    {
        AHDRList = AHDRList.OrderBy(ahdr => ahdr.assetID).ToList();
    }
}

public class Section_AINF : HipSection
{
    public int value;

    public Section_AINF(int value)
    {
        this.value = value;
    }

    public Section_AINF(BinaryReader binaryReader)
    {
        SectionSize = HipByte.Switch(binaryReader.ReadInt32());
        value = HipByte.Switch(binaryReader.ReadInt32());
    }

    protected override string GetMagic() => "AINF";

    protected override void WriteBody(Game game, Platform platform, List<byte> listBytes)
    {
        HipByte.AddBigEndian(listBytes, value);
    }
}

public class Section_ADBG : HipSection
{
    public int alignment;
    public string assetName;
    public string assetFileName;
    public int checksum;

    public Section_ADBG(int alignment, string assetName, string assetFileName, int checksum)
    {
        this.alignment = alignment;
        this.assetName = assetName;
        this.assetFileName = assetFileName;
        this.checksum = checksum;
    }

    public Section_ADBG(BinaryReader binaryReader)
    {
        SectionSize = HipByte.Switch(binaryReader.ReadInt32());
        alignment = HipByte.Switch(binaryReader.ReadInt32());
        assetName = HipByte.ReadString(binaryReader);
        assetFileName = HipByte.ReadString(binaryReader);
        checksum = HipByte.Switch(binaryReader.ReadInt32());
    }

    protected override string GetMagic() => "ADBG";

    protected override void WriteBody(Game game, Platform platform, List<byte> listBytes)
    {
        HipByte.AddBigEndian(listBytes, alignment);
        HipByte.AddString(listBytes, assetName);
        HipByte.AddString(listBytes, assetFileName);
        HipByte.AddBigEndian(listBytes, checksum);
    }
}

public class Section_LTOC : HipSection
{
    public Section_LINF LINF;
    public List<Section_LHDR> LHDRList;

    public Section_LTOC()
    {
        LINF = new Section_LINF(0);
        LHDRList = new List<Section_LHDR>();
    }

    public Section_LTOC(BinaryReader binaryReader)
    {
        SectionSize = HipByte.Switch(binaryReader.ReadInt32());
        long startSectionPosition = binaryReader.BaseStream.Position;

        if (HipByte.ReadMagic(binaryReader) != "LINF") throw new Exception();
        LINF = new Section_LINF(binaryReader);

        LHDRList = new List<Section_LHDR>();
        while (binaryReader.BaseStream.Position < startSectionPosition + SectionSize)
        {
            if (HipByte.ReadMagic(binaryReader) != "LHDR") throw new Exception();
            LHDRList.Add(new Section_LHDR(binaryReader));
        }

        binaryReader.BaseStream.Position = startSectionPosition + SectionSize;
    }

    protected override string GetMagic() => "LTOC";

    protected override void WriteBody(Game game, Platform platform, List<byte> listBytes)
    {
        LINF.SetBytes(game, platform, ref listBytes);
        foreach (Section_LHDR i in LHDRList)
            i.SetBytes(game, platform, ref listBytes);
    }
}

public class Section_LINF : HipSection
{
    public int value;

    public Section_LINF(int value)
    {
        this.value = value;
    }

    public Section_LINF(BinaryReader binaryReader)
    {
        SectionSize = HipByte.Switch(binaryReader.ReadInt32());
        value = HipByte.Switch(binaryReader.ReadInt32());
    }

    protected override string GetMagic() => "LINF";

    protected override void WriteBody(Game game, Platform platform, List<byte> listBytes)
    {
        HipByte.AddBigEndian(listBytes, value);
    }
}

public class Section_LHDR : HipSection
{
    public int layerType;
    public List<uint> assetIDlist;
    public Section_LDBG LDBG;

    public Section_LHDR() { }

    public Section_LHDR(BinaryReader binaryReader)
    {
        SectionSize = HipByte.Switch(binaryReader.ReadInt32());
        layerType = HipByte.Switch(binaryReader.ReadInt32());
        int assetAmount = HipByte.Switch(binaryReader.ReadInt32());

        assetIDlist = new List<uint>(assetAmount);
        for (int i = 0; i < assetAmount; i++)
            assetIDlist.Add(HipByte.Switch(binaryReader.ReadUInt32()));

        if (HipByte.ReadMagic(binaryReader) != "LDBG") throw new Exception();
        LDBG = new Section_LDBG(binaryReader);
    }

    protected override string GetMagic() => "LHDR";

    protected override void WriteBody(Game game, Platform platform, List<byte> listBytes)
    {
        HipByte.AddBigEndian(listBytes, layerType);
        HipByte.AddBigEndian(listBytes, assetIDlist.Count());

        foreach (int i in assetIDlist)
            HipByte.AddBigEndian(listBytes, (uint)i);

        LDBG.SetBytes(game, platform, ref listBytes);
    }
}

public class Section_LDBG : HipSection
{
    public int value;

    public Section_LDBG(int value)
    {
        this.value = value;
    }

    public Section_LDBG(BinaryReader binaryReader)
    {
        SectionSize = HipByte.Switch(binaryReader.ReadInt32());
        value = HipByte.Switch(binaryReader.ReadInt32());
    }

    protected override string GetMagic() => "LDBG";

    protected override void WriteBody(Game game, Platform platform, List<byte> listBytes)
    {
        HipByte.AddBigEndian(listBytes, value);
    }
}

public class Section_STRM : HipSection
{
    public Section_DHDR DHDR;
    public Section_DPAK DPAK;

    public Section_STRM()
    {
        DHDR = new Section_DHDR(-1);
        DPAK = new Section_DPAK();
    }

    public Section_STRM(BinaryReader binaryReader)
    {
        SectionSize = HipByte.Switch(binaryReader.ReadInt32());

        if (HipByte.ReadMagic(binaryReader) != "DHDR") throw new Exception();
        DHDR = new Section_DHDR(binaryReader);

        if (HipByte.ReadMagic(binaryReader) != "DPAK") throw new Exception();
        DPAK = new Section_DPAK(binaryReader);
    }

    protected override string GetMagic() => "STRM";

    protected override void WriteBody(Game game, Platform platform, List<byte> listBytes)
    {
        DHDR.SetBytes(game, platform, ref listBytes);
        DPAK.SetBytes(game, platform, ref listBytes);
    }
}

public class Section_DHDR : HipSection
{
    public int value;

    public Section_DHDR(int value)
    {
        this.value = value;
    }

    public Section_DHDR(BinaryReader binaryReader)
    {
        SectionSize = HipByte.Switch(binaryReader.ReadInt32());
        value = HipByte.Switch(binaryReader.ReadInt32());
    }

    protected override string GetMagic() => "DHDR";

    protected override void WriteBody(Game game, Platform platform, List<byte> listBytes)
    {
        HipByte.AddBigEndian(listBytes, value);
    }
}

public class Section_DPAK : HipSection
{
    public int globalRelativeStartOffset;
    public byte[] data;

    public Section_DPAK() { }

    public Section_DPAK(BinaryReader binaryReader)
    {
        SectionSize = HipByte.Switch(binaryReader.ReadInt32());

        if (Section_ATOC.noAHDR)
        {
            data = binaryReader.ReadBytes(SectionSize);
        }
        else
        {
            int firstPadding = HipByte.Switch(binaryReader.ReadInt32());

            binaryReader.BaseStream.Position += firstPadding;

            globalRelativeStartOffset = (int)binaryReader.BaseStream.Position;

            int sizeOfData = SectionSize - 4 - firstPadding;
            data = binaryReader.ReadBytes(sizeOfData);
        }
    }

    protected override string GetMagic() => "DPAK";

    protected override void WriteBody(Game game, Platform platform, List<byte> listBytes)
    {
        int firstPaddingPosition = listBytes.Count;

        listBytes.AddRange(new byte[] { 0, 0, 0, 0 });

        int alignment = platform == Platform.GameCube ? 0x20 : 0x800;

        while (listBytes.Count % alignment != 0)
            listBytes.Add(0x33);

        globalRelativeStartOffset = listBytes.Count;

        int firstPadding = listBytes.Count - (firstPaddingPosition + 4);

        byte[] firstPaddingBytes = BitConverter.GetBytes(firstPadding);
        listBytes[firstPaddingPosition + 0] = firstPaddingBytes[3];
        listBytes[firstPaddingPosition + 1] = firstPaddingBytes[2];
        listBytes[firstPaddingPosition + 2] = firstPaddingBytes[1];
        listBytes[firstPaddingPosition + 3] = firstPaddingBytes[0];

        if (data != null)
            listBytes.AddRange(data);
    }
}

public class Section_HIPB : HipSection
{
    public static readonly int CurrentVersion = 3;

    public bool VersionMismatch = false;
    public int HasNoLayers;
    public Platform ScoobyPlatform = Platform.Unknown;
    public Game IncrediblesGame = Game.Unknown;
    public Dictionary<int, string> LayerNames = new Dictionary<int, string>();

    public Section_HIPB() { }

    public Section_HIPB(BinaryReader binaryReader)
    {
        SectionSize = HipByte.Switch(binaryReader.ReadInt32());

        int version = HipByte.Switch(binaryReader.ReadInt32());

        HasNoLayers = 0;
        if (version >= 1)
            HasNoLayers = HipByte.Switch(binaryReader.ReadInt32());

        ScoobyPlatform = Platform.Unknown;
        if (version >= 2)
        {
            ScoobyPlatform = (Platform)HipByte.Switch(binaryReader.ReadInt32());
            int customLayerNameCount = HipByte.Switch(binaryReader.ReadInt32());
            for (int i = 0; i < customLayerNameCount; i++)
            {
                int index = HipByte.Switch(binaryReader.ReadInt32());
                string layerName = HipByte.ReadString(binaryReader);
                LayerNames[index] = layerName;
            }
        }

        if (version >= 3)
            IncrediblesGame = (Game)HipByte.Switch(binaryReader.ReadInt32());

        if (version > CurrentVersion)
            VersionMismatch = true;
    }

    protected override string GetMagic() => "HIPB";

    protected override void WriteBody(Game game, Platform platform, List<byte> listBytes)
    {
        HipByte.AddBigEndian(listBytes, CurrentVersion);
        HipByte.AddBigEndian(listBytes, HasNoLayers);
        HipByte.AddBigEndian(listBytes, (int)ScoobyPlatform);
        HipByte.AddBigEndian(listBytes, LayerNames.Count);
        foreach (int index in LayerNames.Keys)
        {
            HipByte.AddBigEndian(listBytes, index);
            HipByte.AddString(listBytes, LayerNames[index]);
        }
        HipByte.AddBigEndian(listBytes, (int)IncrediblesGame);
    }
}

public class Section_AHDR : HipSection
{
    public uint assetID;
    public AssetType assetType;
    public int fileOffset;
    public int fileSize;
    public int plusValue;
    public AHDRFlags flags;
    public Section_ADBG ADBG;

    public byte[] data;

    public Section_AHDR() { }

    public Section_AHDR(uint assetID, string assetTypeName, AHDRFlags flags, Section_ADBG ADBG)
    {
        this.assetID = assetID;

        if (!Enum.TryParse<AssetType>(assetTypeName, out assetType))
            throw new Exception("Unknown asset type: " + assetTypeName);

        this.flags = flags;
        this.ADBG = ADBG;
    }

    public Section_AHDR(BinaryReader binaryReader, Platform platform)
    {
        SectionSize = HipByte.Switch(binaryReader.ReadInt32());

        assetID = HipByte.Switch(binaryReader.ReadUInt32());
        string code = HipByte.ReadMagic(binaryReader);
        fileOffset = HipByte.Switch(binaryReader.ReadInt32());
        fileSize = HipByte.Switch(binaryReader.ReadInt32());
        plusValue = HipByte.Switch(binaryReader.ReadInt32());
        flags = (AHDRFlags)HipByte.Switch(binaryReader.ReadInt32());

        if (HipByte.ReadMagic(binaryReader) != "ADBG") throw new Exception();
        ADBG = new Section_ADBG(binaryReader);

        long savePosition = binaryReader.BaseStream.Position;
        binaryReader.BaseStream.Position = fileOffset;
        data = binaryReader.ReadBytes(fileSize);
        binaryReader.BaseStream.Position = savePosition;

        assetType = HipTypes.AssetTypeFromCode(code, platform, data);
    }

    protected override string GetMagic() => "AHDR";

    protected override void WriteBody(Game game, Platform platform, List<byte> listBytes)
    {
        HipByte.AddBigEndian(listBytes, assetID);
        foreach (char i in HipTypes.GetCode(assetType).PadRight(4))
            listBytes.Add((byte)i);
        HipByte.AddBigEndian(listBytes, fileOffset);
        HipByte.AddBigEndian(listBytes, fileSize);
        HipByte.AddBigEndian(listBytes, plusValue);
        HipByte.AddBigEndian(listBytes, (int)flags);

        ADBG.SetBytes(game, platform, ref listBytes);
    }
}

public class HipFile
{
    public Section_HIPA HIPA;
    public Section_PACK PACK;
    public Section_DICT DICT;
    public Section_STRM STRM;
    public Section_HIPB HIPB;

    public HipFile(Section_HIPA HIPA, Section_PACK PACK, Section_DICT DICT, Section_STRM STRM, Section_HIPB HIPB)
    {
        this.HIPA = HIPA;
        this.PACK = PACK;
        this.DICT = DICT;
        this.STRM = STRM;
        this.HIPB = HIPB;
    }

    HipFile()
    {
        HIPA = new Section_HIPA();
        PACK = new Section_PACK();
        DICT = new Section_DICT();
        STRM = new Section_STRM();
    }

    public static (HipFile, Game, Platform) FromPath(string fileName)
    {
        Section_ATOC.noAHDR = false;

        HipFile hipFile = new HipFile();
        Game game = Game.Unknown;
        Platform platform = Platform.Unknown;

        byte[] bytes = File.ReadAllBytes(fileName);
        using (MemoryStream ms = new MemoryStream(bytes, false))
        using (BinaryReader binaryReader = new BinaryReader(ms))
            while (binaryReader.BaseStream.Position < binaryReader.BaseStream.Length)
            {
                string currentSection = HipByte.ReadMagic(binaryReader);
                if (currentSection == "HIPA")
                    hipFile.HIPA = new Section_HIPA(binaryReader);
                else if (currentSection == "PACK")
                    hipFile.PACK = new Section_PACK(binaryReader, out game, out platform);
                else if (currentSection == "DICT")
                    hipFile.DICT = new Section_DICT(binaryReader, platform);
                else if (currentSection == "STRM")
                    hipFile.STRM = new Section_STRM(binaryReader);
                else if (currentSection == "HIPB")
                {
                    hipFile.HIPB = new Section_HIPB(binaryReader);
                    if (hipFile.HIPB.VersionMismatch)
                        break;
                }
                else
                    throw new Exception(currentSection);
            }

        DateTimeOffset hipCreated = DateTimeOffset.FromUnixTimeSeconds(hipFile.PACK.PCRT.fileDate);
        if (game == Game.Incredibles && !hipFile.PACK.PCRT.dateString.Contains("/"))
        {
            if (hipCreated.Year == 2004)
                game = Game.Incredibles;
            else if (hipCreated.Year == 2005)
                game = Game.ROTU;
            else if (hipCreated.Year == 2006)
                game = Game.RatProto;
        }
        else if (game == Game.Incredibles)
            game = (hipFile.HIPB == null) ? Game.Unknown : hipFile.HIPB.IncrediblesGame;

        if (game == Game.Scooby)
        {
            if (hipCreated.Year == 2002 && hipCreated.Month == 4)
                platform = Platform.PS2;
            else if (hipCreated.Year == 2002 && hipCreated.Month == 8)
                platform = Platform.GameCube;
            else if (hipCreated.Year == 2003)
                platform = Platform.Xbox;
            else
                platform = Platform.Unknown;
        }

        if (hipFile.HIPB == null)
            hipFile.HIPB = new Section_HIPB();

        return (hipFile, game, platform);
    }

    public static (HipFile, Game, Platform) FromINI(string fileName)
    {
        Section_ATOC.noAHDR = false;

        HipFile hipFile = new HipFile();
        hipFile.SetupFromINI(fileName, out Game game, out Platform platform);
        return (hipFile, game, platform);
    }

    Game GetGame(string v)
    {
        switch (v)
        {
            case "Scooby":
                return Game.Scooby;
            case "BFBB":
                return Game.BFBB;
            case "Incredibles":
                return Game.Incredibles;
            case "ROTU":
                return Game.ROTU;
            case "RatProto":
                return Game.RatProto;
            default:
                throw new Exception("Unknown game");
        }
    }

    void SetupFromINI(string iniFileName, out Game game, out Platform platform)
    {
        string[] INI = File.ReadAllLines(iniFileName);
        char sep = ';';

        List<uint> assetIDlist = new List<uint>();
        Section_LHDR CurrentLHDR = new Section_LHDR();

        game = Game.Unknown;

        foreach (string s in INI)
        {
            if (s.StartsWith("Game="))
            {
                game = GetGame(s.Split('=')[1]);

                if (game == Game.BFBB || game >= Game.Incredibles)
                    PACK.PLAT = new Section_PLAT();
            }
            else if (s.StartsWith("IniVersion"))
            {
                if (Convert.ToInt32(s.Split('=')[1]) != 2)
                    throw new Exception("Unsupported INI file. Please use HipHopFile v0.5.0 to import this INI.");
            }
            else if (s.StartsWith("PACK.PVER"))
            {
                string[] j = s.Split('=')[1].Split(sep);
                PACK.PVER = new Section_PVER(Convert.ToInt32(j[0]), Convert.ToInt32(j[1]), Convert.ToInt32(j[2]));
            }
            else if (s.StartsWith("PACK.PFLG"))
            {
                PACK.PFLG = new Section_PFLG(Convert.ToInt32(s.Split('=')[1]));
            }
            else if (s.StartsWith("PACK.PCRT"))
            {
                string[] j = s.Split('=')[1].Split(sep);
                PACK.PMOD = new Section_PMOD(Convert.ToInt32(j[0]));
                PACK.PCRT = new Section_PCRT(Convert.ToInt32(j[0]), j[1]);
            }
            else if (s.StartsWith("PACK.PLAT.Target="))
            {
                PACK.PLAT.targetPlatform = s.Split('=')[1];
            }
            else if (s.StartsWith("PACK.PLAT.TargetPlatformName"))
            {
                PACK.PLAT.targetPlatformName = s.Split('=')[1];
            }
            else if (s.StartsWith("PACK.PLAT.RegionFormat"))
            {
                PACK.PLAT.regionFormat = s.Split('=')[1];
            }
            else if (s.StartsWith("PACK.PLAT.Language"))
            {
                PACK.PLAT.language = s.Split('=')[1];
            }
            else if (s.StartsWith("PACK.PLAT.TargetGame"))
            {
                PACK.PLAT.targetGame = s.Split('=')[1];
            }
            else if (s.StartsWith("DICT.ATOC.AINF"))
            {
                DICT.ATOC.AINF = new Section_AINF(Convert.ToInt32(s.Split('=')[1]));
            }
            else if (s.StartsWith("DICT.LTOC.LINF"))
            {
                DICT.LTOC.LINF = new Section_LINF(Convert.ToInt32(s.Split('=')[1]));
            }
            else if (s.StartsWith("STRM.DHDR"))
            {
                STRM.DHDR = new Section_DHDR(Convert.ToInt32(s.Split('=')[1]));
            }
            else if (s.StartsWith("LayerType"))
            {
                CurrentLHDR.layerType = Convert.ToInt32(s.Split('=')[1].Split()[0]);
            }
            else if (s.StartsWith("Asset="))
            {
                AddAsset(s.Split('=')[1].Split(sep), ref assetIDlist);
            }
            else if (s.StartsWith("LHDR.LDBG"))
            {
                CurrentLHDR.LDBG = new Section_LDBG(Convert.ToInt32(s.Split('=')[1]));
            }
            else if (s.StartsWith("EndLayer"))
            {
                CurrentLHDR.assetIDlist = assetIDlist;
                DICT.LTOC.LHDRList.Add(CurrentLHDR);

                assetIDlist = new List<uint>();
                CurrentLHDR = new Section_LHDR();
            }
        }

        platform = PACK.PLAT.GetPlatform();

        Dictionary<uint, byte[]> assetDataDictionary = new Dictionary<uint, byte[]>();

        foreach (string f in Directory.GetDirectories(Path.GetDirectoryName(iniFileName)))
            foreach (string i in Directory.GetFiles(f))
            {
                byte[] file = File.ReadAllBytes(i);
                try
                {
                    assetDataDictionary.Add(Convert.ToUInt32(Path.GetFileName(i).Substring(1, 8), 16), file);
                }
                catch
                {
                    Console.WriteLine("Error importing asset " + Path.GetFileName(i));
                }
            }

        List<Section_AHDR> missingAssets = new List<Section_AHDR>();
        foreach (Section_AHDR AHDR in DICT.ATOC.AHDRList)
        {
            if (!assetDataDictionary.ContainsKey(AHDR.assetID))
            {
                Console.WriteLine($"Error: asset with ID [{AHDR.assetID.ToString("X8")}] was not found. The asset will be removed from the archive.");
                missingAssets.Add(AHDR);
            }
            else
                AHDR.data = assetDataDictionary[AHDR.assetID];
        }

        foreach (Section_AHDR missingAsset in missingAssets)
            DICT.ATOC.AHDRList.Remove(missingAsset);
    }

    void AddAsset(string[] j, ref List<uint> assetIDlist)
    {
        uint assetID = Convert.ToUInt32(j[0], 16);
        string assetType = j[1];
        AHDRFlags flags = (AHDRFlags)Convert.ToInt32(j[2]);
        int align = Convert.ToInt32(j[3]);
        string assetName = j[4];
        string assetFileName = j[5];
        int checksum = Convert.ToInt32(j[6], 16);

        assetIDlist.Add(assetID);

        Section_ADBG newADBG = new Section_ADBG(align, assetName, assetFileName, checksum);
        Section_AHDR newAHDR = new Section_AHDR(assetID, assetType, flags, newADBG);

        DICT.ATOC.AHDRList.Add(newAHDR);
    }

    void SetupSTRM(Game game, Platform platform)
    {
        int pcnA = PACK.PCNT?.sizeOfLargestSourceFileAsset ?? 0;
        int pcnL = PACK.PCNT?.sizeOfLargestLayer ?? 0;
        int pcnV = PACK.PCNT?.sizeOfLargestSourceVirtualAsset ?? 0;

        List<byte> temporaryFile = new List<byte>();

        HIPA.SetBytes(game, platform, ref temporaryFile);

        PACK.PCNT = new Section_PCNT(0, 0, 0, 0, 0);
        PACK.SetBytes(game, platform, ref temporaryFile);

        DICT.SetBytes(game, platform, ref temporaryFile);

        STRM.DPAK = new Section_DPAK() { data = new byte[0] };
        STRM.SetBytes(game, platform, ref temporaryFile);

        Dictionary<uint, Section_AHDR> assetDictionary = new Dictionary<uint, Section_AHDR>();
        foreach (Section_AHDR AHDR in DICT.ATOC.AHDRList)
            assetDictionary.Add(AHDR.assetID, AHDR);

        STRM.DPAK.data = BuildStream(game, platform, assetDictionary);

        PACK.PCNT = new Section_PCNT(DICT.ATOC.AHDRList.Count, DICT.LTOC.LHDRList.Count, pcnA, pcnL, pcnV);
    }

    byte[] BuildStream(Game game, Platform platform, Dictionary<uint, Section_AHDR> assetDictionary)
    {
        List<byte> newStream = new List<byte>();

        foreach (Section_LHDR LHDR in DICT.LTOC.LHDRList)
        {
            int startLayer = newStream.Count;
            bool isSRAM = game >= Game.Incredibles ? (LayerType_TSSM)LHDR.layerType == LayerType_TSSM.SRAM : (LayerType_BFBB)LHDR.layerType == LayerType_BFBB.SRAM;

            LHDR.assetIDlist = LHDR.assetIDlist.OrderBy(i => HipTypes.GetCompareValue(DICT.ATOC.GetFromAssetID(i), game)).ToList();

            if (platform == Platform.PS2 && isSRAM)
                LHDR.assetIDlist = LHDR.assetIDlist.GroupBy(g => DICT.ATOC.GetFromAssetID(g).assetType).SelectMany(g => g.OrderBy(i => i)).ToList();

            int finalAlignment = platform == Platform.GameCube ? 0x20 : 0x800;

            for (int i = 0; i < LHDR.assetIDlist.Count; i++)
            {
                if (!assetDictionary.ContainsKey(LHDR.assetIDlist[i]))
                {
                    LHDR.assetIDlist.RemoveAt(i);
                    i--;
                    continue;
                }

                Section_AHDR AHDR = assetDictionary[LHDR.assetIDlist[i]];

                AHDR.fileOffset = newStream.Count + STRM.DPAK.globalRelativeStartOffset;
                AHDR.fileSize = AHDR.data.Length;

                newStream.AddRange(AHDR.data);

                AHDR.plusValue = 0;
                int alignment = 16;

                switch (AHDR.assetType)
                {
                    case AssetType.SoundStream when game >= Game.Incredibles && platform == Platform.GameCube:
                        AHDR.ADBG.alignment = 1;
                        break;
                    case AssetType.SoundStream:
                        alignment = finalAlignment;
                        AHDR.ADBG.alignment = alignment;
                        break;
                    case AssetType.Sound:
                        if (platform == Platform.PS2)
                        {
                            if (game == Game.Scooby)
                                alignment = 2048;
                            AHDR.ADBG.alignment = alignment;
                        }
                        else if (game >= Game.Incredibles && platform == Platform.GameCube)
                            AHDR.ADBG.alignment = 1;
                        else
                        {
                            alignment = finalAlignment;
                            AHDR.ADBG.alignment = alignment;
                        }
                        break;
                    case AssetType.Cutscene:
                        AHDR.ADBG.alignment = finalAlignment;
                        break;
                    case AssetType.TextureStream:
                    case AssetType.BinkVideo:
                    case AssetType.WireframeModel:
                        alignment = finalAlignment;
                        break;
                    case AssetType.JSP:
                    case AssetType.JSPInfo:
                    case AssetType.CutsceneStreamingSound:
                    case AssetType.LightKit:
                        AHDR.ADBG.alignment = alignment;
                        break;
                    case AssetType.BSP:
                        AHDR.ADBG.alignment = -1;
                        break;
                    default:
                        if (HipTypes.IsDyna(AHDR.assetType))
                            AHDR.ADBG.alignment = -1;
                        else
                            AHDR.ADBG.alignment = 0;
                        break;
                }

                if (i < LHDR.assetIDlist.IndexOf(LHDR.assetIDlist.LastOrDefault(aid => assetDictionary[aid].data.Length != 0)))
                {
                    AHDR.plusValue = (alignment - (AHDR.fileSize % alignment)) % alignment;
                    for (int j = 0; j < AHDR.plusValue; j++)
                        newStream.Add(0x33);
                }

                if (isSRAM && platform == Platform.PS2 && game != Game.Scooby)
                {
                    if (AHDR.assetID == LHDR.assetIDlist.LastOrDefault(aid => assetDictionary[aid].assetType == AssetType.Sound) && AHDR.assetID != LHDR.assetIDlist.Last())
                    {
                        AHDR.plusValue = (2048 - ((newStream.Count - startLayer) % 2048)) % 2048;
                        for (int j = 0; j < AHDR.plusValue; j++)
                            newStream.Add(0x33);
                    }
                }
            }

            while ((newStream.Count + STRM.DPAK.globalRelativeStartOffset) % finalAlignment != 0)
                newStream.Add(0x33);
        }

        return newStream.ToArray();
    }

    public byte[] ToBytes(Game game, Platform platform)
    {
        DICT.ATOC.SortAHDRList();

        SetupSTRM(game, platform);

        List<byte> list = new List<byte>();

        HIPA.SetBytes(game, platform, ref list);
        PACK.SetBytes(game, platform, ref list);
        DICT.SetBytes(game, platform, ref list);
        STRM.SetBytes(game, platform, ref list);
        if (HIPB != null)
            HIPB.SetBytes(game, platform, ref list);

        return list.ToArray();
    }

    public void ToIni(Game game, string unpackFolder, bool multiFolder, bool alphabetical)
    {
        Directory.CreateDirectory(unpackFolder);

        string fileName = Path.Combine(unpackFolder, "Settings.ini");

        StreamWriter INIWriter = new StreamWriter(new FileStream(fileName, FileMode.Create));
        INIWriter.WriteLine("Game=" + game.ToString());
        INIWriter.WriteLine("IniVersion=2");

        INIWriter.WriteLine("PACK.PVER=" + PACK.PVER.subVersion.ToString() + ";" + PACK.PVER.clientVersion.ToString() + ";" + PACK.PVER.compatible.ToString());
        INIWriter.WriteLine("PACK.PFLG=" + PACK.PFLG.flags.ToString());
        INIWriter.WriteLine("PACK.PCRT=" + PACK.PCRT.fileDate.ToString() + ";" + PACK.PCRT.dateString);
        if (game == Game.BFBB || game >= Game.Incredibles)
        {
            INIWriter.WriteLine("PACK.PLAT.Target=" + PACK.PLAT.targetPlatform);
            INIWriter.WriteLine("PACK.PLAT.RegionFormat=" + PACK.PLAT.regionFormat);
            INIWriter.WriteLine("PACK.PLAT.Language=" + PACK.PLAT.language);
            INIWriter.WriteLine("PACK.PLAT.TargetGame=" + PACK.PLAT.targetGame);
            if (game == Game.BFBB)
                INIWriter.WriteLine("PACK.PLAT.TargetPlatformName=" + PACK.PLAT.targetPlatformName);
        }
        else if (game != Game.Scooby)
            throw new Exception("Unknown game");

        INIWriter.WriteLine();

        ExtractAssetsToFolders(unpackFolder, multiFolder);

        INIWriter.WriteLine("DICT.ATOC.AINF=" + DICT.ATOC.AINF.value.ToString());
        INIWriter.WriteLine("DICT.LTOC.LINF=" + DICT.LTOC.LINF.value.ToString());
        INIWriter.WriteLine();

        Dictionary<uint, Section_AHDR> ahdrDictionary = new Dictionary<uint, Section_AHDR>();

        foreach (Section_AHDR AHDR in DICT.ATOC.AHDRList)
            ahdrDictionary.Add(AHDR.assetID, AHDR);

        foreach (Section_LHDR LHDR in DICT.LTOC.LHDRList)
        {
            if (game >= Game.Incredibles)
                INIWriter.WriteLine("LayerType=" + LHDR.layerType + " " + ((LayerType_TSSM)LHDR.layerType).ToString());
            else
                INIWriter.WriteLine("LayerType=" + LHDR.layerType + " " + ((LayerType_BFBB)LHDR.layerType).ToString());

            if (LHDR.assetIDlist.Count == 0)
                INIWriter.WriteLine("AssetAmount=0");
            INIWriter.WriteLine("LHDR.LDBG=" + LHDR.LDBG.value.ToString());

            List<Section_AHDR> ahdrList = new List<Section_AHDR>(LHDR.assetIDlist.Count);
            foreach (uint j in LHDR.assetIDlist)
                ahdrList.Add(ahdrDictionary[j]);

            if (alphabetical)
                ahdrList = ahdrList.OrderBy(ahdr => ahdr.ADBG.assetName).ToList();

            foreach (Section_AHDR AHDR in ahdrList)
            {
                string assetToString = AHDR.assetID.ToString("X8") + ";" + AHDR.assetType.ToString() + ";" + ((int)AHDR.flags).ToString() + ";" + AHDR.ADBG.alignment.ToString() + ";" + AHDR.ADBG.assetName.Replace(';', '_') + ";" + AHDR.ADBG.assetFileName + ";" + AHDR.ADBG.checksum.ToString("X8");
                INIWriter.WriteLine("Asset=" + assetToString);
            }

            INIWriter.WriteLine("EndLayer");

            INIWriter.WriteLine();
        }

        INIWriter.WriteLine("STRM.DHDR=" + STRM.DHDR.value.ToString());
        INIWriter.WriteLine();

        INIWriter.Close();
    }

    void ExtractAssetsToFolders(string unpackFolder, bool multiFolder)
    {
        foreach (Section_AHDR AHDR in DICT.ATOC.AHDRList)
        {
            string directoryToUnpack = Path.Combine(unpackFolder, "files");

            if (multiFolder)
                directoryToUnpack = Path.Combine(unpackFolder, AHDR.assetType.ToString());

            if (!Directory.Exists(directoryToUnpack))
                Directory.CreateDirectory(directoryToUnpack);

            string assetFileName = "[" + AHDR.assetID.ToString("X8") + "] " + AHDR.ADBG.assetName;

            foreach (char c in Path.GetInvalidFileNameChars())
                assetFileName = assetFileName.Replace(c, '_');

            File.WriteAllBytes(Path.Combine(directoryToUnpack, assetFileName), AHDR.data);
        }
    }
}