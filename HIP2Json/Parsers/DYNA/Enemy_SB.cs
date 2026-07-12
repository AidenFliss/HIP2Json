using System;
using System.IO;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class Enemy_SBParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version, string dynaType)
    {
        xBaseAsset baseAsset = ReadBaseAsset(br);
        xEntAsset entAsset = ReadEntAsset(br);

        object enemyData = dynaType switch
        {
            "Enemy:SB:BucketOTron" => new Enemy_SB_BucketOTron
            {
                aid_spawnGroup = ReadUInt32BE(br),
                spawnMode = (en_spawnmode)ReadInt32BE(br),
                spawnDelay = ReadFloatBE(br),
                flg_buckass = ReadInt32BE(br),
                maxSpawn = ReadInt32BE(br)
            },

            "Enemy:SB:CastNCrew" => new Enemy_SB_CastNCrew
            {
                
            },

            "Enemy:SB:Critter" => new Enemy_SB_Critter
            {
                mvptID = ReadUInt32BE(br),
                unknown = ReadInt32BE(br)
            },

            "Enemy:SB:Dennis" => new Enemy_SB_Dennis
            {
                movePointID = ReadUInt32BE(br),
                movePointGroupID = ReadUInt32BE(br),
                unknown1 = ReadInt32BE(br),
                unknown2 = ReadInt32BE(br)
            },

            "Enemy:SB:FrogFish" => new Enemy_SB_FrogFish
            {
                unknown = ReadUInt32BE(br)
            },

            "Enemy:SB:Mindy" => new Enemy_SB_Mindy
            {
                taskBoxID = ReadUInt32BE(br),
                clamOpenDistance = ReadFloatBE(br),
                clamCloseDistance = ReadFloatBE(br),
                textBoxID = ReadUInt32BE(br),
                primaryCharacter = (MindyCharacter)ReadUInt32BE(br),
                secondaryTaskBoxID = ReadUInt32BE(br)
            },

            "Enemy:SB:Neptune" => new Enemy_SB_Neptune
            {
                unknown1 = ReadUInt32BE(br),
                unknown2 = ReadUInt32BE(br)
            },

            "Enemy:SB:Standard" => new Enemy_SB_Standard
            {
                mvptID = ReadUInt32BE(br),
                mvptGroupID = ReadUInt32BE(br),
                enemyFlags = (EnemyFlags)ReadUInt32BE(br),
                unknown1 = ReadInt32BE(br),
                unknown2 = ReadInt32BE(br),
                unknown3 = ReadInt32BE(br),
                unknown4 = ReadInt32BE(br)
            },

            "Enemy:SB:SupplyCrate" => new Enemy_SB_SupplyCrate
            {
                mvptID = ReadUInt32BE(br)
            },

            "Enemy:SB:Turret" => new Enemy_SB_Turret
            {
                rotation = ReadFloatBE(br),
                unknown1 = ReadInt32BE(br),
                targetPlayer = ReadInt32BE(br),
                unknown2 = ReadInt32BE(br),
                unknown3 = ReadInt32BE(br)
            },

            _ => throw new InvalidDataException($"Unknown enemy type '{dynaType}'.")
        };

        return new Enemy_SB
        {
            baseAsset = baseAsset,
            entityAsset = entAsset,
            enemyData = enemyData
        };
    }

    public override byte[] Serialize(object obj, short version, string dynaType)
    {
        Enemy_SB enemySB = (Enemy_SB)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteBaseAsset(bw, enemySB.baseAsset);
        WriteEntAsset(bw, enemySB.entityAsset);

        switch (dynaType)
        {
            case "Enemy:SB:BucketOTron":
            {
                var e = (Enemy_SB_BucketOTron)enemySB.enemyData;
                WriteUInt32BE(bw, e.aid_spawnGroup);
                WriteInt32BE(bw, (int)e.spawnMode);
                WriteFloatBE(bw, e.spawnDelay);
                WriteInt32BE(bw, e.flg_buckass);
                WriteInt32BE(bw, e.maxSpawn);
                break;
            }

            case "Enemy:SB:CastNCrew":
            {
                var e = (Enemy_SB_CastNCrew)enemySB.enemyData;
                break;
            }

            case "Enemy:SB:Critter":
            {
                var e = (Enemy_SB_Critter)enemySB.enemyData;
                WriteUInt32BE(bw, e.mvptID);
                WriteInt32BE(bw, e.unknown);
                break;
            }

            case "Enemy:SB:Dennis":
            {
                var e = (Enemy_SB_Dennis)enemySB.enemyData;
                WriteUInt32BE(bw, e.movePointID);
                WriteUInt32BE(bw, e.movePointGroupID);
                WriteInt32BE(bw, e.unknown1);
                WriteInt32BE(bw, e.unknown2);
                break;
            }

            case "Enemy:SB:FrogFish":
            {
                var e = (Enemy_SB_FrogFish)enemySB.enemyData;
                WriteUInt32BE(bw, e.unknown);
                break;
            }

            case "Enemy:SB:Mindy":
            {
                var e = (Enemy_SB_Mindy)enemySB.enemyData;
                WriteUInt32BE(bw, e.taskBoxID);
                WriteFloatBE(bw, e.clamOpenDistance);
                WriteFloatBE(bw, e.clamCloseDistance);
                WriteUInt32BE(bw, e.textBoxID);
                WriteUInt32BE(bw, (uint)e.primaryCharacter);
                WriteUInt32BE(bw, e.secondaryTaskBoxID);
                break;
            }

            case "Enemy:SB:Neptune":
            {
                var e = (Enemy_SB_Neptune)enemySB.enemyData;
                WriteUInt32BE(bw, e.unknown1);
                WriteUInt32BE(bw, e.unknown2);
                break;
            }

            case "Enemy:SB:Standard":
            {
                var e = (Enemy_SB_Standard)enemySB.enemyData;
                WriteUInt32BE(bw, e.mvptID);
                WriteUInt32BE(bw, e.mvptGroupID);
                WriteUInt32BE(bw, (uint)e.enemyFlags);
                WriteInt32BE(bw, e.unknown1);
                WriteInt32BE(bw, e.unknown2);
                WriteInt32BE(bw, e.unknown3);
                WriteInt32BE(bw, e.unknown4);
                break;
            }

            case "Enemy:SB:SupplyCrate":
            {
                var e = (Enemy_SB_SupplyCrate)enemySB.enemyData;
                WriteUInt32BE(bw, e.mvptID);
                break;
            }

            case "Enemy:SB:Turret":
            {
                var e = (Enemy_SB_Turret)enemySB.enemyData;
                WriteFloatBE(bw, e.rotation);
                WriteInt32BE(bw, e.unknown1);
                WriteInt32BE(bw, e.targetPlayer);
                WriteInt32BE(bw, e.unknown2);
                WriteInt32BE(bw, e.unknown3);
                break;
            }

            default:
                throw new InvalidDataException($"Unknown enemy type '{dynaType}'.");
        }

        return ms.ToArray();
    }

    public override string GetFolderName() { return "Enemy"; }
}

