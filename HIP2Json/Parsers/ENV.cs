using System.IO;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class ENVParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        var env = new ENV
        {
            bspAssetID = ReadUInt32BE(br),
            startCameraAssetID = ReadUInt32BE(br),
            climateFlags = ReadInt32BE(br),
            climateStrengthMin = ReadFloatBE(br),
            climateStrengthMax = ReadFloatBE(br),
            bspLightKit = ReadUInt32BE(br),
            objectLightKit = ReadUInt32BE(br),
            flags = ReadInt32BE(br),
            bspCollisionAssetID = ReadUInt32BE(br),
            bspFXAssetID = ReadUInt32BE(br),
            bspCameraAssetID = ReadUInt32BE(br),
            bspMapperID = ReadUInt32BE(br),
            bspMapperCollisionID = ReadUInt32BE(br),
            bspMapperFXID = ReadUInt32BE(br),
            loldHeight = ReadFloatLE(br)
        };

        if (Program.CurrentGame != GameType.BFBB)
        {
            env.minBounds = ReadVector3BE(br);
            env.maxBounds = ReadVector3BE(br);
        }

        return env;
    }

    public override object Serialize(object obj)
    {
        ENV env = (ENV)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, env.bspAssetID);
        WriteUInt32BE(bw, env.startCameraAssetID);
        WriteInt32BE(bw, env.climateFlags);
        WriteFloatBE(bw, env.climateStrengthMin);
        WriteFloatBE(bw, env.climateStrengthMax);
        WriteUInt32BE(bw, env.bspLightKit);
        WriteUInt32BE(bw, env.objectLightKit);
        WriteInt32BE(bw, env.flags);
        WriteUInt32BE(bw, env.bspCollisionAssetID);
        WriteUInt32BE(bw, env.bspFXAssetID);
        WriteUInt32BE(bw, env.bspCameraAssetID);
        WriteUInt32BE(bw, env.bspMapperID);
        WriteUInt32BE(bw, env.bspMapperCollisionID);
        WriteUInt32BE(bw, env.bspMapperFXID);
        WriteFloatLE(bw, env.loldHeight);

        if (Program.CurrentGame != GameType.BFBB)
        {
            WriteVector3BE(bw, env.minBounds);
            WriteVector3BE(bw, env.maxBounds);
        }

        return ms.ToArray();
    }
}

public class ENV
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint bspAssetID { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint startCameraAssetID { get; set; }
    public int climateFlags { get; set; }
    public float climateStrengthMin { get; set; }
    public float climateStrengthMax { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint bspLightKit { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint objectLightKit { get; set; }
    public int flags { get; set; } //pad in bfbb, flags in motion video game
    [JsonConverter(typeof(AssetIDConverter))]
    public uint bspCollisionAssetID { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint bspFXAssetID { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint bspCameraAssetID { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint bspMapperID { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint bspMapperCollisionID { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint bspMapperFXID { get; set; }
    public float loldHeight { get; set; }
    public xVec3 minBounds { get; set; }
    public xVec3 maxBounds { get; set; }
}