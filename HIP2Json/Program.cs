using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HIP2Json;

class Program
{
    public static GameType CurrentGame;
    public static GamePlatform CurrentPlatform;
    public static bool BigEndian = true;

    static readonly HashSet<string> BLACKLIST_ASSETS = new HashSet<string> { "BSP", "JSP", "MODL", "TEXS", "ANIM", "SHRP" };
    static readonly HashSet<string> BASE_ASSETS = new HashSet<string>
    {
        "CAM",
        "CCRV",
        "CNTR",
        "COND",
        "CSNM",
        "DPAT",
        "DSCO",
        "DTRK",
        "DUPC",
        "DYNA",
        "ENV",
        "FOG",
        "GRSM",
        "GRUP",
        "GUST",
        "LITE",
        "LOBM",
        "MVPT",
        "NGMS",
        "PARE",
        "PARP",
        "PARS",
        "PGRS",
        "PORT",
        "PRJT",
        "RANM",
        "SCRP",
        "SDFX",
        "SFX",
        "SGRP",
        "SLID",
        "SPLN",
        "SSET",
        "SUBT",
        "SURF",
        "TIMR",
        "TPIK",
        "TRWT",
        "UIM",
        "VOLU",
        "ZLIN",
    };
    static readonly HashSet<string> ENTITY_ASSETS = new HashSet<string> { "BOUL", "BUTN", "DSTR", "EGEN", "HANG", "NPC", "PEND", "PKUP", "PLAT", "PLYR", "SIMP", "TRIG", "UI", "UIFT", "VIL" };

    static readonly HashSet<string> SKIP_FILES = new HashSet<string> { "font2", "db05", "b301" };

    public static bool GameProvided;
    public static bool PlatformProvided;

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
            bool unpackMode = args.Contains("--unpack") || args.Contains("-u");
            bool projectMode = args.Contains("--project") || args.Contains("-j");
            bool showProgress = args.Contains("--progress") || args.Contains("-c");
            bool overwriteFlag = args.Contains("--overwrite") || args.Contains("-o");

            int modeCount = (extractMode ? 1 : 0) + (packMode ? 1 : 0) + (unpackMode ? 1 : 0) + (projectMode ? 1 : 0);

            if (modeCount != 1)
            {
                Logger.LogError("Error: Specify exactly one mode (--extract, --unpack, --pack, or --project).");
                ShowUsage();
                return;
            }

            string gameStr = GetFlagValue(args, "--game", "-g");
            string platformStr = GetFlagValue(args, "--platform", "-p");

            GameProvided = !string.IsNullOrEmpty(gameStr);
            PlatformProvided = !string.IsNullOrEmpty(platformStr);

            if (GameProvided)
            {
                if (!Enum.TryParse<GameType>(gameStr, true, out CurrentGame))
                {
                    Logger.LogError("Error: Invalid game (--game <BFBB|TSSM>).");
                    ShowUsage();
                    return;
                }
            }
            else
            {
                Logger.LogInfo("No --game specified; auto-detecting from archive contents.");
            }

            if (PlatformProvided)
            {
                if (!Enum.TryParse<GamePlatform>(platformStr, true, out CurrentPlatform))
                {
                    Logger.LogError("Error: Invalid platform (--platform <GC|PS2|XBOX>).");
                    ShowUsage();
                    return;
                }
            }
            else
            {
                Logger.LogInfo("No --platform specified; auto-detecting from archive contents.");
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
                string outputDir = pathArgs.Length > 1 ? pathArgs[1] : GetDefaultExtractPath(inputPath);

                RunExtract(inputPath, outputDir, showProgress);
            }
            else if (unpackMode)
            {
                string outputDir = pathArgs.Length > 1 ? pathArgs[1] : GetDefaultUnpackPath(inputPath);

                RunUnpack(inputPath, outputDir);
            }
            else if (projectMode)
            {
                string outputDir = pathArgs.Length > 1 ? pathArgs[1] : GetDefaultProjectPath(inputPath);

                RunProject(inputPath, outputDir, showProgress);
            }
            else
            {
                string outputPath = pathArgs.Length > 1 ? pathArgs[1] : GetDefaultPackPath(inputPath);

                RunPack(inputPath, outputPath, overwriteFlag);
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
            if (string.Equals(args[i], longName, StringComparison.OrdinalIgnoreCase) || string.Equals(args[i], shortName, StringComparison.OrdinalIgnoreCase))
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

    static string GetDefaultUnpackPath(string inputPath)
    {
        string folderName = Path.GetFileNameWithoutExtension(inputPath);
        if (File.Exists(inputPath))
        {
            return Path.Combine(Directory.GetCurrentDirectory(), folderName + "_raw");
        }

        string cleanInput = inputPath.TrimEnd('/', '\\');
        return Path.Combine(Directory.GetCurrentDirectory(), Path.GetFileName(cleanInput) + "_raw");
    }

    static string GetDefaultProjectPath(string inputPath)
    {
        string folderName = Path.GetFileNameWithoutExtension(inputPath);
        if (File.Exists(inputPath))
        {
            return Path.Combine(Path.GetDirectoryName(inputPath) ?? Directory.GetCurrentDirectory(), folderName + "_proj");
        }

        return Path.Combine(Directory.GetCurrentDirectory(), Path.GetFileName(inputPath.TrimEnd('/', '\\')) + "_proj");
    }

    static string GetDefaultPackPath(string inputPath)
    {
        if (File.Exists(inputPath))
            return inputPath;

        string cleanPath = inputPath.TrimEnd('/', '\\');

        if (cleanPath.EndsWith("_unpacked", StringComparison.OrdinalIgnoreCase))
        {
            string filePrefix = Path.GetFileName(cleanPath).Replace("_unpacked", "");

            string ext = filePrefix.EndsWith("_HOP", StringComparison.OrdinalIgnoreCase) ? ".hop" : ".hip";
            string cleanFileName = filePrefix.Replace("_HOP", "").Replace("_HIP", "");

            return Path.Combine(cleanPath, cleanFileName + ext);
        }

        if (cleanPath.EndsWith("_proj", StringComparison.OrdinalIgnoreCase))
            return cleanPath;

        return Path.Combine(cleanPath, "packed");
    }

    static void ShowUsage()
    {
        string ns = typeof(Program).Namespace;

        Logger.LogInfo("Usage:");
        Logger.LogInfo($"  {ns} --unpack  <input_path> [output_path] [options]");
        Logger.LogInfo($"  {ns} --extract <input_path> [output_path] [options]");
        Logger.LogInfo($"  {ns} --project <input_path> [output_path] [options]");
        Logger.LogInfo($"  {ns} --pack    <input_path> [output_path] [options]");
        Logger.LogInfo("");
        Logger.LogInfo("Modes:");
        Logger.LogInfo("  --unpack, -u   Unpack .hip/.hop archive(s) to raw asset files + Settings.ini only (no JSON/repack).");
        Logger.LogInfo("  --extract, -e  Extract a single .hip/.hop archive OR an entire game files directory.");
        Logger.LogInfo("  --project, -j  Build in-memory project from .hip/.hop (project.json + assets.json + mod_assets.json, raw bytes as base64).");
        Logger.LogInfo("  --pack, -k      Pack a project folder (*_unpacked or *_proj) back into binary archive(s).");
        Logger.LogInfo("");
        Logger.LogInfo("Options:");
        Logger.LogInfo("  --game, -g      Specify target game format (BFBB or TSSM). [optional; auto-detected from archive when omitted]");
        Logger.LogInfo("  --platform, -p  Specify target platform format (GC, PS2, or XBOX). [optional; auto-detected from archive when omitted]");
        Logger.LogInfo("  --overwrite, -o When packing a *_proj folder, overwrite the original source archive in its own spot (sha-256 verified).");
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
                if (fileExt != ".hip" && fileExt != ".hop")
                    continue;

                string fileName = Path.GetFileNameWithoutExtension(file);
                if (SKIP_FILES.Contains(fileName))
                    continue;

                string parentFolder = Path.GetFileName(Path.GetDirectoryName(file)!);
                if (parentFolder is "Working" or "New Folder" or "backup")
                    continue;

                ProcessSingleArchiveExtract(file, targetPath, projectDir, showProgress);
            }
        }
        else
        {
            Logger.LogError($"Error: Target path '{targetPath}' does not exist.");
        }

