using System.Text.Json.Serialization;

namespace HIP2Json;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Enemy_SB_BucketOTron), "Enemy:SB:BucketOTron")]
[JsonDerivedType(typeof(Enemy_SB_CastNCrew), "Enemy:SB:CastNCrew")]
[JsonDerivedType(typeof(Enemy_SB_Critter), "Enemy:SB:Critter")]
[JsonDerivedType(typeof(Enemy_SB_Dennis), "Enemy:SB:Dennis")]
[JsonDerivedType(typeof(Enemy_SB_FrogFish), "Enemy:SB:FrogFish")]
[JsonDerivedType(typeof(Enemy_SB_Mindy), "Enemy:SB:Mindy")]
[JsonDerivedType(typeof(Enemy_SB_Neptune), "Enemy:SB:Neptune")]
[JsonDerivedType(typeof(Enemy_SB_Standard), "Enemy:SB:Standard")]
[JsonDerivedType(typeof(Enemy_SB_SupplyCrate), "Enemy:SB:SupplyCrate")]
[JsonDerivedType(typeof(Enemy_SB_Turret), "Enemy:SB:Turret")]
public abstract class Enemy_SBData { }

public class Enemy_SB_BucketOTron : Enemy_SBData
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint aid_spawnGroup { get; set; }
    public en_spawnmode spawnMode { get; set; }
    public float spawnDelay { get; set; }
    public int flg_buckass { get; set; }
    public int maxSpawn { get; set; }
}

public class Enemy_SB_CastNCrew : Enemy_SBData { }

public class Enemy_SB_Critter : Enemy_SBData
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint mvptID { get; set; }
    public int unknown { get; set; }
}

public class Enemy_SB_Dennis : Enemy_SBData
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint movePointID { get; set; }

    [JsonConverter(typeof(AssetIDConverter))]
    public uint movePointGroupID { get; set; }
    public int unknown1 { get; set; }
    public int unknown2 { get; set; }
}

public class Enemy_SB_FrogFish : Enemy_SBData
{
    public uint unknown { get; set; }
}

public class Enemy_SB_Mindy : Enemy_SBData
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

public class Enemy_SB_Neptune : Enemy_SBData
{
    public uint unknown1 { get; set; }
    public uint unknown2 { get; set; }
}

public class Enemy_SB_Standard : Enemy_SBData
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

public class Enemy_SB_SupplyCrate : Enemy_SBData
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint mvptID { get; set; }
}

public class Enemy_SB_Turret : Enemy_SBData
{
    public float rotation { get; set; }
    public int unknown1 { get; set; }
    public int targetPlayer { get; set; }
    public int unknown2 { get; set; }
    public int unknown3 { get; set; }
}

public class Enemy_SB
{
    public xBaseAsset baseAsset { get; set; }
    public xEntAsset entityAsset { get; set; }

    public Enemy_SBData enemyData { get; set; }
}
