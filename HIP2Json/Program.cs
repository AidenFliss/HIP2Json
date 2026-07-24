using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using HipHopFile;

namespace HIP2Json;

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

public abstract class MotionSpecificData { }

[JsonConverter(typeof(xMotionConverter))]
public class xMotion
{
    public MotionType type { get; set; }
    public byte useBanking { get; set; }
    public ushort flags { get; set; }
    public MotionSpecificData specific { get; set; }
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

[Flags]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BaseFlags : ushort
{
    None = 0,
    Enabled = 0x01,
    Persistent = 0x02,
    Valid = 0x04,
    VisibleDuringCutscenes = 0x08,
    ReceiveShadows = 0x10,
}

public struct xBaseAsset
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint id;
    public string baseType;
    public byte linkCount;
    public BaseFlags baseFlags;
    public override string ToString()
        => $"id: {id}, baseType: {baseType}, linkCount: {linkCount}, baseFlags: {baseFlags}";
}

[Flags]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EntFlags : byte
{
    None = 0,
    Visible = 0x01,
    Stackable = 0x02,
    Unused04 = 0x04,
    Unknown08 = 0x08,
    Unused10 = 0x10,
    Unused20 = 0x20,
    NoShadow = 0x40,
    Unused80 = 0x80,
}

[Flags]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EntFlagsMore : byte
{
    None = 0,
    Unused01 = 0x01,
    PreciseCollision = 0x02,
    Unknown04 = 0x04,
    Grabbable = 0x08,
    Hittable = 0x10,
    AnimateCollision = 0x20,
    Unused40 = 0x40,
    LedgeGrab = 0x80,
}

public struct xEntAsset
{
    public EntFlags flags;
    public byte subtype;
    public byte pflags;
    public EntFlagsMore moreFlags;
    public uint surfaceID;
    public xVec3 ang, pos, scale;
    public float redMult, greenMult, blueMult, seeThru, seeThruSpeed;
    [JsonConverter(typeof(AssetIDConverter))]
    public uint modelInfoID, animListID;
    public override string ToString() =>
        $"flags: {flags}, subtype: {subtype}, pflags: {pflags}, moreFlags: {moreFlags}\n" +
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

public enum GamePlatform
{
    GC,
    PS2,
    XBOX
}

class Program
{
    public static GameType CurrentGame;
    public static GamePlatform CurrentPlatform;
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
    public static int _unimplemented;
    public static Dictionary<string, int> _unimplByType = new();

    static void Main(string[] args)
    {
        try
        {
            if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
            {
                ShowUsage();
                return;
            }

            bool extractMode = args.Contains("--extract") || args.Contains("-e");
            bool packMode = args.Contains("--pack") || args.Contains("-k");
            bool showProgress = args.Contains("--progress") || args.Contains("-c");

            if (extractMode == packMode)
            {
                Logger.LogError("Error: Specify exactly one mode (--extract or --pack).");
                ShowUsage();
                return;
            }

            string gameStr = GetFlagValue(args, "--game", "-g");
            string platformStr = GetFlagValue(args, "--platform", "-p");

            if (string.IsNullOrEmpty(gameStr) || !Enum.TryParse<GameType>(gameStr, true, out CurrentGame))
            {
                Logger.LogError("Error: Invalid or missing game (--game <BFBB|TSSM>).");
                ShowUsage();
                return;
            }

            if (string.IsNullOrEmpty(platformStr) || !Enum.TryParse<GamePlatform>(platformStr, true, out CurrentPlatform))
            {
                Logger.LogError("Error: Invalid or missing platform (--platform <GC|PS2|XBOX>).");
                ShowUsage();
                return;
            }

            BigEndian = CurrentPlatform == GamePlatform.GC;

            var pathArgs = GetPositionalArguments(args);

            if (pathArgs.Length == 0)
            {
                Logger.LogError("Error: Missing target input path.");
                ShowUsage();
                return;
            }

            string inputPath = pathArgs[0];

            if (extractMode)
            {
                string outputDir = pathArgs.Length > 1 
                    ? pathArgs[1] 
                    : GetDefaultExtractPath(inputPath);

                RunExtract(inputPath, outputDir, showProgress);
            }
            else 
            {
                string outputPath = pathArgs.Length > 1 
                    ? pathArgs[1] 
                    : GetDefaultPackPath(inputPath);

                RunPack(inputPath, outputPath);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("Fatal: " + ex);
            Environment.Exit(3);
        }
    }

    static string GetFlagValue(string[] args, string longName, string shortName)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], longName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(args[i], shortName, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }
        return null;
    }

