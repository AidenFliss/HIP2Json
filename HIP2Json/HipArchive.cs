using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HIP2Json;

public sealed class HipArchive
{
    private readonly HipFile _hip;
    private readonly Game _game;
    private readonly Platform _platform;
    private readonly List<HipAsset> _assets = new List<HipAsset>();

    public string SourcePath { get; }
    public Game Game => _game;
    public Platform Platform => _platform;
    public IReadOnlyList<HipAsset> Assets => _assets;

    private HipArchive(string sourcePath, HipFile hip, Game game, Platform platform)
    {
        SourcePath = sourcePath;
        _hip = hip;
        _game = game;
        _platform = platform;
    }

    public static HipArchive Open(string path)
    {
        (HipFile hip, Game game, Platform platform) = HipFile.FromPath(path);

        if (File.ReadAllBytes(path).AsSpan().IndexOf("HIPB"u8) < 0)
            hip.HIPB = null;

        Program.ResolveGamePlatform(game, platform);

        HipArchive archive = new HipArchive(path, hip, game, platform);

        foreach (Section_AHDR ahdr in hip.DICT.ATOC.AHDRList)
        {
            string type = ahdr.assetType.GetCode();
            string assetName = ahdr.ADBG?.assetName;

            ParsedAsset parsed = null;
            if (!Program.BLACKLIST_ASSETS.Contains(type))
            {
                try
                {
                    parsed = Program.ParseAssetBytes(ahdr.data, type, assetName);
                }
                catch
                {
                    parsed = null;
                }
            }

            archive._assets.Add(new HipAsset(ahdr, type, assetName, parsed));
        }

        return archive;
    }

    public byte[] ToBytes()
    {
        foreach (HipAsset asset in _assets)
            asset.ApplyTo(_hip);

        return _hip.ToBytes(_game, _platform);
    }

    public void Save(string path)
    {
        File.WriteAllBytes(path, ToBytes());
    }

    public void Overwrite()
    {
        Save(SourcePath);
    }

    public HipAsset this[uint assetID]
    {
        get { return _assets.FirstOrDefault(a => a.AssetID == assetID); }
    }

    public HipAsset this[string assetName]
    {
        get { return _assets.FirstOrDefault(a => string.Equals(a.AssetName, assetName, System.StringComparison.OrdinalIgnoreCase)); }
    }

    public static uint HashName(string name)
    {
        uint hash = 0;
        foreach (char c in name.ToUpperInvariant())
            hash = (hash * 131) + c;
        return hash;
    }

    public HipAsset AddAsset(string name, string type, ParsedAsset parsed)
    {
        uint assetID = HashName(name);

        if (_assets.Any(a => a.AssetID == assetID))
            throw new InvalidOperationException($"Asset ID collision for '{name}' ({assetID:X8}).");

        if (parsed.Base != null)
        {
            xBaseAsset b = parsed.Base.Value;
            b.id = assetID;
            parsed.Base = b;
        }

        AssetType assetType = AssetType.Null;
        foreach (AssetType t in System.Enum.GetValues<AssetType>())
        {
            if (HipTypes.GetCode(t) == type)
            {
                assetType = t;
                break;
            }
        }

        if (assetType == AssetType.Null)
            throw new InvalidOperationException($"Unknown asset type '{type}'.");

        Section_AHDR ahdr = new Section_AHDR(assetID, assetType.ToString(), AHDRFlags.SOURCE_VIRTUAL, new Section_ADBG(0, name, name, 0));
        ahdr.data = Program.SerializeParsedAsset(parsed);

        _hip.DICT.ATOC.AHDRList.Add(ahdr);

        Section_LHDR layer = _hip.DICT.LTOC.LHDRList.FirstOrDefault(l => l.layerType == 0);
        if (layer == null)
        {
            layer = new Section_LHDR { layerType = 0, assetIDlist = new List<uint>(), LDBG = new Section_LDBG(0) };
            _hip.DICT.LTOC.LHDRList.Add(layer);
        }
        layer.assetIDlist.Add(assetID);

        HipAsset asset = new HipAsset(ahdr, type, name, parsed);
        _assets.Add(asset);
        return asset;
    }

    public int RemoveAssets(Func<HipAsset, bool> predicate)
    {
        List<HipAsset> doomed = _assets.Where(predicate).ToList();

        foreach (HipAsset asset in doomed)
        {
            _assets.Remove(asset);
            _hip.DICT.ATOC.AHDRList.RemoveAll(h => h.assetID == asset.AssetID);
            foreach (Section_LHDR layer in _hip.DICT.LTOC.LHDRList)
                layer.assetIDlist.RemoveAll(id => id == asset.AssetID);
        }

        return doomed.Count;
    }
}

public sealed class HipAsset
{
    private readonly Section_AHDR _ahdr;

    public string Type { get; }
    public string AssetName { get; }
    public uint AssetID => _ahdr.assetID;
    public int AssetFlags => (int)_ahdr.flags;
    public ParsedAsset Parsed { get; }
    public byte[] Raw => _ahdr.data;

    public HipAsset(Section_AHDR ahdr, string type, string assetName, ParsedAsset parsed)
    {
        _ahdr = ahdr;
        Type = type;
        AssetName = assetName;
        Parsed = parsed;
    }

    public byte[] ToBytes()
    {
        if (Parsed != null)
            return Program.SerializeParsedAsset(Parsed);

        return Raw;
    }

    public T Get<T>() where T : class
    {
        if (Parsed?.AssetData != null && Parsed.AssetData.TryGetValue(Type, out object value))
            return value as T;

        return null;
    }

    internal void ApplyTo(HipFile hip)
    {
        byte[] data = ToBytes();
        _ahdr.data = data;

        Section_ADBG adbg = _ahdr.ADBG;
        int checksum = unchecked((int)Crc32Mpeg2.Compute(data));
        _ahdr.ADBG = new Section_ADBG(
            adbg?.alignment ?? 0,
            adbg?.assetName,
            adbg?.assetFileName ?? adbg?.assetName,
            checksum
        );
    }
}