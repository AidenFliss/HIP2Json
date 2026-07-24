using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class game_object_NPCSettingsParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version, string dynaType)
    {
        BasisType basisType = (BasisType)ReadUInt32BE(br);
        byte allowDetect = ReadByte(br);
        byte allowPatrol = ReadByte(br);
        byte allowWander = ReadByte(br);
        byte reduceCollide = ReadByte(br);
        byte useNavSplines = ReadByte(br);
        br.ReadBytes(3);
        byte allowChase = ReadByte(br);
        byte allowAttack = ReadByte(br);
        byte assumeLOS = ReadByte(br);
        byte assumeFOV = ReadByte(br);
        DuploWaveMode duploWaveMode = (DuploWaveMode)ReadUInt32BE(br);
        float duploSpawnDelay = ReadFloatBE(br);
        int duploSpawnLifeMax = ReadInt32BE(br);

        return new game_object_NPCSettings
        {
            basisType = basisType,
            allowDetect = allowDetect,
            allowPatrol = allowPatrol,
            allowWander = allowWander,
            reduceCollide = reduceCollide,
            useNavSplines = useNavSplines,
            allowChase = allowChase,
            allowAttack = allowAttack,
            assumeLOS = assumeLOS,
            assumeFOV = assumeFOV,
            duploWaveMode = duploWaveMode,
            duploSpawnDelay = duploSpawnDelay,
            duploSpawnLifeMax = duploSpawnLifeMax
        };
    }

    public override byte[] Serialize(object obj, short version, string dynaType)
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
        bw.Write(new byte[3]);
        WriteByte(bw, npcSettings.allowChase);
        WriteByte(bw, npcSettings.allowAttack);
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