        if (showProgress)
            LogParseReport();
    }

    static void ProcessSingleArchiveExtract(string filePath, string baseDir, string projectDir, bool showProgress)
    {
        string unpackedDir = Path.Combine(projectDir, "unpacked");

        bool isHipFile = Path.GetExtension(filePath).ToLower() == ".hip";
        bool isHopFile = Path.GetExtension(filePath).ToLower() == ".hop";

        string type =
            isHipFile ? "HIP"
            : isHopFile ? "HOP"
            : "Unknown";

        if (type == "Unknown")
            return;

        string parentFolder = Path.GetFileName(Path.GetDirectoryName(filePath)!);

        if (parentFolder is "Working" or "New Folder" or "backup")
            return;

        Logger.LogInfo($"Processing {filePath}...");

        (HipFile hipfile, Game game, Platform platform) = HipFile.FromPath(filePath);

        ResolveGamePlatform(game, platform);

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

            bool shortForm = (parsed.AssetData.TryGetValue("TIMR", out var timrObj) && timrObj is TIMR { ShortForm: true })
                          || (parsed.AssetData.TryGetValue("SURF", out var surfObj) && surfObj is SURF { ShortForm: true });

            if (shortForm)
            {
                try
                {
                    var expandOpts = new JsonSerializerOptions
                    {
                        WriteIndented = false,
                        IncludeFields = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    };
                    JsonElement elem = JsonSerializer.SerializeToElement(parsed, expandOpts);
                    byte[] fullBytes = SerializeModdedAsset(elem, out _, out _);
                    long rawLen = new FileInfo(assetFile).Length;
                    if (fullBytes.Length > rawLen)
                    {
                        File.WriteAllBytes(assetFile, fullBytes);
                        Logger.LogWarning($"Short-form base asset {Path.GetFileName(assetFile)} expanded from {rawLen} to {fullBytes.Length} bytes (full struct)");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Could not expand short-form asset {Path.GetFileName(assetFile)}: {ex.Message}");
                }
            }

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

        string jsonOutputFolderOgPath = isBulkExtract || !string.IsNullOrEmpty(relativeSubFolder) ? Path.Combine(projectDir, "parsed", "og", relativeSubFolder) : Path.Combine(projectDir, "og");

        string jsonOutputFolderModPath = isBulkExtract || !string.IsNullOrEmpty(relativeSubFolder) ? Path.Combine(projectDir, "parsed", "mod", relativeSubFolder) : Path.Combine(projectDir, "mod");

        Directory.CreateDirectory(jsonOutputFolderOgPath);
        Directory.CreateDirectory(jsonOutputFolderModPath);

        string jsonFileName = archiveName + "_assets.json";

        File.WriteAllText(Path.Combine(jsonOutputFolderOgPath, jsonFileName), json);
        File.WriteAllText(Path.Combine(jsonOutputFolderModPath, jsonFileName), json);
    }

    static void RunUnpack(string targetPath, string outputDir)
    {
        if (File.Exists(targetPath))
        {
            string ext = Path.GetExtension(targetPath).ToLower();
            if (ext != ".hip" && ext != ".hop")
            {
                Logger.LogError($"Error: Target file '{targetPath}' is not a .hip or .hop file.");
                return;
            }

            Logger.LogInfo($"Unpacking single archive: {targetPath} -> {outputDir}");
            Directory.CreateDirectory(outputDir);

            ProcessSingleArchiveUnpack(targetPath, outputDir);
        }
        else if (Directory.Exists(targetPath))
        {
            Logger.LogInfo($"Unpacking full directory: {targetPath} -> {outputDir}");
            Directory.CreateDirectory(outputDir);

            foreach (string file in Directory.GetFiles(targetPath, "*.*", SearchOption.AllDirectories))
            {
                string fileExt = Path.GetExtension(file).ToLower();
                if (fileExt != ".hip" && fileExt != ".hop")
                    continue;

                string fileName = Path.GetFileNameWithoutExtension(file);
                if (SKIP_FILES.Contains(fileName))
                    continue;

                string parentFolder = Path.GetFileName(Path.GetDirectoryName(file)!);
                if (parentFolder is "Working" or "New Folder" or "backup")
                    continue;

                ProcessSingleArchiveUnpack(file, outputDir);
            }
        }
        else
        {
            Logger.LogError($"Error: Target path '{targetPath}' does not exist.");
        }
    }

    static void ProcessSingleArchiveUnpack(string filePath, string outputDir)
    {
        try
        {
            (HipFile hipfile, Game game, Platform platform) = HipFile.FromPath(filePath);

            ResolveGamePlatform(game, platform);

            if (game == Game.Unknown)
            {
                game = GetGameFromCli();

                if (game == Game.Unknown)
                {
                    Logger.LogWarning($"Skipping {filePath}: could not determine game. Pass --game <BFBB|TSSM> to override.");
                    return;
                }
            }

            bool isHipFile = Path.GetExtension(filePath).ToLower() == ".hip";
            bool isHopFile = Path.GetExtension(filePath).ToLower() == ".hop";

            string type =
                isHipFile ? "HIP"
                : isHopFile ? "HOP"
                : "Unknown";

            if (type == "Unknown")
                return;

            string archiveName = Path.GetFileNameWithoutExtension(filePath) + "_" + type;
            string extractDir = Path.Combine(outputDir, archiveName);

            hipfile.ToIni(game, extractDir, true, true);

            Logger.LogInfo($"Unpacked {Path.GetFileName(filePath)} -> {extractDir}");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to unpack {filePath}: {ex.Message}");
        }
        finally
        {
            Section_ATOC.noAHDR = false;
        }
    }

    static Game GetGameFromCli()
    {
        if (!GameProvided)
            return Game.Unknown;
        if (CurrentGame == GameType.BFBB)
            return Game.BFBB;
        if (CurrentGame == GameType.TSSM)
            return Game.Incredibles;
        return Game.Unknown;
    }

    static void ResolveGamePlatform(Game game, Platform platform)
    {
        if (!GameProvided)
        {
            CurrentGame = game == Game.BFBB ? GameType.BFBB : GameType.TSSM;
        }

        if (!PlatformProvided)
        {
            CurrentPlatform = platform switch
            {
                Platform.GameCube => GamePlatform.GC,
                Platform.PS2 => GamePlatform.PS2,
                Platform.Xbox => GamePlatform.XBOX,
                _ => CurrentPlatform,
            };
        }

        BigEndian = CurrentPlatform == GamePlatform.GC;
    }

    static string Sha256Hex(string path)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        byte[] hash = sha.ComputeHash(System.IO.File.ReadAllBytes(path));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    static void RunProject(string targetPath, string outputDir, bool showProgress)
    {
        if (File.Exists(targetPath))
        {
            string ext = Path.GetExtension(targetPath).ToLower();
            if (ext != ".hip" && ext != ".hop")
            {
                Logger.LogError($"Error: Target file '{targetPath}' is not a .hip or .hop file.");
                return;
            }

            Directory.CreateDirectory(outputDir);
            Logger.LogInfo($"Building in-memory project for: {targetPath}");
            ProcessSingleArchiveProject(targetPath, outputDir, showProgress);
        }
        else if (Directory.Exists(targetPath))
        {
            Directory.CreateDirectory(outputDir);

            foreach (string file in System.IO.Directory.GetFiles(targetPath, "*.*", SearchOption.AllDirectories))
            {
                string fileExt = Path.GetExtension(file).ToLower();
                if (fileExt != ".hip" && fileExt != ".hop")
                    continue;

                string fileName = Path.GetFileNameWithoutExtension(file);
                if (SKIP_FILES.Contains(fileName))
                    continue;

                string parentFolder = Path.GetFileName(Path.GetDirectoryName(file)!);
                if (parentFolder is "Working" or "New Folder" or "backup")
                    continue;

                string projectDir = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(file) + "_proj");
                Directory.CreateDirectory(projectDir);

                Logger.LogInfo($"Building in-memory project for: {file}");
                ProcessSingleArchiveProject(file, projectDir, showProgress);
            }
        }
        else
        {
            Logger.LogError($"Error: Target path '{targetPath}' does not exist.");
        }

        if (showProgress)
            LogParseReport();
    }

    static void ProcessSingleArchiveProject(string filePath, string projectDir, bool showProgress)
    {
        try
        {
            (HipFile hipfile, Game game, Platform platform) = HipFile.FromPath(filePath);

            ResolveGamePlatform(game, platform);

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                IncludeFields = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            };

            var layers = new List<object>();
            foreach (var LHDR in hipfile.DICT.LTOC.LHDRList)
            {
                layers.Add(
                    new
                    {
                        layerType = LHDR.layerType,
                        ldbg = LHDR.LDBG?.value ?? 0,
                        assetIDs = LHDR.assetIDlist.Select(id => "0x" + id.ToString("X8")),
                    }
                );
            }

            var assets = new List<ParsedAsset>();

            foreach (var AHDR in hipfile.DICT.ATOC.AHDRList)
            {
                string typeCode = AHDR.assetType.GetCode();
                string assetTypeName = AHDR.assetType.ToString();
                string assetID = "0x" + AHDR.assetID.ToString("X8");
                int flags = (int)AHDR.flags;
                int alignment = AHDR.ADBG?.alignment ?? 0;
                int checksum = AHDR.ADBG?.checksum ?? 0;
                string assetName = AHDR.ADBG?.assetName;
                string assetFileName = AHDR.ADBG?.assetFileName;

                ParsedAsset parsed = ParseAssetBytes(AHDR.data, typeCode, assetName);

                ParsedAsset entry;
                if (parsed != null)
                {
                    entry = parsed;
                    parsed.Type = typeCode;
                    parsed.AssetID = assetID;
                    parsed.AssetFlags = flags;
                    parsed.Alignment = alignment;
                    parsed.AssetName = assetName;
                    parsed.AssetFileName = assetFileName;
                    parsed.AssetTypeName = assetTypeName;
                    parsed.AssetChecksum = checksum;
                    parsed.AssetFriendlyName = assetName;
                    parsed.FileName = assetFileName ?? assetName;
                }
                else if (AHDR.data != null)
                {
                    entry = new ParsedAsset
                    {
                        Type = typeCode,
                        AssetID = assetID,
                        AssetFlags = flags,
                        Alignment = alignment,
                        AssetName = assetName,
                        AssetFileName = assetFileName,
                        AssetTypeName = assetTypeName,
                        AssetChecksum = checksum,
                        AssetFriendlyName = assetName,
                        FileName = assetFileName ?? assetName,
                        RawBase64 = Convert.ToBase64String(AHDR.data),
                    };
                }
                else
                {
                    continue;
                }

                assets.Add(entry);
            }

            bool isHip = Path.GetExtension(filePath).ToLower() == ".hip";
            string archiveName = Path.GetFileNameWithoutExtension(filePath) + (isHip ? "_HIP" : "_HOP");
            string archiveExt = isHip ? "hip" : "hop";

            object platObj = null;
            if (hipfile.PACK.PLAT != null)
            {
                platObj = new
                {
                    targetPlatform = hipfile.PACK.PLAT.targetPlatform,
                    targetPlatformName = hipfile.PACK.PLAT.targetPlatformName,
                    regionFormat = hipfile.PACK.PLAT.regionFormat,
                    language = hipfile.PACK.PLAT.language,
                    targetGame = hipfile.PACK.PLAT.targetGame,
                };
            }

            object hipbObj = null;
            if (System.IO.File.ReadAllBytes(filePath).AsSpan().IndexOf("HIPB"u8) >= 0)
            {
                hipbObj = new
                {
                    hasNoLayers = hipfile.HIPB?.HasNoLayers ?? 0,
                    scoobyPlatform = (int)(hipfile.HIPB?.ScoobyPlatform ?? Platform.Unknown),
                    incrediblesGame = (int)(hipfile.HIPB?.IncrediblesGame ?? Game.Unknown),
                    layerNames = hipfile.HIPB.LayerNames ?? new Dictionary<int, string>(),
                };
            }

            string sourceFull = System.IO.Path.GetFullPath(filePath);

            var projectInfo = new Dictionary<string, object>
            {
                { "projectVersion", 1 },
                { "archiveName", archiveName },
                { "archiveType", archiveExt },
                { "sourceFile", sourceFull },
                { "sourceFileSHA256", Sha256Hex(filePath) },
                { "game", game.ToString() },
                { "platform", platform.ToString() },
                { "gameType", CurrentGame.ToString() },
                { "platformType", CurrentPlatform.ToString() },
                { "bigEndian", BigEndian },
                { "pver", new { subVersion = hipfile.PACK.PVER.subVersion, clientVersion = hipfile.PACK.PVER.clientVersion, compatible = hipfile.PACK.PVER.compatible } },
                { "pflg", hipfile.PACK.PFLG.flags },
                { "pcnA", hipfile.PACK.PCNT?.sizeOfLargestSourceFileAsset ?? 0 },
                { "pcnL", hipfile.PACK.PCNT?.sizeOfLargestLayer ?? 0 },
                { "pcnV", hipfile.PACK.PCNT?.sizeOfLargestSourceVirtualAsset ?? 0 },
                { "pcrT", new { fileDate = hipfile.PACK.PCRT.fileDate, dateString = hipfile.PACK.PCRT.dateString } },
                { "pmod", hipfile.PACK.PMOD?.modDate ?? 0 },
                { "plat", platObj },
                { "ainf", hipfile.DICT.ATOC.AINF?.value ?? 0 },
                { "linf", hipfile.DICT.LTOC.LINF?.value ?? 0 },
                { "dhdr", hipfile.STRM.DHDR?.value ?? 0 },
                { "hipb", hipbObj },
                { "layers", layers },
                { "assetCount", assets.Count },
            };

            var projectJson = JsonSerializer.Serialize(projectInfo, jsonOptions);

            File.WriteAllText(Path.Combine(projectDir, "project.json"), projectJson);

            var assetsJson = JsonSerializer.Serialize(assets, jsonOptions);
            File.WriteAllText(Path.Combine(projectDir, "assets.json"), assetsJson);
            File.WriteAllText(Path.Combine(projectDir, "mod_assets.json"), assetsJson);

            Logger.LogInfo($"Wrote project to {projectDir} ({assets.Count} assets)");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to build project for {filePath}: {ex.Message}");
        }
        finally
        {
            Section_ATOC.noAHDR = false;
        }
    }

    internal static byte[] SerializeModdedAsset(JsonElement modElem, out string detectedAssetType, out object obj)
    {
        detectedAssetType = null;
        foreach (var prop in modElem.EnumerateObject())
        {
            if (ParserMaps.AssetToParser.ContainsKey(prop.Name))
            {
                detectedAssetType = prop.Name;
                break;
            }
        }

        if (string.IsNullOrEmpty(detectedAssetType))
            throw new Exception("asset_type_not_detected");

        if (!ParserMaps.AssetToParser.TryGetValue(detectedAssetType, out AssetParser assetParser))
            throw new Exception("no_parser");

        var asm = typeof(Program).Assembly;
        string ns = typeof(Program).Namespace;
        Type targetType = asm.GetType($"{ns}." + detectedAssetType, false, true);

        if (targetType == null)
            throw new Exception("type_not_found");

        var serOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, IncludeFields = true, NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals };
        obj = JsonSerializer.Deserialize(modElem.GetProperty(detectedAssetType).GetRawText(), targetType, serOpts);

        if (obj is DYNA dyna && dyna.dynaSpecificData is JsonElement dynaElem)
        {
            string className = dyna.typeNameInternal.Replace(':', '_');
            Type payloadType = Type.GetType($"{ns}.{className}");

            if (payloadType != null)
                dyna.dynaSpecificData = dynaElem.Deserialize(payloadType, serOpts)!;
        }

        object serialized = assetParser.Serialize(obj);

        if (serialized is byte[] coreBytes)
            return SerializeAssetElement(modElem, detectedAssetType, obj, coreBytes, asm, ns);

        throw new Exception("serialize_returned_non_bytes");
    }

    static void RunPackProject(string projectDir, string outputPath, bool overwriteFlag)
    {
        try
        {
            using var projDoc = JsonDocument.Parse(File.ReadAllText(Path.Combine(projectDir, "project.json")));
            var proj = projDoc.RootElement;

            string sourceFile = proj.GetProperty("sourceFile").GetString();
            string sourceSha = proj.GetProperty("sourceFileSHA256").GetString();
            string archiveName = proj.GetProperty("archiveName").GetString();
            bool isHip = !proj.GetProperty("archiveType").GetString().Equals("hop", StringComparison.OrdinalIgnoreCase);

            Game game = Enum.TryParse<Game>(proj.GetProperty("game").GetString(), true, out var projGame) ? projGame : Game.Unknown;
            Platform platform = Enum.TryParse<Platform>(proj.GetProperty("platform").GetString(), true, out var projPlatform) ? projPlatform : Platform.Unknown;

            if (GameProvided)
                game = GetGameFromCli();
            if (PlatformProvided)
                platform = CurrentPlatform switch
                {
                    GamePlatform.GC => Platform.GameCube,
                    GamePlatform.PS2 => Platform.PS2,
                    _ => Platform.Xbox,
                };

            ResolveGamePlatform(game, platform);

            string layerTypeGame = game == Game.BFBB ? "BFBB" : "TSSM";
            Logger.LogInfo($"Packing project (game={game}, platform={platform})...");

            Section_HIPB newHipb = null;
            if (proj.TryGetProperty("hipb", out var hipbProp) && hipbProp.ValueKind == JsonValueKind.Object)
            {
                newHipb = new Section_HIPB();
                newHipb.HasNoLayers = hipbProp.GetProperty("hasNoLayers").GetInt32();
                if (hipbProp.TryGetProperty("scoobyPlatform", out var scoobyPlatform))
                    newHipb.ScoobyPlatform = (Platform)scoobyPlatform.GetInt32();
                if (hipbProp.TryGetProperty("incrediblesGame", out var incrediblesGame))
                    newHipb.IncrediblesGame = (Game)incrediblesGame.GetInt32();
                if (hipbProp.TryGetProperty("layerNames", out var layerNames) && layerNames.ValueKind == JsonValueKind.Object)
                {
                    foreach (var layerNameProp in layerNames.EnumerateObject())
                    {
                        if (int.TryParse(layerNameProp.Name, out int layerNameIdx))
                            newHipb.LayerNames[layerNameIdx] = layerNameProp.Value.GetString();
                    }
                }
            }

            var hipFile = new HipFile(
                new Section_HIPA(),
                new Section_PACK(),
                new Section_DICT(),
                new Section_STRM(),
                newHipb
            );

            hipFile.PACK.PVER = new Section_PVER(
                proj.GetProperty("pver").GetProperty("subVersion").GetInt32(),
                proj.GetProperty("pver").GetProperty("clientVersion").GetInt32(),
                proj.GetProperty("pver").GetProperty("compatible").GetInt32()
            );
            hipFile.PACK.PFLG = new Section_PFLG(proj.GetProperty("pflg").GetInt32());
            hipFile.PACK.PCRT = new Section_PCRT(proj.GetProperty("pcrT").GetProperty("fileDate").GetInt32(), proj.GetProperty("pcrT").GetProperty("dateString").GetString());
            hipFile.PACK.PMOD = new Section_PMOD(proj.GetProperty("pmod").GetInt32());

            hipFile.PACK.PCNT = new Section_PCNT(
                0,
                0,
                proj.TryGetProperty("pcnA", out var pcnA) ? pcnA.GetInt32() : 0,
                proj.TryGetProperty("pcnL", out var pcnL) ? pcnL.GetInt32() : 0,
                proj.TryGetProperty("pcnV", out var pcnV) ? pcnV.GetInt32() : 0
            );

            if (proj.TryGetProperty("plat", out var platProp) && platProp.ValueKind == JsonValueKind.Object)
            {
                var plat = new Section_PLAT();
                if (platProp.TryGetProperty("targetPlatform", out var tp)) plat.targetPlatform = tp.GetString();
                if (platProp.TryGetProperty("targetPlatformName", out var tpn)) plat.targetPlatformName = tpn.GetString();
                if (platProp.TryGetProperty("regionFormat", out var rf)) plat.regionFormat = rf.GetString();
                if (platProp.TryGetProperty("language", out var la)) plat.language = la.GetString();
                if (platProp.TryGetProperty("targetGame", out var tg)) plat.targetGame = tg.GetString();
                hipFile.PACK.PLAT = plat;
            }

            hipFile.DICT.ATOC.AINF = new Section_AINF(proj.GetProperty("ainf").GetInt32());
            hipFile.DICT.LTOC.LINF = new Section_LINF(proj.GetProperty("linf").GetInt32());
            hipFile.STRM.DHDR = new Section_DHDR(proj.GetProperty("dhdr").GetInt32());

            string assetsFile = File.Exists(Path.Combine(projectDir, "mod_assets.json"))
                ? Path.Combine(projectDir, "mod_assets.json")
                : Path.Combine(projectDir, "assets.json");

            using var assetsDoc = JsonDocument.Parse(File.ReadAllText(assetsFile));
            var root = assetsDoc.RootElement;

            foreach (var layer in proj.GetProperty("layers").EnumerateArray())
            {
                var LHDR = new Section_LHDR
                {
                    layerType = layer.GetProperty("layerType").GetInt32(),
                    assetIDlist = layer.GetProperty("assetIDs").EnumerateArray()
                        .Select(id => Convert.ToUInt32(id.GetString().Substring(2), 16))
                        .ToList(),
                    LDBG = new Section_LDBG(layer.TryGetProperty("ldbg", out var ldbgProp) ? ldbgProp.GetInt32() : 0),
                };
                hipFile.DICT.LTOC.LHDRList.Add(LHDR);
            }

            Dictionary<uint, byte[]> assetDataDictionary = root.EnumerateArray()
                .Select(elem => new
                {
                    id = Convert.ToUInt32(elem.GetProperty("AssetID").GetString().Substring(2), 16),
                    bytes = ResolveAssetBytes(elem),
                })
                .ToDictionary(x => x.id, x => x.bytes);

            foreach (var elem in root.EnumerateArray())
            {
                uint assetID = Convert.ToUInt32(elem.GetProperty("AssetID").GetString().Substring(2), 16);

                if (!assetDataDictionary.TryGetValue(assetID, out byte[] data))
                    continue;

                string assetTypeName = elem.TryGetProperty("AssetTypeName", out var atn) && atn.ValueKind == JsonValueKind.String
                    ? atn.GetString()
                    : elem.TryGetProperty("Type", out var tp) ? tp.GetString() : null;

                if (string.IsNullOrEmpty(assetTypeName))
                {
                    Logger.LogWarning($"Skipping element {assetID:X8}: no asset type name.");
                    continue;
                }

                int flags = elem.TryGetProperty("AssetFlags", out var afl) ? afl.GetInt32() : 0;
                int alignment = elem.TryGetProperty("Alignment", out var al) ? al.GetInt32() : 0;
                string assetName = elem.TryGetProperty("AssetName", out var an) ? an.GetString() : null;
                string assetFileName = elem.TryGetProperty("AssetFileName", out var afn) ? afn.GetString() : null;
                int checksum = unchecked((int)Crc32Mpeg2.Compute(data));

                var ADBG = new Section_ADBG(alignment, assetName, assetFileName ?? assetName, checksum);

                Section_AHDR AHDR;
                try
                {
                    AHDR = new Section_AHDR(assetID, assetTypeName, (AHDRFlags)flags, ADBG);
                }
                catch
                {
                    Logger.LogWarning($"Skipping element {assetID:X8}: unknown asset type '{assetTypeName}'.");
                    continue;
                }

                AHDR.data = data;
                hipFile.DICT.ATOC.AHDRList.Add(AHDR);
            }

            byte[] hipBytes = hipFile.ToBytes(game, platform);

            string finalOutFile;
            string defaultPack = GetDefaultPackPath(projectDir);

            if (outputPath != projectDir && outputPath != defaultPack)
            {
                if (outputPath.EndsWith(".hip", StringComparison.OrdinalIgnoreCase) || outputPath.EndsWith(".hop", StringComparison.OrdinalIgnoreCase))
                {
                    finalOutFile = outputPath;
                }
                else
                {
                    finalOutFile = Path.Combine(outputPath, Path.GetFileName(sourceFile));
                }
            }
            else if (overwriteFlag)
            {
                if (string.IsNullOrEmpty(sourceFile) || !File.Exists(sourceFile))
                {
                    Logger.LogError($"Cannot overwrite: original source '{sourceFile}' not found.");
                    return;
                }

                string currentSha = Sha256Hex(sourceFile);
                if (!string.Equals(currentSha, sourceSha, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.LogError($"Cannot overwrite: original source '{sourceFile}' changed since export (SHA mismatch). Refusing.");
                    return;
                }

                finalOutFile = sourceFile;
            }
            else
            {
                string sourceDir = Path.GetDirectoryName(sourceFile) ?? projectDir;
                string baseName = Path.GetFileNameWithoutExtension(sourceFile);
                finalOutFile = Path.Combine(sourceDir, baseName + ".new." + (isHip ? "hip" : "hop"));
            }

            string parent = Path.GetDirectoryName(finalOutFile);
            if (!string.IsNullOrEmpty(parent))
                Directory.CreateDirectory(parent);

            File.WriteAllBytes(finalOutFile, hipBytes);
            Logger.LogInfo($"Packed project archive to {finalOutFile}");
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to pack project: " + ex);
        }
        finally
        {
            Section_ATOC.noAHDR = false;
        }
    }

    static byte[] ResolveAssetBytes(JsonElement elem)
    {
        if (elem.TryGetProperty("RawBase64", out var raw) && raw.ValueKind == JsonValueKind.String)
            return Convert.FromBase64String(raw.GetString());

        return SerializeModdedAsset(elem, out _, out _);
    }

    static void RunPack(string inputPath, string outputPath, bool overwriteFlag)
    {
        if (!Directory.Exists(inputPath))
        {
            Logger.LogError($"Error: Target directory '{inputPath}' does not exist.");
            return;
        }

        if (File.Exists(Path.Combine(inputPath, "project.json")))
        {
            RunPackProject(inputPath, outputPath, overwriteFlag);
            return;
        }

        List<string> projectSubfolders = Directory.GetDirectories(inputPath)
            .Where(dir => File.Exists(Path.Combine(dir, "project.json")))
            .ToList();

        if (projectSubfolders.Count > 0)
        {
            foreach (string projectSubfolder in projectSubfolders)
                RunPackProject(projectSubfolder, outputPath, overwriteFlag);
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
                (HipFile hipFile, Game game, Platform platform) = HipFile.FromINI(directSettings);
                byte[] hipBytes = hipFile.ToBytes(game, platform);

                string finalOutFile = outputPath.EndsWith(".hip") || outputPath.EndsWith(".hop") ? outputPath : outputPath + ".hip";

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
                if (!File.Exists(settingsPath))
                    continue;

                (HipFile hipFile, Game game, Platform platform) = HipFile.FromINI(settingsPath);
                byte[] hipBytes = hipFile.ToBytes(game, platform);

                string rawName = Path.GetFileName(archiveFolder);
                string outFileName =
                    rawName.EndsWith("_HIP") ? rawName.Replace("_HIP", ".hip")
                    : rawName.EndsWith("_HOP") ? rawName.Replace("_HOP", ".hop")
                    : rawName;

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
                (HipFile hipFile, Game game, Platform platform) = HipFile.FromINI(settingsPath);
                byte[] hipBytes = hipFile.ToBytes(game, platform);

                string finalOutFile = outputPath.EndsWith(".hip") || outputPath.EndsWith(".hop") ? outputPath : outputPath + ".hip";

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
                            structureErrors.Add(
                                new Dictionary<string, object>
                                {
                                    { "key", k },
                                    { "added_props", addedProps },
                                    { "removed_props", removedProps },
                                }
                            );
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
                        Logger.LogInfo($"  Element {e["key"]}: added_props={JsonSerializer.Serialize(e["added_props"])}, removed_props={JsonSerializer.Serialize(e["removed_props"])}");
                    }
                    changes.Add(
                        new Dictionary<string, object>
                        {
                            { "file", rel },
                            { "error", "invalid_structure" },
                            { "details", structureErrors },
                        }
                    );
                }
                else if (added.Count > 0 || removed.Count > 0 || modified.Count > 0)
                {
                    Logger.LogInfo($"Changed: {rel} (added:{added.Count}, removed:{removed.Count}, modified:{modified.Count})");

                    if (added.Count > 0)
                        Logger.LogInfo($"  Added element keys: {string.Join(", ", added)}");
                    if (removed.Count > 0)
                        Logger.LogInfo($"  Removed element keys: {string.Join(", ", removed)}");
                    if (modified.Count > 0)
                        Logger.LogInfo($"  Modified element keys: {string.Join(", ", modified)}");

                    var changeEntry = new Dictionary<string, object>
                    {
                        { "file", rel },
                        { "added", added },
                        { "removed", removed },
                        { "modified", modified },
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
                                serializedOutputs.Add(
                                    new Dictionary<string, object>
                                    {
                                        { "key", key },
                                        { "asset", detectedAssetType },
                                        { "error", "no_parser" },
                                    }
                                );
                                continue;
                            }

                            var asm = typeof(Program).Assembly;
                            string ns = typeof(Program).Namespace;
                            Type targetType = asm.GetType($"{ns}." + detectedAssetType, false, true);

                            if (targetType == null)
                            {
                                serializedOutputs.Add(
                                    new Dictionary<string, object>
                                    {
                                        { "key", key },
                                        { "asset", detectedAssetType },
                                        { "error", "type_not_found" },
                                    }
                                );
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

                                serialized = assetParser.Serialize(obj);
                            }
                            catch (NotImplementedException)
                            {
                                serializedOutputs.Add(
                                    new Dictionary<string, object>
                                    {
                                        { "key", key },
                                        { "asset", detectedAssetType },
                                        { "error", "serialize_not_implemented" },
                                    }
                                );
                                continue;
                            }

                            if (serialized is byte[] coreBytes)
                            {
                                byte[] fullBytes = SerializeAssetElement(modElem, detectedAssetType, obj, coreBytes, asm, ns);

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
                                string archiveName = Path.GetFileNameWithoutExtension(modFile.Replace("_assets.json", ""));

                                string hipHopContainer = File.Exists(Path.Combine(hipHopFolder, "Settings.ini")) ? hipHopFolder : Path.Combine(hipHopFolder, archiveName);

                                string outFile = Path.Combine(hipHopContainer, folder, safeKey);

                                if (!Directory.Exists(Path.Combine(hipHopContainer, folder)))
                                {
                                    Directory.CreateDirectory(Path.Combine(hipHopContainer, folder)); //new assets never present wont have folders yet
                                }

                                if (added.Contains(key))
                                {
                                    string iniLine = "";
                                    if (detectedAssetType == "DYNA")
                                        iniLine =
                                            "Asset="
                                            + GetAssetId(safeKey).Substring(2)
                                            + ";"
                                            + folder
                                            + ";"
                                            + "2"
                                            + ";"
                                            + "0"
                                            + ";"
                                            + safeKey.Substring(11)
                                            + ";;"
                                            + Crc32Mpeg2.Compute(fullBytes).ToString("X8");
                                    else
                                        iniLine =
                                            "Asset="
                                            + GetAssetId(safeKey).Substring(2)
                                            + ";"
                                            + folder
                                            + ";"
                                            + "2"
                                            + ";"
                                            + "-1"
                                            + ";"
                                            + safeKey.Substring(11)
                                            + ";;"
                                            + Crc32Mpeg2.Compute(fullBytes).ToString("X8");

                                    Logger.LogInfo(iniLine);

                                    string settingsIni = Path.Combine(hipHopContainer, "Settings.ini");

                                    var lines = File.ReadAllLines(settingsIni);

                                    var newLines = File.ReadLines(settingsIni) //remove duplicates to prevent crash on packing
                                        .Where(line => !line.StartsWith("Asset=" + GetAssetId(safeKey).Substring(2) + ";"))
                                        .ToArray();

                                    int start = Array.FindIndex(newLines, l => l == "LayerType=0 DEFAULT");
                                    int end = Array.FindIndex(newLines, start + 1, l => l == "EndLayer");

                                    var newerLines = newLines.Take(end).Append(iniLine).Concat(newLines.Skip(end));

                                    File.WriteAllLines(settingsIni, newerLines);
                                }

                                File.WriteAllBytes(outFile, fullBytes);

                                serializedOutputs.Add(
                                    new Dictionary<string, object>
                                    {
                                        { "key", key },
                                        { "asset", detectedAssetType },
                                        { "out_file", outFile },
                                    }
                                );
                            }
                            else
                            {
                                serializedOutputs.Add(
                                    new Dictionary<string, object>
                                    {
                                        { "key", key },
                                        { "asset", detectedAssetType },
                                        { "error", "serialize_returned_non_bytes" },
                                    }
                                );
                            }
                        }
                        catch (Exception ex)
                        {
                            serializedOutputs.Add(
                                new Dictionary<string, object>
                                {
                                    { "key", key },
                                    { "error", "exception" },
                                    { "message", ex.Message },
                                }
                            );
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
                            serializedOutputs.Add(
                                new Dictionary<string, object>
                                {
                                    { "key", key },
                                    { "asset", detectedAssetType },
                                    { "error", "no_parser" },
                                }
                            );
                            continue;
                        }

                        var asm = typeof(Program).Assembly;
                        string ns = typeof(Program).Namespace;

                        Type targetType = asm.GetType($"{ns}." + detectedAssetType, false, true);

                        if (targetType == null)
                        {
                            serializedOutputs.Add(
                                new Dictionary<string, object>
                                {
                                    { "key", key },
                                    { "asset", detectedAssetType },
                                    { "error", "type_not_found" },
                                }
                            );
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
                        string archiveName = Path.GetFileNameWithoutExtension(modFile.Replace("_assets.json", ""));

                        string hipHopContainer = File.Exists(Path.Combine(hipHopFolder, "Settings.ini")) ? hipHopFolder : Path.Combine(hipHopFolder, archiveName);

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
                changes.Add(
                    new Dictionary<string, object>
                    {
                        { "file", rel },
                        { "error", "invalid_json" },
                        { "message", jex.Message },
                    }
                );
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

    static byte[] SerializeAssetElement(JsonElement modElem, string detectedAssetType, object obj, byte[] coreBytes, Assembly asm, string ns)
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
                byte subtype = entProp.TryGetProperty("subtype", out var st) && st.ValueKind == JsonValueKind.Number ? (byte)st.GetUInt32() : (byte)0;
                byte pflags = entProp.TryGetProperty("pflags", out var pf) && pf.ValueKind == JsonValueKind.Number ? (byte)pf.GetUInt32() : (byte)0;
                EntFlagsMore moreFlags = entProp.TryGetProperty("moreFlags", out var mf) ? mf.Deserialize<EntFlagsMore>() : EntFlagsMore.None;

                bw.Write((byte)flags);
                bw.Write(subtype);
                bw.Write(pflags);
                bw.Write((byte)moreFlags);

                if (CurrentGame == GameType.BFBB)
                {
                    bw.Write(new byte[4]);
                }

                uint surfaceID = entProp.TryGetProperty("surfaceID", out var sid) && sid.ValueKind == JsonValueKind.Number ? sid.GetUInt32() : 0u;
                Util.WriteUInt32(bw, surfaceID);

                if (entProp.TryGetProperty("ang", out var ang) && ang.ValueKind == JsonValueKind.Object)
                {
                    float ax = ang.TryGetProperty("x", out var axp) && axp.ValueKind == JsonValueKind.Number ? axp.GetSingle() : 0f;
                    float ay = ang.TryGetProperty("y", out var ayp) && ayp.ValueKind == JsonValueKind.Number ? ayp.GetSingle() : 0f;
                    float az = ang.TryGetProperty("z", out var azp) && azp.ValueKind == JsonValueKind.Number ? azp.GetSingle() : 0f;
                    var rad = ToRadians(new xVec3(ax, ay, az));
                    Util.WriteFloat(bw, rad.x);
                    Util.WriteFloat(bw, rad.y);
                    Util.WriteFloat(bw, rad.z);
                }
                else
                {
                    Util.WriteFloat(bw, 0f);
                    Util.WriteFloat(bw, 0f);
                    Util.WriteFloat(bw, 0f);
                }

                if (entProp.TryGetProperty("pos", out var pos) && pos.ValueKind == JsonValueKind.Object)
                {
                    float px = pos.TryGetProperty("x", out var pxp) && pxp.ValueKind == JsonValueKind.Number ? pxp.GetSingle() : 0f;
                    float py = pos.TryGetProperty("y", out var pyp) && pyp.ValueKind == JsonValueKind.Number ? pyp.GetSingle() : 0f;
                    float pz = pos.TryGetProperty("z", out var pzp) && pzp.ValueKind == JsonValueKind.Number ? pzp.GetSingle() : 0f;
                    Util.WriteFloat(bw, px);
                    Util.WriteFloat(bw, py);
                    Util.WriteFloat(bw, pz);
                }
                else
                {
                    Util.WriteFloat(bw, 0f);
                    Util.WriteFloat(bw, 0f);
                    Util.WriteFloat(bw, 0f);
                }

                if (entProp.TryGetProperty("scale", out var scale) && scale.ValueKind == JsonValueKind.Object)
                {
                    float sx = scale.TryGetProperty("x", out var sxp) && sxp.ValueKind == JsonValueKind.Number ? sxp.GetSingle() : 1f;
                    float sy = scale.TryGetProperty("y", out var syp) && syp.ValueKind == JsonValueKind.Number ? syp.GetSingle() : 1f;
                    float sz = scale.TryGetProperty("z", out var szp) && szp.ValueKind == JsonValueKind.Number ? szp.GetSingle() : 1f;
                    Util.WriteFloat(bw, sx);
                    Util.WriteFloat(bw, sy);
                    Util.WriteFloat(bw, sz);
                }
                else
                {
                    Util.WriteFloat(bw, 1f);
                    Util.WriteFloat(bw, 1f);
                    Util.WriteFloat(bw, 1f);
                }

                float redMult = entProp.TryGetProperty("redMult", out var rm) && rm.ValueKind == JsonValueKind.Number ? rm.GetSingle() : 1f;
                float greenMult = entProp.TryGetProperty("greenMult", out var gm) && gm.ValueKind == JsonValueKind.Number ? gm.GetSingle() : 1f;
                float blueMult = entProp.TryGetProperty("blueMult", out var bm) && bm.ValueKind == JsonValueKind.Number ? bm.GetSingle() : 1f;
                float seeThru = entProp.TryGetProperty("seeThru", out var stt) && stt.ValueKind == JsonValueKind.Number ? stt.GetSingle() : 0f;
                float seeThruSpeed = entProp.TryGetProperty("seeThruSpeed", out var sts) && sts.ValueKind == JsonValueKind.Number ? sts.GetSingle() : 0f;

                Util.WriteFloat(bw, redMult);
                Util.WriteFloat(bw, greenMult);
                Util.WriteFloat(bw, blueMult);
                Util.WriteFloat(bw, seeThru);
                Util.WriteFloat(bw, seeThruSpeed);

                uint modelInfoID = 0;
                if (entProp.TryGetProperty("modelInfoID", out var mid) && mid.ValueKind == JsonValueKind.String && mid.GetString().StartsWith("0x"))
                    modelInfoID = Convert.ToUInt32(mid.GetString().Substring(2), 16);
                else if (entProp.TryGetProperty("modelInfoID", out var mid2) && mid2.ValueKind == JsonValueKind.Number)
                    modelInfoID = mid2.GetUInt32();
                uint animListID = 0;
                if (entProp.TryGetProperty("animListID", out var aid) && aid.ValueKind == JsonValueKind.String && aid.GetString().StartsWith("0x"))
                    animListID = Convert.ToUInt32(aid.GetString().Substring(2), 16);
                else if (entProp.TryGetProperty("animListID", out var aid2) && aid2.ValueKind == JsonValueKind.Number)
                    animListID = aid2.GetUInt32();

                Util.WriteUInt32(bw, modelInfoID);
                Util.WriteUInt32(bw, animListID);
            }

            if (detectedAssetType != "PLYR")
                bw.Write(coreBytes);

            Type eventType = CurrentGame switch
            {
                GameType.BFBB => asm.GetType($"{ns}.EventBFBB", true, true),
                GameType.TSSM => asm.GetType($"{ns}.EventTSSM", true, true),
                _ => throw new Exception($"Unsupported game type: {CurrentGame}"),
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
                    while (ms.Position < aligned)
                        bw.Write((byte)0);

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
                    if (link.TryGetProperty("dstAssetID", out var da) && da.ValueKind == JsonValueKind.String && da.GetString().StartsWith("0x"))
                        dstAssetID = Convert.ToUInt32(da.GetString().Substring(2), 16);
                    else if (link.TryGetProperty("dstAssetID", out var da2) && da2.ValueKind == JsonValueKind.Number)
                        dstAssetID = da2.GetUInt32();
                    Util.WriteUInt32(bw, dstAssetID);

                    uint[] pU32 = new uint[4];
                    if (link.TryGetProperty("paramU32", out var pu) && pu.ValueKind == JsonValueKind.Array)
                    {
                        int i = 0;
                        foreach (var item in pu.EnumerateArray())
                        {
                            if (i >= 4)
                                break;
                            if (item.ValueKind == JsonValueKind.String && item.GetString().StartsWith("0x"))
                                pU32[i] = Convert.ToUInt32(item.GetString().Substring(2), 16);
                            else if (item.ValueKind == JsonValueKind.Number)
                                pU32[i] = item.GetUInt32();
                            i++;
                        }
                    }
                    else if (link.TryGetProperty("paramF32", out var pf) && pf.ValueKind == JsonValueKind.Array)
                    {
                        int i = 0;
                        foreach (var item in pf.EnumerateArray())
                        {
                            if (i >= 4)
                                break;
                            float fv = item.ValueKind == JsonValueKind.Number ? item.GetSingle() : 0f;
                            if (float.IsFinite(fv)) //can have coincidental NaN as a float interpretation... thx for no metadata :heavyironmoment:
                            {
                                int iv = BitConverter.SingleToInt32Bits(fv);
                                pU32[i] = unchecked((uint)iv);
                                i++;
                            }
                        }
                    }

                    for (int i = 0; i < 4; i++)
                        Util.WriteUInt32(bw, pU32[i]);

                    linkIndex++;

                    uint paramWidgetAssetID = 0;
                    if (link.TryGetProperty("paramWidgetAssetID", out var pw) && pw.ValueKind == JsonValueKind.String && pw.GetString().StartsWith("0x"))
                        paramWidgetAssetID = Convert.ToUInt32(pw.GetString().Substring(2), 16);
                    else if (link.TryGetProperty("paramWidgetAssetID", out var pw2) && pw2.ValueKind == JsonValueKind.Number)
                        paramWidgetAssetID = pw2.GetUInt32();
                    uint chkAssetID = 0;
                    if (link.TryGetProperty("chkAssetID", out var ck) && ck.ValueKind == JsonValueKind.String && ck.GetString().StartsWith("0x"))
                        chkAssetID = Convert.ToUInt32(ck.GetString().Substring(2), 16);
                    else if (link.TryGetProperty("chkAssetID", out var ck2) && ck2.ValueKind == JsonValueKind.Number)
                        chkAssetID = ck2.GetUInt32();

                    Util.WriteUInt32(bw, paramWidgetAssetID);
                    Util.WriteUInt32(bw, chkAssetID);
                }
            }

            if (detectedAssetType == "PLYR")
                bw.Write(coreBytes);

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

            fullBytes = ms.ToArray();
        }
        return fullBytes;
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

            assetDescriptor = new AssetDescriptor() { AssetType = "DYNA", AssetStorage = AssetStorage.Base };
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

        if (assetDescriptor.AssetStorage != AssetStorage.Binary)
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
                baseFlags = (BaseFlags)baseFlags,
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

        if (assetDescriptor.AssetStorage == AssetStorage.Entity)
        {
            ent = ParseEntityChunk(br);
        }

        long dataStart = br.BaseStream.Position;

        if (ParserMaps.AssetToParser.TryGetValue(assetType, out AssetParser parser))
        {
            object parsed;
            try
            {
                parsed = parser.Parse(br, assetStart, dataStart);
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Could not parse {Path.GetFileName(filePath)} as {assetType}: {ex.Message}. Keeping raw bytes.");
                return null;
            }

            additionalData[assetType] = parsed;

            if (assetDescriptor.AssetStorage != AssetStorage.Binary)
            {
                long linksOffset = parser.GetLinksOffset(br, linkCount);

                br.BaseStream.Seek(linksOffset, SeekOrigin.Begin);

                links = new xLinkAsset[linkCount];

                for (int i = 0; i < linkCount; i++)
                    links[i] = ReadLinkAsset(br);
            }

            if (category == "binary")
                _parsedBinary++;
            if (category == "dyna")
                _parsedDyna++;
            if (category == "base")
                _parsedBase++;
            if (category == "entity")
                _parsedEntity++;
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
            AssetData = additionalData,
        };
    }

    internal static ParsedAsset ParseAssetBytes(byte[] data, string assetType, string assetName)
    {
        if (data == null || data.Length == 0)
            return null;

        if (BLACKLIST_ASSETS.Contains(assetType))
            return null;

        AssetDescriptor assetDescriptor = new AssetDescriptor
        {
            AssetType = assetType,
            AssetStorage =
                ENTITY_ASSETS.Contains(assetType) ? AssetStorage.Entity
                : BASE_ASSETS.Contains(assetType) ? AssetStorage.Base
                : AssetStorage.Binary,
        };

        using var ms = new MemoryStream(data);
        using var br = new BinaryReader(ms);

        long assetStart = br.BaseStream.Position;

        byte linkCount = 0;

        xBaseAsset? baseAsset = null;

        if (assetDescriptor.AssetStorage != AssetStorage.Binary)
        {
            byte[] header = br.ReadBytes(8);
            if (header.Length < 8)
                return null;

            uint id = Util.ReadUInt32(header, 0);
            byte baseTypeByte = header[4];
            linkCount = header[5];
            ushort baseFlags = Util.ReadUInt16(header, 6);

            baseAsset = new xBaseAsset
            {
                id = id,
                baseType = $"0x{baseTypeByte:X2}",
                linkCount = linkCount,
                baseFlags = (BaseFlags)baseFlags,
            };
        }

        Dictionary<string, object> additionalData = new();

        xLinkAsset[] links = Array.Empty<xLinkAsset>();
        xEntAsset? ent = null;

        if (assetDescriptor.AssetStorage == AssetStorage.Entity)
        {
            ent = ParseEntityChunk(br);
        }

        long dataStart = br.BaseStream.Position;

        if (ParserMaps.AssetToParser.TryGetValue(assetType, out AssetParser parser))
        {
            object parsed;
            try
            {
                parsed = parser.Parse(br, assetStart, dataStart);
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Could not parse {assetName} as {assetType}: {ex.Message}");
                return null;
            }

            if (assetType == "DYNA" && parsed is DYNA dyna && dyna.typeNameInternal == null)
                return null;

            additionalData[assetType] = parsed;

            if (assetDescriptor.AssetStorage != AssetStorage.Binary)
            {
                long linksOffset = parser.GetLinksOffset(br, linkCount);

                br.BaseStream.Seek(linksOffset, SeekOrigin.Begin);

                links = new xLinkAsset[linkCount];

                for (int i = 0; i < linkCount; i++)
                    links[i] = ReadLinkAsset(br);
            }
        }
        else
        {
            return null;
        }

        return new ParsedAsset
        {
            Base = baseAsset,
            Links = links,
            Entity = ent,
            AssetData = additionalData,
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

        foreach (var kvp in _unimplByType.OrderByDescending(x => x.Value))
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

        xVec3 ang = ToDegrees(new xVec3(Util.ReadFloat(br.ReadBytes(4), 0), Util.ReadFloat(br.ReadBytes(4), 0), Util.ReadFloat(br.ReadBytes(4), 0)));

        xVec3 pos = new xVec3(Util.ReadFloat(br.ReadBytes(4), 0), Util.ReadFloat(br.ReadBytes(4), 0), Util.ReadFloat(br.ReadBytes(4), 0));

        xVec3 scale = new xVec3(Util.ReadFloat(br.ReadBytes(4), 0), Util.ReadFloat(br.ReadBytes(4), 0), Util.ReadFloat(br.ReadBytes(4), 0));

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
            animListID = animListID,
        };
    }

    static xLinkAsset ReadLinkAsset(BinaryReader br)
    {
        long p = br.BaseStream.Position;
        long aligned = (p + 3) & ~3;
        if (aligned != p)
            br.BaseStream.Position = aligned;

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
            chkAssetID = chkAssetID,
        };
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

    public static xVec3 ToDegrees(xVec3 radiansVec)
    {
        const double Rad2Deg = 180.0 / Math.PI;
        return new xVec3((float)(radiansVec.x * Rad2Deg), (float)(radiansVec.y * Rad2Deg), (float)(radiansVec.z * Rad2Deg));
    }

    public static xVec3 ToRadians(xVec3 degreesVec)
    {
        const double Deg2Rad = Math.PI / 180.0;
        return new xVec3((float)(degreesVec.x * Deg2Rad), (float)(degreesVec.y * Deg2Rad), (float)(degreesVec.z * Deg2Rad));
    }
}