public class Enemy_SB
{
    public xBaseAsset baseAsset { get; set; }
    public xEntAsset entityAsset { get; set; }
    public object enemyData { get; set; }
    
}

public class Enemy_SB_BucketOTron
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint aid_spawnGroup { get; set; }
    public en_spawnmode spawnMode { get; set; }
    public float spawnDelay { get; set; }
    public int flg_buckass { get; set; }
    public int maxSpawn { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum en_spawnmode : int
{
    NME_SPAWNMODE_CONTINUOUS = 0,
    NME_SPAWNMODE_WAVES = 1,
    NME_SPAWNMODE_AMBUSHWAVE = 2,
    NME_SPAWNMODE_AMBUSHCONT = 3,
    NME_SPAWNMODE_NOMORE = 4,
    NME_SPAWNMODE_FORCE = 5
}

public class Enemy_SB_CastNCrew
{
    
}

public class Enemy_SB_Critter
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint mvptID { get; set; }
    public int unknown { get; set; }
}

public class Enemy_SB_Dennis
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint movePointID { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint movePointGroupID { get; set; }
    public int unknown1 { get; set; }
    public int unknown2 { get; set; }
}

public class Enemy_SB_FrogFish
{
    public uint unknown { get; set; }
}

public class Enemy_SB_Mindy
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint taskBoxID { get; set; }
    public float clamOpenDistance { get; set; }
    public float clamCloseDistance { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint textBoxID { get; set; }
    public MindyCharacter primaryCharacter { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint secondaryTaskBoxID { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MindyCharacter : uint
{
    Spongebob = 0,
    Patrick = 1
}

public class Enemy_SB_Neptune
{
    public uint unknown1 { get; set; }
    public uint unknown2 { get; set; }
}

public class Enemy_SB_Standard
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint mvptID { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint mvptGroupID { get; set; }
    public EnemyFlags enemyFlags { get; set; }
    public int unknown1 { get; set; }
    public int unknown2 { get; set; }
    public int unknown3 { get; set; }
    public int unknown4 { get; set; }
}

[Flags]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EnemyFlags : uint
{
    None = 0,
    PrepareForScare = 0x01,
    Unknown02 = 0x02,
    WalkOnPLATs = 0x04,
    WalkOnSIMPs = 0x08,
}

public class Enemy_SB_SupplyCrate
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint mvptID { get; set; }
}

public class Enemy_SB_Turret
{
    public float rotation { get; set; }
    public int unknown1 { get; set; }
    public int targetPlayer { get; set; }
    public int unknown2 { get; set; }
    public int unknown3 { get; set; }
}