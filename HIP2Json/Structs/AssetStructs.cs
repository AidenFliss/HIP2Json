using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HIP2Json;

public struct xBaseAsset
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint id;
    public string baseType;
    public byte linkCount;
    public BaseFlags baseFlags;

    public override string ToString() => $"id: {id}, baseType: {baseType}, linkCount: {linkCount}, baseFlags: {baseFlags}";
}

public struct xEntAsset
{
    public EntFlags flags;
    public byte subtype;
    public byte pflags;
    public EntFlagsMore moreFlags;
    public uint surfaceID;
    public xVec3 ang,
        pos,
        scale;
    public float redMult,
        greenMult,
        blueMult,
        seeThru,
        seeThruSpeed;

    [JsonConverter(typeof(AssetIDConverter))]
    public uint modelInfoID,
        animListID;

    public override string ToString() =>
        $"flags: {flags}, subtype: {subtype}, pflags: {pflags}, moreFlags: {moreFlags}\n"
        + $"surfaceID: {surfaceID}\n"
        + $"ang: {ang}, pos: {pos}, scale: {scale}\n"
        + $"redMult: {redMult}, greenMult: {greenMult}, blueMult: {blueMult}, seeThru: {seeThru}, seeThruSpeed: {seeThruSpeed}\n"
        + $"modelInfoID: {modelInfoID}, animListID: {animListID}";
}

public struct xLinkAsset
{
    public string srcEvent;
    public string dstEvent;

    [JsonConverter(typeof(AssetIDConverter))]
    public uint dstAssetID;

    [JsonConverter(typeof(AssetIDArrayConverter))]
    public uint[] paramU32;
    public float[] paramF32;

    [JsonConverter(typeof(AssetIDConverter))]
    public uint paramWidgetAssetID;

    [JsonConverter(typeof(AssetIDConverter))]
    public uint chkAssetID;
}

public class AssetDescriptor
{
    public string AssetType { get; set; }
    public AssetType AssetStorage { get; set; }
}

public class ParsedAsset
{
#nullable enable
    public xBaseAsset? Base { get; set; }
    public xLinkAsset[]? Links { get; set; }
    public xEntAsset? Entity { get; set; }

#nullable disable

    [JsonExtensionData]
    public Dictionary<string, object> AssetData { get; set; } = new();
    public string AssetFriendlyName { get; set; }
    public string FileName { get; set; }
}

public abstract class MotionSpecificData { }

[JsonConverter(typeof(xMotionConverter))]
public class xMotion
{
    public MotionType type { get; set; }
    public byte useBanking { get; set; }
    public ushort flags { get; set; }
    public MotionSpecificData specific { get; set; }
}
