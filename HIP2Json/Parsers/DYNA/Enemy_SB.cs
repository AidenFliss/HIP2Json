using System.IO;

namespace HIP2Json;

public sealed class Enemy_SBParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version, string dynaType)
    {
        xBaseAsset baseAsset = ReadBaseAsset(br);
        xEntAsset entAsset = ReadEntAsset(br);

        Enemy_SBData enemyData = dynaType switch
        {
            "Enemy:SB:BucketOTron" => new Enemy_SB_BucketOTron
            {
                aid_spawnGroup = ReadUInt32BE(br),
                spawnMode = (en_spawnmode)ReadInt32BE(br),
                spawnDelay = ReadFloatBE(br),
                flg_buckass = ReadInt32BE(br),
                maxSpawn = ReadInt32BE(br),
            },
            "Enemy:SB:CastNCrew" => new Enemy_SB_CastNCrew(),
            "Enemy:SB:Critter" => new Enemy_SB_Critter { mvptID = ReadUInt32BE(br), unknown = ReadInt32BE(br) },
            "Enemy:SB:Dennis" => new Enemy_SB_Dennis
            {
                movePointID = ReadUInt32BE(br),
                movePointGroupID = ReadUInt32BE(br),
                unknown1 = ReadInt32BE(br),
                unknown2 = ReadInt32BE(br),
            },
            "Enemy:SB:FrogFish" => new Enemy_SB_FrogFish { unknown = ReadUInt32BE(br) },
            "Enemy:SB:Mindy" => new Enemy_SB_Mindy
            {
                taskBoxID = ReadUInt32BE(br),
                clamOpenDistance = ReadFloatBE(br),
                clamCloseDistance = ReadFloatBE(br),
                textBoxID = ReadUInt32BE(br),
                primaryCharacter = (MindyCharacter)ReadUInt32BE(br),
                secondaryTaskBoxID = ReadUInt32BE(br),
            },
            "Enemy:SB:Neptune" => new Enemy_SB_Neptune { unknown1 = ReadUInt32BE(br), unknown2 = ReadUInt32BE(br) },
            "Enemy:SB:Standard" => new Enemy_SB_Standard
            {
                mvptID = ReadUInt32BE(br),
                mvptGroupID = ReadUInt32BE(br),
                enemyFlags = (EnemyFlags)ReadUInt32BE(br),
                unknown1 = ReadInt32BE(br),
                unknown2 = ReadInt32BE(br),
                unknown3 = ReadInt32BE(br),
                unknown4 = ReadInt32BE(br),
            },
            "Enemy:SB:SupplyCrate" => new Enemy_SB_SupplyCrate { mvptID = ReadUInt32BE(br) },
            "Enemy:SB:Turret" => new Enemy_SB_Turret
            {
                rotation = ReadFloatBE(br),
                unknown1 = ReadInt32BE(br),
                targetPlayer = ReadInt32BE(br),
                unknown2 = ReadInt32BE(br),
                unknown3 = ReadInt32BE(br),
            },
            _ => throw new InvalidDataException($"Unknown enemy type '{dynaType}'."),
        };

        return new Enemy_SB
        {
            baseAsset = baseAsset,
            entityAsset = entAsset,
            enemyData = enemyData,
        };
    }

    public override byte[] Serialize(object obj, short version, string dynaType)
    {
        Enemy_SB enemySB = (Enemy_SB)obj;

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteBaseAsset(bw, enemySB.baseAsset);
        WriteEntAsset(bw, enemySB.entityAsset);

        if (enemySB.enemyData != null)
        {
            WriteEnemyData(bw, enemySB.enemyData);
        }

        return ms.ToArray();
    }

    private void WriteEnemyData(BinaryWriter bw, Enemy_SBData data)
    {
        switch (data)
        {
            case Enemy_SB_BucketOTron e:
                WriteUInt32BE(bw, e.aid_spawnGroup);
                WriteInt32BE(bw, (int)e.spawnMode);
                WriteFloatBE(bw, e.spawnDelay);
                WriteInt32BE(bw, e.flg_buckass);
                WriteInt32BE(bw, e.maxSpawn);
                break;

            case Enemy_SB_CastNCrew:
                break;

            case Enemy_SB_Critter e:
                WriteUInt32BE(bw, e.mvptID);
                WriteInt32BE(bw, e.unknown);
                break;

            case Enemy_SB_Dennis e:
                WriteUInt32BE(bw, e.movePointID);
                WriteUInt32BE(bw, e.movePointGroupID);
                WriteInt32BE(bw, e.unknown1);
                WriteInt32BE(bw, e.unknown2);
                break;

            case Enemy_SB_FrogFish e:
                WriteUInt32BE(bw, e.unknown);
                break;

            case Enemy_SB_Mindy e:
                WriteUInt32BE(bw, e.taskBoxID);
                WriteFloatBE(bw, e.clamOpenDistance);
                WriteFloatBE(bw, e.clamCloseDistance);
                WriteUInt32BE(bw, e.textBoxID);
                WriteUInt32BE(bw, (uint)e.primaryCharacter);
                WriteUInt32BE(bw, e.secondaryTaskBoxID);
                break;

            case Enemy_SB_Neptune e:
                WriteUInt32BE(bw, e.unknown1);
                WriteUInt32BE(bw, e.unknown2);
                break;

            case Enemy_SB_Standard e:
                WriteUInt32BE(bw, e.mvptID);
                WriteUInt32BE(bw, e.mvptGroupID);
                WriteUInt32BE(bw, (uint)e.enemyFlags);
                WriteInt32BE(bw, e.unknown1);
                WriteInt32BE(bw, e.unknown2);
                WriteInt32BE(bw, e.unknown3);
                WriteInt32BE(bw, e.unknown4);
                break;

            case Enemy_SB_SupplyCrate e:
                WriteUInt32BE(bw, e.mvptID);
                break;

            case Enemy_SB_Turret e:
                WriteFloatBE(bw, e.rotation);
                WriteInt32BE(bw, e.unknown1);
                WriteInt32BE(bw, e.targetPlayer);
                WriteInt32BE(bw, e.unknown2);
                WriteInt32BE(bw, e.unknown3);
                break;
        }
    }

    public override string GetFolderName() => "Enemy";
}
