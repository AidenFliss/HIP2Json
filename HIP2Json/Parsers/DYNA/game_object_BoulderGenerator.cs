using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class game_object_BoulderGeneratorParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version, string dynaType)
    {
        return new game_object_BoulderGenerator
        {
            objct = ReadUInt32BE(br),
            offset = ReadVector3BE(br),
            offsetRand = ReadFloatBE(br),
            initvel = ReadVector3BE(br),
            velAngleRand = ReadFloatBE(br),
            velMagRand = ReadFloatBE(br),
            initaxis = ReadVector3BE(br),
            angvel = ReadFloatBE(br)
        };
    }

    public override byte[] Serialize(object obj, short version, string dynaType)
    {
        game_object_BoulderGenerator boulderGenerator = (game_object_BoulderGenerator)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, boulderGenerator.objct);
        WriteVector3BE(bw, boulderGenerator.offset);
        WriteFloatBE(bw, boulderGenerator.offsetRand);
        WriteVector3BE(bw, boulderGenerator.initvel);
        WriteFloatBE(bw, boulderGenerator.velAngleRand);
        WriteFloatBE(bw, boulderGenerator.velMagRand);
        WriteVector3BE(bw, boulderGenerator.initaxis);
        WriteFloatBE(bw, boulderGenerator.angvel);

        return ms.ToArray();
    }

    public override string GetFolderName() { return "BoulderGenerator"; }
}

public class game_object_BoulderGenerator
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint objct { get; set; }
    public xVec3 offset { get; set; }
    public float offsetRand { get; set; }
    public xVec3 initvel { get; set; }
    public float velAngleRand { get; set; }
    public float velMagRand { get; set; }
    public xVec3 initaxis { get; set; }
    public float angvel { get; set; }
}