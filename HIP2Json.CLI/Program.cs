using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using static HIP2Json.Program;

namespace HIP2Json.CLI;

class Program
{
    static readonly HashSet<string> SKIP_FILES = new HashSet<string> { "font2", "db05", "b301" };

    static int _totalAssets;
    static int _parsedBase;
    static int _parsedEntity;
    static int _parsedDyna;
    static int _parsedBinary;
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

            int modeCount = ((extractMode || projectMode) ? 1 : 0) + (packMode ? 1 : 0) + (unpackMode ? 1 : 0);

            if (modeCount != 1)
            {
                Logger.LogError("Error: Specify exactly one mode. --extract and --project are the same in-memory project flow (use either, or both); --unpack dumps raw assets; --pack repacks.");
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

    static void ShowUsage()
    {
        string ns = typeof(HIP2Json.Program).Namespace;

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

                            var asm = typeof(HIP2Json.Program).Assembly;
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
                                    Type payloadType = ParserMaps.GetDYNAPayloadType(dyna.typeNameInternal, ns);

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

                        var asm = typeof(HIP2Json.Program).Assembly;
                        string ns = typeof(HIP2Json.Program).Namespace;

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

}
