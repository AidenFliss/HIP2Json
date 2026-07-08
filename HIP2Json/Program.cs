using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using HipHopFile;

namespace PortHeavyIronGameRewrite;

public struct xVec2
{
    public float x, y;
    public xVec2(float x, float y) { this.x = x; this.y = y; }
    public override string ToString() => $"({x}, {y})";
}

public struct xVec3
{
    public float x, y, z;
    public xVec3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
    public override string ToString() => $"({x}, {y}, {z})";
}

public struct xVec4
{
    public float x, y, z, w;
    public xVec4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
    public override string ToString() => $"({x}, {y}, {z}, {w})";
}

public struct xColor
{
    public float r, g, b, a;
    public xColor(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
    public override string ToString() => $"({r}, {g}, {b}, {a})";
}

public class xMotion
{
    public MotionType type { get; set; }
    public byte useBanking { get; set; }
    public ushort flags { get; set; }
    public object specific { get; set; }
}

public struct xLinkAsset
{
    public string srcEvent;
    public string dstEvent;
    [JsonConverter(typeof(AssetIDConverter))]
    public uint dstAssetID;
    [JsonNumberHandling(JsonNumberHandling.Strict)]
    [JsonConverter(typeof(AssetIDArrayConverter))]
    public uint[] paramU32;
    [JsonNumberHandling(JsonNumberHandling.Strict)]
    public float[] paramF32;
    [JsonConverter(typeof(AssetIDConverter))]
    public uint paramWidgetAssetID;
    [JsonConverter(typeof(AssetIDConverter))]
    public uint chkAssetID;
}

public struct xBaseAsset
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint id;
    public string baseType;
    public byte linkCount;
    public ushort baseFlags;
    public override string ToString()
        => $"id: {id}, baseType: {baseType}, linkCount: {linkCount}, baseFlags: {baseFlags}";
}

public struct xEntAsset
{
    public byte flags, subtype, pflags, moreFlags, pad;
    public uint surfaceID;
    public xVec3 ang, pos, scale;
    public float redMult, greenMult, blueMult, seeThru, seeThruSpeed;
    [JsonConverter(typeof(AssetIDConverter))]
    public uint modelInfoID, animListID;
    public override string ToString() =>
        $"flags: {flags}, subtype: {subtype}, pflags: {pflags}, moreFlags: {moreFlags}, pad: {pad}\n" +
        $"surfaceID: {surfaceID}\n" +
        $"ang: {ang}, pos: {pos}, scale: {scale}\n" +
        $"redMult: {redMult}, greenMult: {greenMult}, blueMult: {blueMult}, seeThru: {seeThru}, seeThruSpeed: {seeThruSpeed}\n" +
        $"modelInfoID: {modelInfoID}, animListID: {animListID}";
}

public enum AssetType
{
    Binary,
    Base,
    Entity
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

public enum GameType
{
    BFBB,
    TSSM
}

class Program
{
    public static GameType CurrentGame;
    public static bool BigEndian = true;

    static readonly HashSet<string> BLACKLIST_ASSETS = new HashSet<string> { "BSP", "JSP", "MODL", "RWTX", "TEXS", "ANIM", "SNDS", "SND", "SHRP", "ATBL", "ALST" };
    static readonly HashSet<string> BASE_ASSETS = new HashSet<string> { "CAM", "CCRV", "CNTR", "COND", "CSNM", "DPAT", "DSCO", "DTRK", "DUPC", "DYNA", "ENV", "FOG", "GRSM", "GRUP", "GUST", "LITE", "LOBM", "MVPT", "NGMS", "PARE", "PARP", "PARS", "PGRS", "PORT", "PRJT", "RANM", "SCRP", "SDFX", "SFX", "SGRP", "SLID", "SPLN", "SSET", "SUBT", "SURF", "TIMR", "TPIK", "TRWT", "UIM", "VOLU", "ZLIN" };
    static readonly HashSet<string> ENTITY_ASSETS = new HashSet<string> { "BOUL", "BUTN", "DSTR", "EGEN", "HANG", "NPC", "PEND", "PKUP", "PLAT", "PLYR", "SIMP", "TRIG", "UI", "UIFT", "VIL" };

    static readonly HashSet<string> SKIP_FILES = new HashSet<string> { "font2", "db05", "b301" };

    static int _totalAssets;
    static int _parsedBase;
    static int _parsedEntity;
    static int _parsedDyna;
    static int _parsedBinary;
    static int _unimplemented;
    static Dictionary<string, int> _unimplByType = new();

    static void Main(string[] args)
    {
        try
        {
            if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
            {
                ShowUsage();
                Environment.Exit(0);
            }

            bool extractMode = args.Contains("--extract");
            bool packMode = args.Contains("--pack");

            if (extractMode == packMode)
            {
                Logger.LogError("Error: you must specify exactly one of --extract or --pack.");
                ShowUsage();
                Environment.Exit(1);
            }

            string gameArg = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "--game", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(args[i], "-g", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length)
                    {
                        gameArg = args[i + 1];
                        break;
                    }
                    Logger.LogError("Error: --game requires a value (BFBB or TSSM).");
                    ShowUsage();
                    Environment.Exit(1);
                }
            }

            if (string.IsNullOrEmpty(gameArg) || !Enum.TryParse<GameType>(gameArg, true, out CurrentGame))
            {
                Logger.LogError($"Error: missing or invalid game configuration. Valid options: BFBB or TSSM.");
                ShowUsage();
                Environment.Exit(1);
            }

