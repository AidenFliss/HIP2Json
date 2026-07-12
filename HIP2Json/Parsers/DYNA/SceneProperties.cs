using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class ScenePropertiesParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version, string dynaType)
    {
        int idle03ExtraCount = ReadInt32BE(br);
        uint idle03Extras = ReadUInt32BE(br);
        int idle04ExtraCount = ReadInt32BE(br);
        uint idle04Extras = ReadUInt32BE(br);
        byte bombCount = ReadByte(br);
        byte extraIdleDelay = ReadByte(br);
        byte hdrGlow = ReadByte(br);
        byte hdrDarken = ReadByte(br);
        uint musicID = ReadUInt32BE(br);
        uint flags = ReadUInt32BE(br);
        float waterTileWidth = ReadFloatBE(br);
        float lodFadeDistance = ReadFloatBE(br);

        uint[] padding = Enumerable.Range(0, 4)
                .Select(_ => ReadUInt32BE(br))
                .ToArray();

        return new SceneProperties
        {
            idle03ExtraCount = idle03ExtraCount,
            idle03Extras = idle03Extras,
            idle04ExtraCount = idle04ExtraCount,
            idle04Extras = idle04Extras,
            bombCount = bombCount,
            extraIdleDelay = extraIdleDelay,
            hdrGlow = hdrGlow,
            hdrDarken = hdrDarken,
            musicID = musicID,
            flags = flags,
            waterTileWidth = waterTileWidth,
            lodFadeDistance = lodFadeDistance,
            padding = padding
        };
    }

    public override byte[] Serialize(object obj, short version, string dynaType)
    {
        SceneProperties sceneProperties = (SceneProperties)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteInt32BE(bw, sceneProperties.idle03ExtraCount);
        WriteUInt32BE(bw, sceneProperties.idle03Extras);
        WriteInt32BE(bw, sceneProperties.idle04ExtraCount);
        WriteUInt32BE(bw, sceneProperties.idle04Extras);
        WriteByte(bw, sceneProperties.bombCount);
        WriteByte(bw, sceneProperties.extraIdleDelay);
        WriteByte(bw, sceneProperties.hdrGlow);
        WriteByte(bw, sceneProperties.hdrDarken);
        WriteUInt32BE(bw, sceneProperties.musicID);
        WriteUInt32BE(bw, sceneProperties.flags);
        WriteFloatBE(bw, sceneProperties.waterTileWidth);
        WriteFloatBE(bw, sceneProperties.lodFadeDistance);

        foreach (var pad in sceneProperties.padding)
            WriteUInt32BE(bw, pad);
        
        return ms.ToArray();
    }

    public override string GetFolderName() { return "SceneProperties"; }
}

public class SceneProperties
{
    public int idle03ExtraCount { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint idle03Extras { get; set; }
    public int idle04ExtraCount { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint idle04Extras { get; set; }
    public byte bombCount { get; set; }
    public byte extraIdleDelay { get; set; }
    public byte hdrGlow { get; set; }
    public byte hdrDarken { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint musicID { get; set; }
    public uint flags { get; set; }
    public float waterTileWidth { get; set; }
    public float lodFadeDistance { get; set; }
    public uint[] padding { get; set; }
}