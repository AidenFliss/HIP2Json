using System.IO;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class game_object_NPCSettingsParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version)
    {
        return new game_object_NPCSettings
        {
            basisType = (BasisType)ReadUInt32BE(br),
            allowDetect = ReadByte(br),
            allowPatrol = ReadByte(br),
            allowWander = ReadByte(br),
            reduceCollide = ReadByte(br),
            useNavSplines = ReadByte(br),
            pad0 = ReadByte(br),
            pad1 = ReadByte(br),
            pad2 = ReadByte(br),
            allowChase = ReadByte(br),
            allowAttack = ReadByte(br),
            assumeLOS = ReadByte(br),
            assumeFOV = ReadByte(br),
            duploWaveMode = (DuploWaveMode)ReadUInt32BE(br),
            duploSpawnDelay = ReadFloatBE(br),
            duploSpawnLifeMax = ReadInt32BE(br)
        };
    }

    public override byte[] Serialize(object obj, short version)
    {
        game_object_NPCSettings npcSettings = (game_object_NPCSettings)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, (uint)npcSettings.basisType);
        WriteByte(bw, npcSettings.allowDetect);
        WriteByte(bw, npcSettings.allowPatrol);
        WriteByte(bw, npcSettings.allowWander);
        WriteByte(bw, npcSettings.reduceCollide);
        WriteByte(bw, npcSettings.useNavSplines);
        WriteByte(bw, npcSettings.pad0);
        WriteByte(bw, npcSettings.pad1);
        WriteByte(bw, npcSettings.pad2);
        WriteByte(bw, npcSettings.allowChase);
        WriteByte(bw, npcSettings.assumeLOS);
        WriteByte(bw, npcSettings.assumeFOV);
        WriteUInt32BE(bw, (uint)npcSettings.duploWaveMode);
        WriteFloatBE(bw, npcSettings.duploSpawnDelay);
        WriteInt32BE(bw, npcSettings.duploSpawnLifeMax);

        return ms.ToArray();
    }

    public override string GetFolderName() { return "NPCSettingsObject"; }
}

public class game_object_NPCSettings
{
    public BasisType basisType { get; set; }
    public byte allowDetect { get; set; }
    public byte allowPatrol { get; set; }
    public byte allowWander { get; set; }
    public byte reduceCollide { get; set; }
    public byte useNavSplines { get; set; }
    public byte pad0 { get; set; }
    public byte pad1 { get; set; }
    public byte pad2 { get; set; }
    public byte allowChase { get; set; }
    public byte allowAttack { get; set; }
    public byte assumeLOS { get; set; }
    public byte assumeFOV { get; set; }
    public DuploWaveMode duploWaveMode { get; set; }
    public float duploSpawnDelay { get; set; }
    public int duploSpawnLifeMax { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BasisType : uint
{
    None = 0,
    EvilRobot = 1,
    FriendlyRobot = 2,
    LovingCitizen = 3,
    GrumpyCitizen = 4
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DuploWaveMode : uint
{
    Continuous = 0,
    Discreet = 1
}