            var standardFlags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "--extract", "--pack", "-p", "--progress" };
            var valueFlags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "--game", "-g" };
            
            string[] paths = GetPathArguments(args, standardFlags, valueFlags);

            if (extractMode)
            {
                if (paths.Length < 2)
                {
                    Logger.LogError("Error: missing required arguments for extract. Expected <game_directory> and <project_directory>.");
                    ShowUsage();
                    Environment.Exit(2);
                }

                bool showProgress = args.Contains("-p") || args.Contains("--progress");
                RunExtract(paths[0], paths[1], showProgress);
            }
            else
            {
                if (paths.Length < 1)
                {
                    Logger.LogError("Error: missing required arguments for pack. Expected <project_directory>.");
                    ShowUsage();
                    Environment.Exit(2);
                }

                RunPack(paths[0]);
            }

            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Logger.LogError("Fatal: " + ex);
            Environment.Exit(3);
        }
    }

    static void ShowUsage()
    {
        string ns = typeof(Program).Namespace;

        Logger.LogInfo("Usage:");
        Logger.LogInfo($"  {ns} --extract <game_directory> <project_directory> [--game <BFBB|TSSM>] [--progress]");
        Logger.LogInfo($"  {ns} --pack <project_directory> [--game <BFBB|TSSM>]");
        Logger.LogInfo("");
        Logger.LogInfo("Modes:");
        Logger.LogInfo("  --extract   Extract game files into a project.");
        Logger.LogInfo("  --pack      Pack modified files from a project.");
        Logger.LogInfo("");
        Logger.LogInfo("Options:");
        Logger.LogInfo("  --game, -g      Specify the game. (BFBB or TSSM)");
        Logger.LogInfo("  --progress, -p  Show parsing converage.");
        Logger.LogInfo("  --help, -h      Show this help message.");
    }

   static string[] GetPathArguments(string[] args, HashSet<string> excludedFlags, HashSet<string> flagsWithValues)
    {
        var positionalArgs = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            if (flagsWithValues.Contains(args[i]))
            {
                i++;
                continue;
            }

            if (excludedFlags.Contains(args[i]))
            {
                continue;
            }

            positionalArgs.Add(args[i]);
        }

        return positionalArgs.ToArray();
    }

    static string GetElementKey(JsonElement elem)
    {
        if (elem.ValueKind == JsonValueKind.Object)
        {
            if (elem.TryGetProperty("FileName", out var nameProp) && nameProp.ValueKind == JsonValueKind.String)
                return nameProp.GetString() ?? string.Empty;
        }

        return string.Empty;
    }
    static void RunExtract(string gameDir, string projectDir, bool showProgress)
    {
        string parsedDir = Path.Combine(projectDir, "parsed");
        string unpackedDir = Path.Combine(projectDir, "unpacked");

        Directory.CreateDirectory(parsedDir);
        Directory.CreateDirectory(unpackedDir);

        foreach (string file in Directory.GetFiles(gameDir, "*.*", SearchOption.AllDirectories))
        {
            if (SKIP_FILES.Contains(Path.GetFileNameWithoutExtension(file)))
                continue;

            bool isHipFile = Path.GetExtension(file).ToLower() == ".hip";
            bool isHopFile = Path.GetExtension(file).ToLower() == ".hop";

            string type = isHipFile ? "HIP" : isHopFile ? "HOP" : "Unknown";

            if (type == "Unknown")
                continue;

            string parentFolder = Path.GetFileName(Path.GetDirectoryName(file)!);

            if (parentFolder is "Working" or "New Folder" or "backup")
                continue;

            Logger.LogInfo($"Processing {file}...");
            
            (HipFile hipfile, Game game, Platform platform) = HipHopFile.HipFile.FromPath(file);

            BigEndian = IsPlatformBigEndian(platform);
            
            hipfile.ToIni(game, Path.Combine(unpackedDir, Path.GetFileNameWithoutExtension(file)) + "_" + type, true, true);

            string extractDir = Path.Combine(
                unpackedDir,
                Path.GetFileNameWithoutExtension(file) + "_" + type
            );

            var files = Directory.GetFiles(extractDir, "*.*", SearchOption.AllDirectories);

            var assets = new List<ParsedAsset>();

            foreach (var assetFile in files)
            {
                if (string.Equals(Path.GetFileName(assetFile), "Settings.ini", StringComparison.OrdinalIgnoreCase))
                    continue;

                var parsed = ParseAsset(assetFile);
                if (parsed == null)
                    continue;

                parsed.AssetFriendlyName = GetFriendlyName(Path.GetFileName(assetFile)) ?? "Unknown";
                parsed.FileName = Path.GetFileName(assetFile) ?? "Unknown";

                string assetIdStr = GetAssetId(Path.GetFileName(assetFile));

                Logger.LogInfo("-------------------------------------------------");
                Logger.LogInfo("Asset ID: " + (assetIdStr ?? "Unknown"));

                if (parsed.Base != null)
                {
                    if (Dictionaries.BASETYPE_TO_FRIENDLY_NAME.TryGetValue(parsed.Base?.baseType, out var friendlyType))
                        Logger.LogInfo("Asset Type: " + friendlyType);
                    else
                        Logger.LogWarning("Asset Type: Unknown for " + parsed.Base?.baseType);
                }

                assets.Add(parsed);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                IncludeFields = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            };

            string json = JsonSerializer.Serialize(assets, options);

            string jsonOutputFolder = GetJsonOutputFolder(gameDir, file);
            string jsonOutputFolderOgPath = string.IsNullOrEmpty(jsonOutputFolder) ? Path.Combine(parsedDir, "og") : Path.Combine(parsedDir, "og", jsonOutputFolder);
            string jsonOutputFolderModPath = string.IsNullOrEmpty(jsonOutputFolder) ? Path.Combine(parsedDir, "mod") : Path.Combine(parsedDir, "mod", jsonOutputFolder);

            if (!Directory.Exists(jsonOutputFolderOgPath))
                Directory.CreateDirectory(jsonOutputFolderOgPath);
            if (!Directory.Exists(jsonOutputFolderModPath))
                Directory.CreateDirectory(jsonOutputFolderModPath);

            string jsonFileName = Path.GetFileNameWithoutExtension(file) + "_" + type + "_assets.json";

            File.WriteAllText(
                Path.Combine(jsonOutputFolderOgPath, jsonFileName),
                json);
            File.WriteAllText(
                Path.Combine(jsonOutputFolderModPath, jsonFileName),
                json);
        }

        if (showProgress)
            LogParseReport();

        Logger.LogInfo("Program finished successfully!");
    }

    static void RunPack(string projectDir)
    {
        string jsonFolder = Path.Combine(projectDir, "parsed", "og");
        string hipHopFolder = Path.Combine(projectDir, "unpacked");
        string packedFolder = Path.Combine(projectDir, "packed");

        Directory.CreateDirectory(packedFolder);

        Logger.LogInfo($"Pack mode selected. OG JSON folder: {jsonFolder}");

        ScanJsonKeyDifferences(jsonFolder, hipHopFolder);

        foreach (string hipHopFile in Directory.GetDirectories(hipHopFolder))
        {
            (HipFile hipFile, Game game, Platform platform) = HipHopFile.HipFile.FromINI(Path.Combine(hipHopFile, "Settings.ini"));
            byte[] hipBytes = hipFile.ToBytes(game, platform);
            File.WriteAllBytes(Path.Combine(packedFolder, Path.GetFileNameWithoutExtension(hipHopFile)).Replace("_", "."), hipBytes);
        }
    }

    static void ScanJsonKeyDifferences(string jsonFolder, string hipHopFolder)
    {
        Logger.LogInfo($"Scanning {jsonFolder} for modified assets...\n");

        string ogFolder = Path.GetFullPath(jsonFolder);
        string modFolder = string.Empty;

        if (ogFolder.EndsWith(Path.DirectorySeparatorChar + "og") || ogFolder.EndsWith(Path.AltDirectorySeparatorChar + "og"))
            modFolder = Path.Combine(Path.GetDirectoryName(ogFolder) ?? ogFolder, "mod");
        else
        {
            string parent = Path.GetDirectoryName(ogFolder) ?? ogFolder;
            string siblingMod = Path.Combine(parent, "mod");
            if (Directory.Exists(siblingMod))
                modFolder = siblingMod;
            else
            {
                var parts = ogFolder.Split(Path.DirectorySeparatorChar);
                for (int i = 0; i < parts.Length; i++)
                {
                    if (string.Equals(parts[i], "og", StringComparison.OrdinalIgnoreCase))
                    {
                        parts[i] = "mod";
                        modFolder = string.Join(Path.DirectorySeparatorChar, parts);
                        break;
                    }
                }
            }
        }

        if (string.IsNullOrEmpty(modFolder) || !Directory.Exists(modFolder))
        {
            Logger.LogError($"Could not locate corresponding mod folder for '{ogFolder}'. Expected sibling 'mod' or an 'og' segment.");
            return;
        }

        if (string.IsNullOrEmpty(hipHopFolder) || !Directory.Exists(hipHopFolder))
        {
            Logger.LogError($"Could not locate HIP HOP folder '{hipHopFolder}'!");
            return;
        }

        var changes = new List<Dictionary<string, object>>();

        var ogFiles = Directory.GetFiles(ogFolder, "*_assets.json", SearchOption.AllDirectories);

        foreach (var ogFile in ogFiles)
        {
            string rel = Path.GetRelativePath(ogFolder, ogFile);
            string modFile = Path.Combine(modFolder, rel);

            if (!File.Exists(modFile))
            {
                Logger.LogWarning($"Missing mod file for {rel}");
                changes.Add(new Dictionary<string, object> { { "file", rel }, { "error", "mod_missing" } });
                continue;
            }

            try
            {
                using var ogDoc = JsonDocument.Parse(File.ReadAllText(ogFile));
                using var modDoc = JsonDocument.Parse(File.ReadAllText(modFile));

                var ogRoot = ogDoc.RootElement;
                var modRoot = modDoc.RootElement;

                if (ogRoot.ValueKind != JsonValueKind.Array || modRoot.ValueKind != JsonValueKind.Array)
                {
                    Logger.LogError($"Invalid JSON root for {rel}: expected array in both OG and MOD files.");
                    changes.Add(new Dictionary<string, object> { { "file", rel }, { "error", "not_array" } });
                    continue;
                }

                var ogKeys = BuildElementKeyMap(ogRoot);
                var modKeys = BuildElementKeyMap(modRoot);

                var added = modKeys.Keys.Except(ogKeys.Keys).ToList();
                var removed = ogKeys.Keys.Except(modKeys.Keys).ToList();
                var common = ogKeys.Keys.Intersect(modKeys.Keys);

                var modified = new List<string>();

                bool invalidStructure = false;
                var structureErrors = new List<Dictionary<string, object>>();

                foreach (var k in common)
                {
                    var ogElem = ogKeys[k];
                    var modElem = modKeys[k];

                    if (ogElem.ValueKind == JsonValueKind.Object && modElem.ValueKind == JsonValueKind.Object)
                    {
                        var ogPaths = CollectPropertyPaths(ogElem);
                        var modPaths = CollectPropertyPaths(modElem);

                        var addedProps = modPaths.Except(ogPaths).ToList();
                        var removedProps = ogPaths.Except(modPaths).ToList();

                        if (addedProps.Count > 0 || removedProps.Count > 0)
                        {
                            invalidStructure = true;
                            structureErrors.Add(new Dictionary<string, object>
                            {
                                { "key", k },
                                { "added_props", addedProps },
                                { "removed_props", removedProps }
                            });
                            continue;
                        }
                    }

                    string ogJson = ogElem.GetRawText();
                    string modJson = modElem.GetRawText();
                    if (!string.Equals(ogJson, modJson, StringComparison.Ordinal))
                    {
                        modified.Add(k);
                    }
                }

                if (invalidStructure)
                {
                    Logger.LogError($"Invalid structure changes detected in {rel} for {structureErrors.Count} element(s).\n");
                    foreach (var e in structureErrors)
                    {
                        Logger.LogInfo($"  Element {e["key"]}: added_props={JsonSerializer.Serialize(e["added_props"])}, removed_props={JsonSerializer.Serialize(e["removed_props"]) }");
                    }
                    changes.Add(new Dictionary<string, object>
                    {
                        { "file", rel },
                        { "error", "invalid_structure" },
                        { "details", structureErrors }
                    });
                }
                else if (added.Count > 0 || removed.Count > 0 || modified.Count > 0)
                {
                    Logger.LogInfo($"Changed: {rel} (added:{added.Count}, removed:{removed.Count}, modified:{modified.Count})");
                   
                    if (added.Count > 0) Logger.LogInfo($"  Added element keys: {string.Join(", ", added)}");
                    if (removed.Count > 0) Logger.LogInfo($"  Removed element keys: {string.Join(", ", removed)}");
                    if (modified.Count > 0) Logger.LogInfo($"  Modified element keys: {string.Join(", ", modified)}");

                    var changeEntry = new Dictionary<string, object>
                    {
                        { "file", rel },
                        { "added", added },
                        { "removed", removed },
                        { "modified", modified }
                    };

                    var serializedOutputs = new List<Dictionary<string, object>>();
                    foreach (var key in modified.Concat(added))
                    {
                        try
                        {
                            var modElem = modKeys[key];

                            string detectedAssetType = null;
                            foreach (var prop in modElem.EnumerateObject())
                            {
                                if (ParserMaps.AssetToParser.ContainsKey(prop.Name))
                                {
                                    detectedAssetType = prop.Name;
                                    break;
                                }
                            }

                            if (string.IsNullOrEmpty(detectedAssetType))
                            {
                                serializedOutputs.Add(new Dictionary<string, object> { { "key", key }, { "error", "asset_type_not_detected" } });
                                continue;
                            }

                            if (!ParserMaps.AssetToParser.TryGetValue(detectedAssetType, out AssetParser assetParser))
                            {
                                serializedOutputs.Add(new Dictionary<string, object> { { "key", key }, { "asset", detectedAssetType }, { "error", "no_parser" } });
                                continue;
                            }

                            var asm = typeof(Program).Assembly;
                            string ns = typeof(Program).Namespace;
                            Type targetType = asm.GetType($"{ns}." + detectedAssetType, false, true);

                            if (targetType == null)
                            {
                                serializedOutputs.Add(new Dictionary<string, object> { { "key", key }, { "asset", detectedAssetType }, { "error", "type_not_found" } });
                                continue;
                            }

                            JsonSerializerOptions serOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, IncludeFields = true };
                            var propElem = modElem.GetProperty(detectedAssetType);
                            object obj = JsonSerializer.Deserialize(propElem.GetRawText(), targetType, serOpts);

                            object serialized = null;

                            try
                            {
                                if (obj is DYNA dyna && dyna.dynaSpecificData is JsonElement dynaElem)
                                {
                                    string className = dyna.typeNameInternal.Replace(':', '_');

                                    Type payloadType = Type.GetType($"{ns}.{className}");

                                    if (payloadType != null)
                                    {
                                        dyna.dynaSpecificData = dynaElem.Deserialize(payloadType, serOpts)!;
                                    }
                                }
                                else if (obj is PLAT plat && plat.specific is JsonElement platElem)
                                {
                                    Type payloadType = null;
                                    if (CurrentGame == GameType.BFBB)
                                    {
                                        payloadType = plat.type switch
                                        {
                                            PlatformType.Conveyor => typeof(ConveyorPlatform),
                                            PlatformType.Falling => typeof(FallingPlatform),
                                            PlatformType.FR => typeof(FRPlatform),
                                            PlatformType.Breakaway => typeof(BreakawayPlatformBFBB),
                                            PlatformType.Teeter => typeof(TeeterPlatform),
                                            PlatformType.Paddle => typeof(PaddlePlatform),
                                            _ => null
                                        };
                                    }
                                    else if (CurrentGame == GameType.TSSM)
                                    {
                                        payloadType = plat.type switch
                                        {
                                            PlatformType.Conveyor => typeof(ConveyorPlatform),
                                            PlatformType.Falling => typeof(FallingPlatform),
                                            PlatformType.FR => typeof(FRPlatform),
                                            PlatformType.Breakaway => typeof(BreakawayPlatformTSSM),
                                            PlatformType.Teeter => typeof(TeeterPlatform),
                                            PlatformType.Paddle => typeof(PaddlePlatform),
                                            _ => null
                                        };
                                    }

                                    if (payloadType != null)
                                    {
                                        plat.specific = platElem.Deserialize(payloadType, serOpts)!;
                                    }
                                }
                                
                                serialized = assetParser.Serialize(obj);
                            }
                            catch (NotImplementedException)
                            {
                                serializedOutputs.Add(new Dictionary<string, object> { { "key", key }, { "asset", detectedAssetType }, { "error", "serialize_not_implemented" } });
                                continue;
                            }

                            if (serialized is byte[] coreBytes)
                            {
                                byte[] fullBytes;
                                using (var ms = new MemoryStream())
                                using (var bw = new BinaryWriter(ms))
                                {
                                    if (modElem.ValueKind == JsonValueKind.Object && modElem.TryGetProperty("Base", out var baseProp) && baseProp.ValueKind == JsonValueKind.Object)
                                    {
                                        uint idVal = 0;
                                        if (baseProp.TryGetProperty("id", out var idProp))
                                        {
                                            if (idProp.ValueKind == JsonValueKind.String)
                                            {
                                                var s = idProp.GetString();
                                                if (!string.IsNullOrEmpty(s) && s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                                                    idVal = Convert.ToUInt32(s.Substring(2), 16);
                                                else if (!string.IsNullOrEmpty(s))
                                                    idVal = Convert.ToUInt32(s);
                                            }
                                            else if (idProp.ValueKind == JsonValueKind.Number)
                                            {
                                                idVal = idProp.GetUInt32();
                                            }
                                        }

                                        byte baseTypeByte = 0;
                                        if (baseProp.TryGetProperty("baseType", out var bt) && bt.ValueKind == JsonValueKind.String)
                                        {
                                            var s = bt.GetString();
                                            if (!string.IsNullOrEmpty(s) && s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                                                baseTypeByte = Convert.ToByte(s.Substring(2), 16);
                                            else if (!string.IsNullOrEmpty(s))
                                                baseTypeByte = Convert.ToByte(Convert.ToInt32(s));
                                        }

                                        byte linkCountByte = 0;
                                        if (modElem.TryGetProperty("Links", out var linksProp) && linksProp.ValueKind == JsonValueKind.Array)
                                            linkCountByte = (byte)linksProp.GetArrayLength();

                                        ushort baseFlags = 0;
                                        if (baseProp.TryGetProperty("baseFlags", out var bf) && bf.ValueKind == JsonValueKind.Number)
                                            baseFlags = bf.GetUInt16();

                                        Util.WriteUInt32(bw, idVal, BigEndian);
                                        bw.Write(baseTypeByte);
                                        bw.Write(linkCountByte);
                                        Util.WriteUInt16(bw, baseFlags, BigEndian);
                                    }

                                    if (modElem.ValueKind == JsonValueKind.Object && modElem.TryGetProperty("Entity", out var entProp) && entProp.ValueKind == JsonValueKind.Object)
                                    {
                                        byte flags = entProp.TryGetProperty("flags", out var f) && f.ValueKind==JsonValueKind.Number ? (byte)f.GetUInt32() : (byte)0;
                                        byte subtype = entProp.TryGetProperty("subtype", out var st) && st.ValueKind==JsonValueKind.Number ? (byte)st.GetUInt32() : (byte)0;
                                        byte pflags = entProp.TryGetProperty("pflags", out var pf) && pf.ValueKind==JsonValueKind.Number ? (byte)pf.GetUInt32() : (byte)0;
                                        byte moreFlags = entProp.TryGetProperty("moreFlags", out var mf) && mf.ValueKind==JsonValueKind.Number ? (byte)mf.GetUInt32() : (byte)0;

                                        bw.Write(flags);
                                        bw.Write(subtype);
                                        bw.Write(pflags);
                                        bw.Write(moreFlags);

                                        if (CurrentGame == GameType.BFBB)
                                        {
                                            byte pad = entProp.TryGetProperty("pad", out var padProp) && padProp.ValueKind==JsonValueKind.Number ? (byte)padProp.GetUInt32() : (byte)0;
                                            bw.Write(pad);
                                            bw.Write((byte)0);
                                            bw.Write((byte)0);
                                            bw.Write((byte)0);
                                        }

                                        uint surfaceID = entProp.TryGetProperty("surfaceID", out var sid) && sid.ValueKind==JsonValueKind.Number ? sid.GetUInt32() : 0u;
                                        Util.WriteUInt32(bw, surfaceID, BigEndian);

                                        double Deg2Rad = 1.0 / 57.29577951308232;
                                        if (entProp.TryGetProperty("ang", out var ang) && ang.ValueKind==JsonValueKind.Object)
                                        {
                                            float ax = ang.TryGetProperty("x", out var axp) && axp.ValueKind==JsonValueKind.Number ? axp.GetSingle() : 0f;
                                            float ay = ang.TryGetProperty("y", out var ayp) && ayp.ValueKind==JsonValueKind.Number ? ayp.GetSingle() : 0f;
                                            float az = ang.TryGetProperty("z", out var azp) && azp.ValueKind==JsonValueKind.Number ? azp.GetSingle() : 0f;
                                            Util.WriteFloat(bw, (float)(ax * Deg2Rad), BigEndian);
                                            Util.WriteFloat(bw, (float)(ay * Deg2Rad), BigEndian);
                                            Util.WriteFloat(bw, (float)(az * Deg2Rad), BigEndian);
                                        }
                                        else { Util.WriteFloat(bw, 0f, BigEndian); Util.WriteFloat(bw, 0f, BigEndian); Util.WriteFloat(bw, 0f, BigEndian); }

                                        if (entProp.TryGetProperty("pos", out var pos) && pos.ValueKind==JsonValueKind.Object)
                                        {
                                            float px = pos.TryGetProperty("x", out var pxp) && pxp.ValueKind==JsonValueKind.Number ? pxp.GetSingle() : 0f;
                                            float py = pos.TryGetProperty("y", out var pyp) && pyp.ValueKind==JsonValueKind.Number ? pyp.GetSingle() : 0f;
                                            float pz = pos.TryGetProperty("z", out var pzp) && pzp.ValueKind==JsonValueKind.Number ? pzp.GetSingle() : 0f;
                                            Util.WriteFloat(bw, px, BigEndian); Util.WriteFloat(bw, py, BigEndian); Util.WriteFloat(bw, pz, BigEndian);
                                        }
                                        else { Util.WriteFloat(bw, 0f, BigEndian); Util.WriteFloat(bw, 0f, BigEndian); Util.WriteFloat(bw, 0f, BigEndian); }

                                        if (entProp.TryGetProperty("scale", out var scale) && scale.ValueKind==JsonValueKind.Object)
                                        {
                                            float sx = scale.TryGetProperty("x", out var sxp) && sxp.ValueKind==JsonValueKind.Number ? sxp.GetSingle() : 1f;
                                            float sy = scale.TryGetProperty("y", out var syp) && syp.ValueKind==JsonValueKind.Number ? syp.GetSingle() : 1f;
                                            float sz = scale.TryGetProperty("z", out var szp) && szp.ValueKind==JsonValueKind.Number ? szp.GetSingle() : 1f;
                                            Util.WriteFloat(bw, sx, BigEndian); Util.WriteFloat(bw, sy, BigEndian); Util.WriteFloat(bw, sz, BigEndian);
                                        }
                                        else { Util.WriteFloat(bw, 1f, BigEndian); Util.WriteFloat(bw, 1f, BigEndian); Util.WriteFloat(bw, 1f, BigEndian); }

                                        float redMult = entProp.TryGetProperty("redMult", out var rm) && rm.ValueKind==JsonValueKind.Number ? rm.GetSingle() : 1f;
                                        float greenMult = entProp.TryGetProperty("greenMult", out var gm) && gm.ValueKind==JsonValueKind.Number ? gm.GetSingle() : 1f;
                                        float blueMult = entProp.TryGetProperty("blueMult", out var bm) && bm.ValueKind==JsonValueKind.Number ? bm.GetSingle() : 1f;
                                        float seeThru = entProp.TryGetProperty("seeThru", out var stt) && stt.ValueKind==JsonValueKind.Number ? stt.GetSingle() : 0f;
                                        float seeThruSpeed = entProp.TryGetProperty("seeThruSpeed", out var sts) && sts.ValueKind==JsonValueKind.Number ? sts.GetSingle() : 0f;

                                        Util.WriteFloat(bw, redMult, BigEndian);
                                        Util.WriteFloat(bw, greenMult, BigEndian);
                                        Util.WriteFloat(bw, blueMult, BigEndian);
                                        Util.WriteFloat(bw, seeThru, BigEndian);
                                        Util.WriteFloat(bw, seeThruSpeed, BigEndian);

                                        uint modelInfoID = 0;
                                        if (entProp.TryGetProperty("modelInfoID", out var mid) && mid.ValueKind==JsonValueKind.String && mid.GetString().StartsWith("0x")) modelInfoID = Convert.ToUInt32(mid.GetString().Substring(2), 16);
                                        else if (entProp.TryGetProperty("modelInfoID", out var mid2) && mid2.ValueKind==JsonValueKind.Number) modelInfoID = mid2.GetUInt32();
                                        uint animListID = 0;
                                        if (entProp.TryGetProperty("animListID", out var aid) && aid.ValueKind==JsonValueKind.String && aid.GetString().StartsWith("0x")) animListID = Convert.ToUInt32(aid.GetString().Substring(2), 16);
                                        else if (entProp.TryGetProperty("animListID", out var aid2) && aid2.ValueKind==JsonValueKind.Number) animListID = aid2.GetUInt32();

                                        Util.WriteUInt32(bw, modelInfoID, BigEndian);
                                        Util.WriteUInt32(bw, animListID, BigEndian);
                                    }

                                    bw.Write(coreBytes);

                                    Type eventType = CurrentGame switch
                                    {
                                        GameType.BFBB => asm.GetType($"{ns}.EventBFBB", true, true),
                                        GameType.TSSM => asm.GetType($"{ns}.EventTSSM", true, true),
                                        _ => throw new Exception($"Unsupported game type: {CurrentGame}")
                                    };

                                    if (eventType == null)
                                    {
                                        throw new Exception($"Could not resolve event type for {CurrentGame} in namespace {ns}");
                                    }

                                    if (modElem.ValueKind == JsonValueKind.Object && modElem.TryGetProperty("Links", out var linksArray) && linksArray.ValueKind == JsonValueKind.Array)
                                    {
                                        int linkIndex = 0;
                                        foreach (var link in linksArray.EnumerateArray())
                                        {
                                            long p = ms.Position;
                                            long aligned = (p + 3) & ~3L;
                                            while (ms.Position < aligned) bw.Write((byte)0);

                                            ushort srcEvent = 0;
                                            ushort dstEvent = 0;

                                            if (link.TryGetProperty("srcEvent", out var se) && se.ValueKind == JsonValueKind.String)
                                            {
                                                string valueStr = se.GetString();
                                                try
                                                {
                                                    object parsed = Enum.Parse(eventType, valueStr, ignoreCase: true);
                                                    srcEvent = Convert.ToUInt16(parsed);
                                                }
                                                catch
                                                {
                                                    throw new Exception($"Unknown srcEvent type alignment failure: {valueStr}");
                                                }
                                            }

                                            if (link.TryGetProperty("dstEvent", out var de) && de.ValueKind == JsonValueKind.String)
                                            {
                                                string valueStr = de.GetString();
                                                try
                                                {
                                                    object parsed = Enum.Parse(eventType, valueStr, ignoreCase: true);
                                                    dstEvent = Convert.ToUInt16(parsed);
                                                }
                                                catch
                                                {
                                                    throw new Exception($"Unknown dstEvent type alignment failure: {valueStr}");
                                                }
                                            }

                                            Util.WriteUInt16(bw, srcEvent, BigEndian);
                                            Util.WriteUInt16(bw, dstEvent, BigEndian);

                                            uint dstAssetID = 0;
                                            if (link.TryGetProperty("dstAssetID", out var da) && da.ValueKind==JsonValueKind.String && da.GetString().StartsWith("0x")) dstAssetID = Convert.ToUInt32(da.GetString().Substring(2), 16);
                                            else if (link.TryGetProperty("dstAssetID", out var da2) && da2.ValueKind==JsonValueKind.Number) dstAssetID = da2.GetUInt32();
                                            Util.WriteUInt32(bw, dstAssetID, BigEndian);

                                            uint[] pU32 = new uint[4];
                                            if (link.TryGetProperty("paramU32", out var pu) && pu.ValueKind==JsonValueKind.Array)
                                            {
                                                int i = 0;
                                                foreach (var item in pu.EnumerateArray())
                                                {
                                                    if (i >= 4) break;
                                                    if (item.ValueKind==JsonValueKind.String && item.GetString().StartsWith("0x")) pU32[i] = Convert.ToUInt32(item.GetString().Substring(2), 16);
                                                    else if (item.ValueKind==JsonValueKind.Number) pU32[i] = item.GetUInt32();
                                                    i++;
                                                }
                                            }
                                            else if (link.TryGetProperty("paramF32", out var pf) && pf.ValueKind==JsonValueKind.Array)
                                            {
                                                int i = 0;
                                                foreach (var item in pf.EnumerateArray())
                                                {
                                                    if (i >= 4) break;
                                                    float fv = item.ValueKind==JsonValueKind.Number ? item.GetSingle() : 0f;
                                                    int iv = BitConverter.SingleToInt32Bits(fv);
                                                    pU32[i] = unchecked((uint)iv);
                                                    i++;
                                                }
                                            }

                                            for (int i = 0; i < 4; i++) Util.WriteUInt32(bw, pU32[i], BigEndian);

                                            linkIndex++;

                                            uint paramWidgetAssetID = 0;
                                            if (link.TryGetProperty("paramWidgetAssetID", out var pw) && pw.ValueKind==JsonValueKind.String && pw.GetString().StartsWith("0x")) paramWidgetAssetID = Convert.ToUInt32(pw.GetString().Substring(2), 16);
                                            else if (link.TryGetProperty("paramWidgetAssetID", out var pw2) && pw2.ValueKind==JsonValueKind.Number) paramWidgetAssetID = pw2.GetUInt32();
                                            uint chkAssetID = 0;
                                            if (link.TryGetProperty("chkAssetID", out var ck) && ck.ValueKind==JsonValueKind.String && ck.GetString().StartsWith("0x")) chkAssetID = Convert.ToUInt32(ck.GetString().Substring(2), 16);
                                            else if (link.TryGetProperty("chkAssetID", out var ck2) && ck2.ValueKind==JsonValueKind.Number) chkAssetID = ck2.GetUInt32();

                                            Util.WriteUInt32(bw, paramWidgetAssetID, BigEndian);
                                            Util.WriteUInt32(bw, chkAssetID, BigEndian);
                                        }
                                    }

                                    fullBytes = ms.ToArray();

                                    if (detectedAssetType == "TRIG") //yep code.... i appreaciate you heavy iron my beloved
                                    {
                                        bw.Seek(0x09, SeekOrigin.Begin);
                                        TRIG objectAsTrig = (TRIG)obj;
                                        bw.Write((byte)objectAsTrig.Type);
                                    }
                                }

                                string folder = "";
                                if (detectedAssetType == "DYNA") //dyna is very very epic, thx heavy iron..
                                {
                                    DYNA dyna = (DYNA)obj;
                                    if (ParserMaps.DYNAToParser.TryGetValue(dyna.typeNameInternal, out AbstractDYNAParser parser))
                                    {
                                        folder = parser.GetFolderName();
                                    }
                                    else
                                    {
                                        throw new Exception("Something has gone horrible wrong....");
                                    }
                                }
                                else
                                {
                                    folder = Dictionaries.ID_TO_FOLDER_NAME[detectedAssetType];
                                }

                                string safeKey = string.Join("_", key.Split(Path.GetInvalidFileNameChars()));
                                string outFile = Path.Combine(hipHopFolder, Path.GetFileNameWithoutExtension(modFile.Replace("_assets.json", "")), folder, safeKey);

                                File.WriteAllBytes(outFile, fullBytes);

                                serializedOutputs.Add(new Dictionary<string, object> { { "key", key }, { "asset", detectedAssetType }, { "out_file", outFile } });
                            }
                            else
                            {
                                serializedOutputs.Add(new Dictionary<string, object> { { "key", key }, { "asset", detectedAssetType }, { "error", "serialize_returned_non_bytes" } });
                            }
                        }
                        catch (Exception ex)
                        {
                            serializedOutputs.Add(new Dictionary<string, object> { { "key", key }, { "error", "exception" }, { "message", ex.Message } });
                        }
                    }

                    foreach (var key in removed)
                    {
                        //todo, remove correct assets...
                    }

                    if (serializedOutputs.Count > 0)
                        changeEntry["serialized"] = serializedOutputs;

                    changes.Add(changeEntry);
                }
            }
            catch (JsonException jex)
            {
                Logger.LogError($"Invalid JSON in {rel}: {jex.Message}");
                changes.Add(new Dictionary<string, object> { { "file", rel }, { "error", "invalid_json" }, { "message", jex.Message } });
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to compare {rel}: {ex.Message}");
            }
        }

        Logger.LogInfo("Changes summary:\n" + JsonSerializer.Serialize(changes, new JsonSerializerOptions { WriteIndented = true }));
    }

    static Dictionary<string, JsonElement> BuildElementKeyMap(JsonElement arr)
    {
        var dict = new Dictionary<string, JsonElement>();
        int idx = 0;
        foreach (var el in arr.EnumerateArray())
        {
            string key = GetElementKey(el);
            if (string.IsNullOrEmpty(key))
                key = $"__idx__{idx}";

            if (dict.ContainsKey(key))
                key = key + "#" + idx;

            dict[key] = el;
            idx++;
        }

        return dict;
    }

    static HashSet<string> CollectPropertyPaths(JsonElement elem)
    {
        var set = new HashSet<string>();
        void Recurse(JsonElement node, string prefix)
        {
            switch (node.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var prop in node.EnumerateObject())
                    {
                        string path = string.IsNullOrEmpty(prefix) ? prop.Name : prefix + "." + prop.Name;
                        set.Add(path);
                        Recurse(prop.Value, path);
                    }
                    break;
                case JsonValueKind.Array:
                    string arrPath = prefix + "[]";
                    set.Add(arrPath);
                    foreach (var item in node.EnumerateArray())
                        Recurse(item, arrPath);
                    break;
                default:
                    break;
            }
        }

        Recurse(elem, string.Empty);
        return set;
    }

    static ParsedAsset ParseAsset(string filePath)
    {
        string folderName = Path.GetFileName(Path.GetDirectoryName(filePath)!);
        if (folderName == null)
        {
            Logger.LogWarning($"Skipping (no folder) {filePath}");
            return null;
        }

        if (!Dictionaries.FolderMap.TryGetValue(folderName, out AssetDescriptor assetDescriptor))
        {
            if (!ParserMaps.DYNAFolderToInternalName.TryGetValue(folderName, out string _))
            {
                Logger.LogWarning($"Skipping unknown folder '{folderName}' for file {Path.GetFileName(filePath)}");
                return null;
            }
            
            assetDescriptor = new AssetDescriptor()
            {
                AssetType = "DYNA",
                AssetStorage = AssetType.Base
            };
        }

        if (BLACKLIST_ASSETS.Contains(assetDescriptor.AssetType))
        {
            Logger.LogInfo($"Skipping blacklisted assets in '{folderName}'");
            return null;
        }

        using var fs = File.OpenRead(filePath);
        using var br = new BinaryReader(fs);

        if (br.BaseStream.Length == 0)
        {
            Logger.LogInfo("Skipping asset with 0 bytes!");
            return null;
        }

        long assetStart = br.BaseStream.Position;

        byte linkCount = 0;

        xBaseAsset? baseAsset = null;

        if (assetDescriptor.AssetStorage != AssetType.Binary)
        {
            byte[] header = br.ReadBytes(8);
            if (header.Length < 8)
                return null;

            uint id = Util.ReadUInt32(header, 0, BigEndian);
            byte baseTypeByte = header[4];
            linkCount = header[5];
            ushort baseFlags = Util.ReadUInt16(header, 6, BigEndian);

            string baseTypeStr = $"0x{baseTypeByte:X2}";

            baseAsset = new xBaseAsset
            {
                id = id,
                baseType = baseTypeStr,
                linkCount = linkCount,
                baseFlags = baseFlags
            };
        }

        string assetType = assetDescriptor.AssetType;

        string category = GetCategory(assetType);
        bool implemented = ParserMaps.AssetToParser.ContainsKey(assetType);
        _totalAssets++;

        Logger.LogInfo($"Parsing {Path.GetFileName(filePath)} as {assetType}");

        Dictionary<string, object> additionalData = new();

        xLinkAsset[] links = Array.Empty<xLinkAsset>();
        xEntAsset? ent = null;

        if (assetDescriptor.AssetStorage == AssetType.Entity)
        {
            ent = ParseEntityChunk(br);
        }

        long dataStart = br.BaseStream.Position;

        if (ParserMaps.AssetToParser.TryGetValue(assetType, out AssetParser parser))
        {
            object parsed = parser.Parse(br, assetStart, dataStart);

            additionalData[assetType] = parsed;

            if (assetDescriptor.AssetStorage != AssetType.Binary)
            {
                long linksOffset = parser.GetLinksOffset(br, linkCount);

                br.BaseStream.Seek(linksOffset, SeekOrigin.Begin);

                links = new xLinkAsset[linkCount];

                for (int i = 0; i < linkCount; i++)
                    links[i] = ReadLinkAsset(br);
            }

            if (category == "binary") _parsedBinary++;
            if (category == "dyna") _parsedDyna++;
            if (category == "base") _parsedBase++;
            if (category == "entity") _parsedEntity++;
        }
        else
        {
            string type = Dictionaries.FolderMap[folderName].AssetType;

            if (!_unimplByType.TryGetValue(type, out int count))
                _unimplByType[type] = 1;
            else
                _unimplByType[type] = count + 1;
        
            _unimplemented++;

            Logger.LogWarning($"Unimplemented parser for {type} ({assetType})");
        }

        return new ParsedAsset
        {
            Base = baseAsset,
            Links = links,
            Entity = ent,
            AssetData = additionalData
        };
    }

    static void LogParseReport()
    {
        int implemented = _parsedBase + _parsedEntity + _parsedDyna + _parsedBinary;
        int total = _totalAssets;

        float percent = total == 0 ? 0f : implemented / (float)total * 100f;

        Logger.LogInfo("====================================");
        Logger.LogInfo(" PARSE FINAL REPORT");
        Logger.LogInfo("====================================");

        Logger.LogInfo($"Total assets:        {total}");
        Logger.LogInfo($"Implemented:         {implemented}");
        Logger.LogInfo($"Unimplemented:       {_unimplemented}");
        Logger.LogInfo("");

        Logger.LogInfo($"Base parsed:         {_parsedBase}");
        Logger.LogInfo($"Entity parsed:       {_parsedEntity}");
        Logger.LogInfo($"DYNA parsed:         {_parsedDyna}");
        Logger.LogInfo($"Binary parsed:         {_parsedBinary}");
        Logger.LogInfo("");

        Logger.LogInfo("");
        Logger.LogInfo(" UNIMPLEMENTED:");
        Logger.LogInfo("------------------------------------");

        foreach (var kvp in _unimplByType
            .OrderByDescending(x => x.Value))
        {
            Logger.LogInfo($"{kvp.Key}: {kvp.Value}");
        }

        Logger.LogInfo($"Progress:            {percent:0.00}%");
        Logger.LogInfo("====================================");
    }

    static string GetCategory(string assetType)
    {
        if (ENTITY_ASSETS.Contains(assetType))
            return "entity";

        if (assetType.Contains("dyna", StringComparison.OrdinalIgnoreCase))
            return "dyna";

        if (BASE_ASSETS.Contains(assetType))
            return "base";
        
        return "binary";
    }

    static xEntAsset ParseEntityChunk(BinaryReader br)
    {
        byte flags = br.ReadByte();
        byte subtype = br.ReadByte();
        byte pflags = br.ReadByte();
        byte moreFlags = br.ReadByte();

        byte pad = 0;
        if (CurrentGame == GameType.BFBB)
        {
            pad = br.ReadByte();
            _ = br.ReadBytes(3);
        }

        uint surfaceID = Util.ReadUInt32(br.ReadBytes(4), 0, BigEndian);

        xVec3 ang = ToDegrees(new xVec3(
            Util.ReadFloat(br.ReadBytes(4), 0, BigEndian),
            Util.ReadFloat(br.ReadBytes(4), 0, BigEndian),
            Util.ReadFloat(br.ReadBytes(4), 0, BigEndian)
        ));

        xVec3 pos = new xVec3(
            Util.ReadFloat(br.ReadBytes(4), 0, BigEndian),
            Util.ReadFloat(br.ReadBytes(4), 0, BigEndian),
            Util.ReadFloat(br.ReadBytes(4), 0, BigEndian)
        );

        xVec3 scale = new xVec3(
            Util.ReadFloat(br.ReadBytes(4), 0, BigEndian),
            Util.ReadFloat(br.ReadBytes(4), 0, BigEndian),
            Util.ReadFloat(br.ReadBytes(4), 0, BigEndian)
        );

        float redMult = Util.ReadFloat(br.ReadBytes(4), 0, BigEndian);
        float greenMult = Util.ReadFloat(br.ReadBytes(4), 0, BigEndian);
        float blueMult = Util.ReadFloat(br.ReadBytes(4), 0, BigEndian);
        float seeThru = Util.ReadFloat(br.ReadBytes(4), 0, BigEndian);
        float seeThruSpeed = Util.ReadFloat(br.ReadBytes(4), 0, BigEndian);

        uint modelInfoID = Util.ReadUInt32(br.ReadBytes(4), 0, BigEndian);
        uint animListID = Util.ReadUInt32(br.ReadBytes(4), 0, BigEndian);

        return new xEntAsset
        {
            flags = flags,
            subtype = subtype,
            pflags = pflags,
            moreFlags = moreFlags,
            pad = pad,
            surfaceID = surfaceID,
            ang = ang,
            pos = pos,
            scale = scale,
            redMult = redMult,
            greenMult = greenMult,
            blueMult = blueMult,
            seeThru = seeThru,
            seeThruSpeed = seeThruSpeed,
            modelInfoID = modelInfoID,
            animListID = animListID
        };
    }

    static xLinkAsset ReadLinkAsset(BinaryReader br)
    {
        Align4(br);

        ushort srcEvent = Util.ReadUInt16(br.ReadBytes(2), 0, BigEndian);
        ushort dstEvent = Util.ReadUInt16(br.ReadBytes(2), 0, BigEndian);
        uint dstAssetID = Util.ReadUInt32(br.ReadBytes(4), 0, BigEndian);

        string srcEventStr = "Unknown";
        string dstEventStr = "Unknown";
        if (CurrentGame == GameType.BFBB)
        {
            srcEventStr = ((EventBFBB)srcEvent).ToString();
            dstEventStr = ((EventBFBB)dstEvent).ToString();
        }
        else if (CurrentGame == GameType.TSSM)
        {
            srcEventStr = ((EventTSSM)srcEvent).ToString();
            dstEventStr = ((EventTSSM)dstEvent).ToString();
        }

        uint[] pU32 = new uint[4];
        float[] pF32 = new float[4];
        for (int i = 0; i < 4; i++)
        {
            pU32[i] = Util.ReadUInt32(br.ReadBytes(4), 0, BigEndian);
            pF32[i] = BitConverter.Int32BitsToSingle(unchecked((int)pU32[i]));
        }
        
        uint paramWidgetAssetID = Util.ReadUInt32(br.ReadBytes(4), 0, BigEndian);
        uint chkAssetID = Util.ReadUInt32(br.ReadBytes(4), 0, BigEndian);

        return new xLinkAsset
        {
            srcEvent = srcEventStr,
            dstEvent = dstEventStr,
            dstAssetID = dstAssetID,
            paramU32 = pU32,
            paramF32 = pF32,
            paramWidgetAssetID = paramWidgetAssetID,
            chkAssetID = chkAssetID
        };
    }

    static void Align4(BinaryReader br)
    {
        long p = br.BaseStream.Position;
        long aligned = (p + 3) & ~3;
        if (aligned != p)
            br.BaseStream.Position = aligned;
    }

    static bool IsPlatformBigEndian(Platform p)
    {
        return p == Platform.GameCube;
    }

    static string GetFriendlyName(string fileName)
    {
        int endBracket = fileName.IndexOf(']');
        if (endBracket >= 0 && endBracket < fileName.Length - 1)
        {
            string namePart = fileName.Substring(endBracket + 1).Trim();
            return namePart;
        }
        return fileName;
    }

    static string GetAssetId(string fileName)
    {
        int start = fileName.LastIndexOf('[');
        int end = fileName.LastIndexOf(']');
        if (start >= 0 && end > start)
            return "0x" + fileName.Substring(start + 1, end - start - 1);
        return null;
    }

    static string GetJsonOutputFolder(string gameDir, string filePath)
    {
        string fileDir = Path.GetDirectoryName(filePath) ?? gameDir;
        string relativeDir = Path.GetRelativePath(gameDir, fileDir);

        if (string.IsNullOrEmpty(relativeDir) || relativeDir == ".")
            return string.Empty;

        return Path.GetFileName(fileDir) ?? string.Empty;
    }

    static xVec3 ToDegrees(xVec3 radiansVec)
    {
        const double Rad2Deg = 57.29577951308232;
        return new xVec3(
            (float)(radiansVec.x * Rad2Deg),
            (float)(radiansVec.y * Rad2Deg),
            (float)(radiansVec.z * Rad2Deg)
        );
    }
}