    static string[] GetPositionalArguments(string[] args)
    {
        var positional = new List<string>();
        var valueFlags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "--game", "-g", "--platform", "-p" };

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];

            if (valueFlags.Contains(arg))
            {
                i++;
                continue;
            }

            if (arg.StartsWith("-"))
                continue;

            positional.Add(arg);
        }

        return positional.ToArray();
    }

    static string GetDefaultExtractPath(string inputPath)
    {
        string folderName = Path.GetFileNameWithoutExtension(inputPath);
        if (File.Exists(inputPath))
        {
            return Path.Combine(Directory.GetCurrentDirectory(), folderName + "_unpacked");
        }

        string cleanInput = inputPath.TrimEnd('/', '\\');
        return Path.Combine(Directory.GetCurrentDirectory(), Path.GetFileName(cleanInput) + "_project");
    }

    static string GetDefaultPackPath(string inputPath)
    {
        if (File.Exists(inputPath)) return inputPath;

        string cleanPath = inputPath.TrimEnd('/', '\\');

        if (cleanPath.EndsWith("_unpacked", StringComparison.OrdinalIgnoreCase))
        {
            string filePrefix = Path.GetFileName(cleanPath).Replace("_unpacked", "");
            
            string ext = filePrefix.EndsWith("_HOP", StringComparison.OrdinalIgnoreCase) ? ".hop" : ".hip";
            string cleanFileName = filePrefix.Replace("_HOP", "").Replace("_HIP", "");

            return Path.Combine(cleanPath, cleanFileName + ext);
        }

        return Path.Combine(cleanPath, "packed");
    }

    static void ShowUsage()
    {
        string ns = typeof(Program).Namespace;

        Logger.LogInfo("Usage:");
        Logger.LogInfo($"  {ns} --extract <input_path> [output_path] [options]");
        Logger.LogInfo($"  {ns} --pack    <input_path> [output_path] [options]");
        Logger.LogInfo("");
        Logger.LogInfo("Modes:");
        Logger.LogInfo("  --extract, -e   Extract a single .hip/.hop archive OR an entire game files directory.");
        Logger.LogInfo("  --pack, -k      Pack a project folder (*_unpacked or *_project) back into binary archive(s).");
        Logger.LogInfo("");
        Logger.LogInfo("Options:");
        Logger.LogInfo("  --game, -g      Specify target game format (BFBB or TSSM). [Required]");
        Logger.LogInfo("  --platform, -p  Specify target platform format (GC, PS2, or XBOX). [Required]");
        Logger.LogInfo("  --progress, -c  Show parsing coverage stats.");
        Logger.LogInfo("  --help, -h      Show this help message.");
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
    static void RunExtract(string targetPath, string projectDir, bool showProgress)
    {
        if (File.Exists(targetPath))
        {
            string baseDir = Path.GetDirectoryName(targetPath) ?? targetPath;

            string ext = Path.GetExtension(targetPath).ToLower();
            if (ext != ".hip" && ext != ".hop")
            {
                Logger.LogError($"Error: Target file '{targetPath}' is not a .hip or .hop file.");
                return;
            }

            Logger.LogInfo($"Extracting single archive: {targetPath} -> {projectDir}");
            Directory.CreateDirectory(projectDir);

            ProcessSingleArchiveExtract(targetPath, baseDir, projectDir, showProgress);
        }
        else if (Directory.Exists(targetPath))
        {
            Logger.LogInfo($"Extracting full directory: {targetPath} -> {projectDir}");
            Directory.CreateDirectory(Path.Combine(projectDir, "parsed", "og"));
            Directory.CreateDirectory(Path.Combine(projectDir, "parsed", "mod"));
            Directory.CreateDirectory(Path.Combine(projectDir, "unpacked"));

            foreach (string file in Directory.GetFiles(targetPath, "*.*", SearchOption.AllDirectories))
            {
                string fileExt = Path.GetExtension(file).ToLower();
                if (fileExt != ".hip" && fileExt != ".hop") continue;

                string fileName = Path.GetFileNameWithoutExtension(file);
                if (SKIP_FILES.Contains(fileName)) continue;

                string parentFolder = Path.GetFileName(Path.GetDirectoryName(file)!);
                if (parentFolder is "Working" or "New Folder" or "backup") continue;

                ProcessSingleArchiveExtract(file, targetPath, projectDir, showProgress);
            }
        }
        else
        {
            Logger.LogError($"Error: Target path '{targetPath}' does not exist.");
        }
    }

    static void ProcessSingleArchiveExtract(string filePath, string baseDir, string projectDir, bool showProgress)
    {
        string unpackedDir = Path.Combine(projectDir, "unpacked");

        bool isHipFile = Path.GetExtension(filePath).ToLower() == ".hip";
        bool isHopFile = Path.GetExtension(filePath).ToLower() == ".hop";

        string type = isHipFile ? "HIP" : isHopFile ? "HOP" : "Unknown";

        if (type == "Unknown")
            return;

        string parentFolder = Path.GetFileName(Path.GetDirectoryName(filePath)!);

        if (parentFolder is "Working" or "New Folder" or "backup")
            return;

        Logger.LogInfo($"Processing {filePath}...");

        (HipFile hipfile, Game game, Platform _) = HipHopFile.HipFile.FromPath(filePath);

        string relativeSubFolder = GetJsonOutputFolder(baseDir, filePath);
        string archiveName = Path.GetFileNameWithoutExtension(filePath) + "_" + type;

        string extractDir = Path.Combine(unpackedDir, archiveName);

        Directory.CreateDirectory(extractDir);

        hipfile.ToIni(game, extractDir, true, true);

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

        bool isBulkExtract = Directory.Exists(baseDir) && baseDir != Path.GetDirectoryName(filePath);

        string jsonOutputFolderOgPath = isBulkExtract || !string.IsNullOrEmpty(relativeSubFolder)
            ? Path.Combine(projectDir, "parsed", "og", relativeSubFolder)
            : Path.Combine(projectDir, "og");

        string jsonOutputFolderModPath = isBulkExtract || !string.IsNullOrEmpty(relativeSubFolder)
            ? Path.Combine(projectDir, "parsed", "mod", relativeSubFolder)
            : Path.Combine(projectDir, "mod");

        Directory.CreateDirectory(jsonOutputFolderOgPath);
        Directory.CreateDirectory(jsonOutputFolderModPath);

        string jsonFileName = archiveName + "_assets.json";

        File.WriteAllText(Path.Combine(jsonOutputFolderOgPath, jsonFileName), json);
        File.WriteAllText(Path.Combine(jsonOutputFolderModPath, jsonFileName), json);
    }

    static void RunPack(string inputPath, string outputPath)
    {
        if (!Directory.Exists(inputPath))
        {
            Logger.LogError($"Error: Target directory '{inputPath}' does not exist.");
            return;
        }

        string ogJsonFolder = Path.Combine(inputPath, "og");
        string hipHopFolder = Path.Combine(inputPath, "unpacked");

        if (!Directory.Exists(ogJsonFolder) && inputPath.EndsWith("og", StringComparison.OrdinalIgnoreCase))
        {
            ogJsonFolder = inputPath;
        }

        if (Directory.Exists(hipHopFolder))
        {
            if (Directory.Exists(ogJsonFolder))
            {
                ScanJsonKeyDifferences(ogJsonFolder, hipHopFolder);
            }
            else
            {
                Logger.LogWarning($"Could not find 'og' folder at '{ogJsonFolder}'. Skipping JSON diff scan.");
            }

            string directSettings = Path.Combine(hipHopFolder, "Settings.ini");
            if (File.Exists(directSettings))
            {
                (HipFile hipFile, Game game, Platform platform) = HipHopFile.HipFile.FromINI(directSettings);
                byte[] hipBytes = hipFile.ToBytes(game, platform);

                string finalOutFile = outputPath.EndsWith(".hip") || outputPath.EndsWith(".hop")
                    ? outputPath
                    : outputPath + ".hip";

                string parentDir = Path.GetDirectoryName(finalOutFile);
                if (!string.IsNullOrEmpty(parentDir))
                    Directory.CreateDirectory(parentDir);

                File.WriteAllBytes(finalOutFile, hipBytes);
                Logger.LogInfo($"Packed archive to {finalOutFile}");
                return;
            }

            Directory.CreateDirectory(outputPath);
            foreach (string archiveFolder in Directory.GetDirectories(hipHopFolder, "*", SearchOption.AllDirectories))
            {
                string settingsPath = Path.Combine(archiveFolder, "Settings.ini");
                if (!File.Exists(settingsPath)) continue;

                (HipFile hipFile, Game game, Platform platform) = HipHopFile.HipFile.FromINI(settingsPath);
                byte[] hipBytes = hipFile.ToBytes(game, platform);

                string rawName = Path.GetFileName(archiveFolder);
                string outFileName = rawName.EndsWith("_HIP") ? rawName.Replace("_HIP", ".hip") :
                                    rawName.EndsWith("_HOP") ? rawName.Replace("_HOP", ".hop") :
                                    rawName;

                string relativePath = Path.GetRelativePath(hipHopFolder, Path.GetDirectoryName(archiveFolder)!);
                string targetOutDir = relativePath == "." ? outputPath : Path.Combine(outputPath, relativePath);
                Directory.CreateDirectory(targetOutDir);

                File.WriteAllBytes(Path.Combine(targetOutDir, outFileName), hipBytes);
                Logger.LogInfo($"Packed archive to {Path.Combine(targetOutDir, outFileName)}");
            }
        }
        else
        {
            string settingsPath = Path.Combine(inputPath, "Settings.ini");
            if (File.Exists(settingsPath))
            {
                (HipFile hipFile, Game game, Platform platform) = HipHopFile.HipFile.FromINI(settingsPath);
                byte[] hipBytes = hipFile.ToBytes(game, platform);

                string finalOutFile = outputPath.EndsWith(".hip") || outputPath.EndsWith(".hop")
                    ? outputPath
                    : outputPath + ".hip";

                File.WriteAllBytes(finalOutFile, hipBytes);
                Logger.LogInfo($"Packed archive to {finalOutFile}");
            }
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

                    if (!JsonElement.DeepEquals(ogElem, modElem))
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

                                        if (dyna.typeNameInternal.StartsWith("Enemy:SB:") && dyna.dynaSpecificData is Enemy_SB enemy)
                                        {
                                            Type enemyType = dyna.typeNameInternal switch
                                            {
                                                "Enemy:SB:BucketOTron" => typeof(Enemy_SB_BucketOTron),
                                                "Enemy:SB:CastNCrew"   => typeof(Enemy_SB_CastNCrew),
                                                "Enemy:SB:Critter"     => typeof(Enemy_SB_Critter),
                                                "Enemy:SB:Dennis"      => typeof(Enemy_SB_Dennis),
                                                "Enemy:SB:FrogFish"    => typeof(Enemy_SB_FrogFish),
                                                "Enemy:SB:Mindy"       => typeof(Enemy_SB_Mindy),
                                                "Enemy:SB:Neptune"     => typeof(Enemy_SB_Neptune),
                                                "Enemy:SB:Standard"    => typeof(Enemy_SB_Standard),
                                                "Enemy:SB:SupplyCrate" => typeof(Enemy_SB_SupplyCrate),
                                                "Enemy:SB:Turret"      => typeof(Enemy_SB_Turret),
                                                _ => null
                                            };

                                            if (enemyType != null &&
                                                enemy.enemyData is JsonElement enemyElem)
                                            {
                                                enemy.enemyData = enemyElem.Deserialize(enemyType, serOpts)!;
                                            }
                                        }
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
                                        if (baseProp.TryGetProperty("baseFlags", out var bf) && bf.ValueKind == JsonValueKind.String)
                                            baseFlags = (ushort)Enum.Parse<BaseFlags>(bf.GetString()!);

                                        Util.WriteUInt32(bw, idVal);
                                        bw.Write(baseTypeByte);
                                        bw.Write(linkCountByte);
                                        Util.WriteUInt16(bw, baseFlags);
                                    }

                                    if (modElem.ValueKind == JsonValueKind.Object && modElem.TryGetProperty("Entity", out var entProp) && entProp.ValueKind == JsonValueKind.Object)
                                    {
                                        EntFlags flags = entProp.TryGetProperty("flags", out var f) ? f.Deserialize<EntFlags>() : EntFlags.None;
                                        byte subtype = entProp.TryGetProperty("subtype", out var st) && st.ValueKind==JsonValueKind.Number ? (byte)st.GetUInt32() : (byte)0;
                                        byte pflags = entProp.TryGetProperty("pflags", out var pf) && pf.ValueKind==JsonValueKind.Number ? (byte)pf.GetUInt32() : (byte)0;
                                        EntFlagsMore moreFlags = entProp.TryGetProperty("moreFlags", out var mf) ? mf.Deserialize<EntFlagsMore>() : EntFlagsMore.None;

                                        bw.Write((byte)flags);
                                        bw.Write(subtype);
                                        bw.Write(pflags);
                                        bw.Write((byte)moreFlags);

                                        if (CurrentGame == GameType.BFBB)
                                        {
                                            bw.Write(new byte[4]);
                                        }

                                        uint surfaceID = entProp.TryGetProperty("surfaceID", out var sid) && sid.ValueKind==JsonValueKind.Number ? sid.GetUInt32() : 0u;
                                        Util.WriteUInt32(bw, surfaceID);

                                        double Deg2Rad = 1.0 / 57.29577951308232;
                                        if (entProp.TryGetProperty("ang", out var ang) && ang.ValueKind==JsonValueKind.Object)
                                        {
                                            float ax = ang.TryGetProperty("x", out var axp) && axp.ValueKind==JsonValueKind.Number ? axp.GetSingle() : 0f;
                                            float ay = ang.TryGetProperty("y", out var ayp) && ayp.ValueKind==JsonValueKind.Number ? ayp.GetSingle() : 0f;
                                            float az = ang.TryGetProperty("z", out var azp) && azp.ValueKind==JsonValueKind.Number ? azp.GetSingle() : 0f;
                                            Util.WriteFloat(bw, (float)(ax * Deg2Rad));
                                            Util.WriteFloat(bw, (float)(ay * Deg2Rad));
                                            Util.WriteFloat(bw, (float)(az * Deg2Rad));
                                        }
                                        else { Util.WriteFloat(bw, 0f); Util.WriteFloat(bw, 0f); Util.WriteFloat(bw, 0f); }

                                        if (entProp.TryGetProperty("pos", out var pos) && pos.ValueKind==JsonValueKind.Object)
                                        {
                                            float px = pos.TryGetProperty("x", out var pxp) && pxp.ValueKind==JsonValueKind.Number ? pxp.GetSingle() : 0f;
                                            float py = pos.TryGetProperty("y", out var pyp) && pyp.ValueKind==JsonValueKind.Number ? pyp.GetSingle() : 0f;
                                            float pz = pos.TryGetProperty("z", out var pzp) && pzp.ValueKind==JsonValueKind.Number ? pzp.GetSingle() : 0f;
                                            Util.WriteFloat(bw, px); Util.WriteFloat(bw, py); Util.WriteFloat(bw, pz);
                                        }
                                        else { Util.WriteFloat(bw, 0f); Util.WriteFloat(bw, 0f); Util.WriteFloat(bw, 0f); }

                                        if (entProp.TryGetProperty("scale", out var scale) && scale.ValueKind==JsonValueKind.Object)
                                        {
                                            float sx = scale.TryGetProperty("x", out var sxp) && sxp.ValueKind==JsonValueKind.Number ? sxp.GetSingle() : 1f;
                                            float sy = scale.TryGetProperty("y", out var syp) && syp.ValueKind==JsonValueKind.Number ? syp.GetSingle() : 1f;
                                            float sz = scale.TryGetProperty("z", out var szp) && szp.ValueKind==JsonValueKind.Number ? szp.GetSingle() : 1f;
                                            Util.WriteFloat(bw, sx); Util.WriteFloat(bw, sy); Util.WriteFloat(bw, sz);
                                        }
                                        else { Util.WriteFloat(bw, 1f); Util.WriteFloat(bw, 1f); Util.WriteFloat(bw, 1f); }

                                        float redMult = entProp.TryGetProperty("redMult", out var rm) && rm.ValueKind==JsonValueKind.Number ? rm.GetSingle() : 1f;
                                        float greenMult = entProp.TryGetProperty("greenMult", out var gm) && gm.ValueKind==JsonValueKind.Number ? gm.GetSingle() : 1f;
                                        float blueMult = entProp.TryGetProperty("blueMult", out var bm) && bm.ValueKind==JsonValueKind.Number ? bm.GetSingle() : 1f;
                                        float seeThru = entProp.TryGetProperty("seeThru", out var stt) && stt.ValueKind==JsonValueKind.Number ? stt.GetSingle() : 0f;
                                        float seeThruSpeed = entProp.TryGetProperty("seeThruSpeed", out var sts) && sts.ValueKind==JsonValueKind.Number ? sts.GetSingle() : 0f;

                                        Util.WriteFloat(bw, redMult);
                                        Util.WriteFloat(bw, greenMult);
                                        Util.WriteFloat(bw, blueMult);
                                        Util.WriteFloat(bw, seeThru);
                                        Util.WriteFloat(bw, seeThruSpeed);

                                        uint modelInfoID = 0;
                                        if (entProp.TryGetProperty("modelInfoID", out var mid) && mid.ValueKind==JsonValueKind.String && mid.GetString().StartsWith("0x")) modelInfoID = Convert.ToUInt32(mid.GetString().Substring(2), 16);
                                        else if (entProp.TryGetProperty("modelInfoID", out var mid2) && mid2.ValueKind==JsonValueKind.Number) modelInfoID = mid2.GetUInt32();
                                        uint animListID = 0;
                                        if (entProp.TryGetProperty("animListID", out var aid) && aid.ValueKind==JsonValueKind.String && aid.GetString().StartsWith("0x")) animListID = Convert.ToUInt32(aid.GetString().Substring(2), 16);
                                        else if (entProp.TryGetProperty("animListID", out var aid2) && aid2.ValueKind==JsonValueKind.Number) animListID = aid2.GetUInt32();

                                        Util.WriteUInt32(bw, modelInfoID);
                                        Util.WriteUInt32(bw, animListID);
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

                                            Util.WriteUInt16(bw, srcEvent);
                                            Util.WriteUInt16(bw, dstEvent);

                                            uint dstAssetID = 0;
                                            if (link.TryGetProperty("dstAssetID", out var da) && da.ValueKind==JsonValueKind.String && da.GetString().StartsWith("0x")) dstAssetID = Convert.ToUInt32(da.GetString().Substring(2), 16);
                                            else if (link.TryGetProperty("dstAssetID", out var da2) && da2.ValueKind==JsonValueKind.Number) dstAssetID = da2.GetUInt32();
                                            Util.WriteUInt32(bw, dstAssetID);

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
                                                    if (float.IsFinite(fv)) //can have coincidental NaN as a float interpretation... thx for no metadata :heavyironmoment:
                                                    {
                                                        int iv = BitConverter.SingleToInt32Bits(fv);
                                                        pU32[i] = unchecked((uint)iv);
                                                        i++;
                                                    }
                                                }
                                            }

                                            for (int i = 0; i < 4; i++) Util.WriteUInt32(bw, pU32[i]);

                                            linkIndex++;

                                            uint paramWidgetAssetID = 0;
                                            if (link.TryGetProperty("paramWidgetAssetID", out var pw) && pw.ValueKind==JsonValueKind.String && pw.GetString().StartsWith("0x")) paramWidgetAssetID = Convert.ToUInt32(pw.GetString().Substring(2), 16);
                                            else if (link.TryGetProperty("paramWidgetAssetID", out var pw2) && pw2.ValueKind==JsonValueKind.Number) paramWidgetAssetID = pw2.GetUInt32();
                                            uint chkAssetID = 0;
                                            if (link.TryGetProperty("chkAssetID", out var ck) && ck.ValueKind==JsonValueKind.String && ck.GetString().StartsWith("0x")) chkAssetID = Convert.ToUInt32(ck.GetString().Substring(2), 16);
                                            else if (link.TryGetProperty("chkAssetID", out var ck2) && ck2.ValueKind==JsonValueKind.Number) chkAssetID = ck2.GetUInt32();

                                            Util.WriteUInt32(bw, paramWidgetAssetID);
                                            Util.WriteUInt32(bw, chkAssetID);
                                        }
                                    }

                                    fullBytes = ms.ToArray();

                                    if (detectedAssetType == "TRIG") //yep code.... i appreaciate you heavy iron my beloved
                                    {
                                        bw.Seek(0x09, SeekOrigin.Begin);
                                        TRIG objectAsTrig = (TRIG)obj;
                                        bw.Write((byte)objectAsTrig.Type);
                                    }
                                    else if (detectedAssetType == "PKUP")
                                    {
                                        bw.Seek(0x09, SeekOrigin.Begin);
                                        PKUP objectAsPkup = (PKUP)obj;
                                        bw.Write((byte)objectAsPkup.pickupType);
                                    }
                                }

                                string folder = "";
                                if (detectedAssetType == "DYNA") //dyna is very very epic, thx heavy iron..
                                {
                                    DYNA dyna = (DYNA)obj;

                                    if (ParserMaps.TryGetDYNAParser(dyna.typeNameInternal, out AbstractDYNAParser parser))
                                    {
                                        folder = parser.GetFolderName();

                                        if (dyna.typeNameInternal.StartsWith("Enemy:SB:"))
                                        {
                                            switch (dyna.typeNameInternal)
                                            {
                                                case "Enemy:SB:BucketOTron":
                                                    folder = "Spawner";
                                                    break;
                                                case "Enemy:SB:CastNCrew":
                                                    folder = "CastNCrew";
                                                    break;
                                                case "Enemy:SB:Critter":
                                                    folder = "Critter";
                                                    break;
                                                case "Enemy:SB:Dennis":
                                                    folder = "Dennis";
                                                    break;
                                                case "Enemy:SB:FrogFish":
                                                    folder = "FrogFish";
                                                    break;
                                                case "Enemy:SB:Mindy":
                                                    folder = "Mindy";
                                                    break;
                                                case "Enemy:SB:Neptune":
                                                    folder = "Neptune";
                                                    break;
                                                case "Enemy:SB:Standard":
                                                    folder = "Enemy";
                                                    break;
                                                case "Enemy:SB:SupplyCrate":
                                                    folder = "Crate";
                                                    break;
                                                case "Enemy:SB:Turret":
                                                    folder = "Turret";
                                                    break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        throw new Exception($"No parser registered for DYNA type '{dyna.typeNameInternal}'.");
                                    }
                                }
                                else
                                {
                                    folder = Dictionaries.ID_TO_FOLDER_NAME[detectedAssetType];
                                }

                                string safeKey = string.Join("_", key.Split(Path.GetInvalidFileNameChars()));
                                string hipHopContainer = Path.Combine(hipHopFolder, Path.GetFileNameWithoutExtension(modFile.Replace("_assets.json", "")));
                                string outFile = Path.Combine(hipHopContainer, folder, safeKey);

                                if (!Directory.Exists(Path.Combine(hipHopContainer, folder)))
                                {
                                    Directory.CreateDirectory(Path.Combine(hipHopContainer, folder)); //new assets never present wont have folders yet
                                }

                                if (added.Contains(key))
                                {
                                    string iniLine = "";
                                    if (detectedAssetType == "DYNA")
                                        iniLine = "Asset=" + GetAssetId(safeKey).Substring(2) + ";" + folder + ";" + "2" + ";" + "0" + ";" + safeKey.Substring(11) + ";;" + Crc32Mpeg2.Compute(fullBytes).ToString("X8");
                                    else
                                        iniLine = "Asset=" + GetAssetId(safeKey).Substring(2) + ";" + folder + ";" + "2" + ";" + "-1" + ";" + safeKey.Substring(11) + ";;" + Crc32Mpeg2.Compute(fullBytes).ToString("X8");
                                    
                                    Logger.LogInfo(iniLine);

                                    string settingsIni = Path.Combine(hipHopContainer, "Settings.ini");
                                    
                                    var lines = File.ReadAllLines(settingsIni);

                                    var newLines = File.ReadLines(settingsIni) //remove duplicates to prevent crash on packing
                                        .Where(line => !line.StartsWith("Asset=" + GetAssetId(safeKey).Substring(2) + ";"))
                                        .ToArray();
                                    
                                    int start = Array.FindIndex(newLines, l => l == "LayerType=0 DEFAULT");
                                    int end = Array.FindIndex(newLines, start + 1, l => l == "EndLayer");
                                    
                                    var newerLines = newLines
                                        .Take(end)
                                        .Append(iniLine)
                                        .Concat(newLines.Skip(end));

                                    File.WriteAllLines(settingsIni, newerLines);
                                }

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
                        var modElem = ogKeys[key];

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

                        string folder = "";
                        if (detectedAssetType == "DYNA")
                        {
                            DYNA dyna = (DYNA)obj;
                            if (ParserMaps.TryGetDYNAParser(dyna.typeNameInternal, out AbstractDYNAParser parser))
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
                        string assetID = GetAssetId(safeKey).Substring(2);

                        string hipHopContainer = Path.Combine(hipHopFolder, Path.GetFileNameWithoutExtension(modFile.Replace("_assets.json", "")));
                        string outFile = Path.Combine(hipHopContainer, folder, safeKey);

                        string settingsIni = Path.Combine(hipHopContainer, "Settings.ini");

                        IEnumerable<string> iniLines = File.ReadLines(settingsIni);

                        var newLines = File.ReadLines(settingsIni) //remove ghost asset entries
                            .Where(line => !line.StartsWith("Asset=" + assetID + ";"))
                            .ToArray();

                        File.Delete(outFile);
                        File.WriteAllLines(settingsIni, newLines);

                        string dir = Path.Combine(hipHopContainer, folder);

                        if (!Directory.EnumerateFileSystemEntries(dir).Any())
                        {
                            Directory.Delete(dir); //delete if the asset type is never used, to clean up
                        }
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

            uint id = Util.ReadUInt32(header, 0);
            byte baseTypeByte = header[4];
            linkCount = header[5];
            ushort baseFlags = Util.ReadUInt16(header, 6);

            string baseTypeStr = $"0x{baseTypeByte:X2}";

            baseAsset = new xBaseAsset
            {
                id = id,
                baseType = baseTypeStr,
                linkCount = linkCount,
                baseFlags = (BaseFlags)baseFlags
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

        if (CurrentGame == GameType.BFBB)
        {
            br.ReadBytes(4);
        }

        uint surfaceID = Util.ReadUInt32(br.ReadBytes(4), 0);

        xVec3 ang = ToDegrees(new xVec3(
            Util.ReadFloat(br.ReadBytes(4), 0),
            Util.ReadFloat(br.ReadBytes(4), 0),
            Util.ReadFloat(br.ReadBytes(4), 0)
        ));

        xVec3 pos = new xVec3(
            Util.ReadFloat(br.ReadBytes(4), 0),
            Util.ReadFloat(br.ReadBytes(4), 0),
            Util.ReadFloat(br.ReadBytes(4), 0)
        );

        xVec3 scale = new xVec3(
            Util.ReadFloat(br.ReadBytes(4), 0),
            Util.ReadFloat(br.ReadBytes(4), 0),
            Util.ReadFloat(br.ReadBytes(4), 0)
        );

        float redMult = Util.ReadFloat(br.ReadBytes(4), 0);
        float greenMult = Util.ReadFloat(br.ReadBytes(4), 0);
        float blueMult = Util.ReadFloat(br.ReadBytes(4), 0);
        float seeThru = Util.ReadFloat(br.ReadBytes(4), 0);
        float seeThruSpeed = Util.ReadFloat(br.ReadBytes(4), 0);

        uint modelInfoID = Util.ReadUInt32(br.ReadBytes(4), 0);
        uint animListID = Util.ReadUInt32(br.ReadBytes(4), 0);

        return new xEntAsset
        {
            flags = (EntFlags)flags,
            subtype = subtype,
            pflags = pflags,
            moreFlags = (EntFlagsMore)moreFlags,
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

        ushort srcEvent = Util.ReadUInt16(br.ReadBytes(2), 0);
        ushort dstEvent = Util.ReadUInt16(br.ReadBytes(2), 0);
        uint dstAssetID = Util.ReadUInt32(br.ReadBytes(4), 0);

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
            pU32[i] = Util.ReadUInt32(br.ReadBytes(4), 0);
            pF32[i] = BitConverter.Int32BitsToSingle(unchecked((int)pU32[i]));
        }
        
        uint paramWidgetAssetID = Util.ReadUInt32(br.ReadBytes(4), 0);
        uint chkAssetID = Util.ReadUInt32(br.ReadBytes(4), 0);

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

    static bool IsPlatformBigEndian(GamePlatform p)
    {
        return p == GamePlatform.GC;
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