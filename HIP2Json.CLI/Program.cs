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

    static void Main(string[] args)
    {
        try
        {
            if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
            {
                ShowUsage();
                return;
            }

            bool packMode = args.Contains("--pack") || args.Contains("-k");
            bool rawMode = args.Contains("--save-assets") || args.Contains("--debug");
            bool legacyDump = args.Contains("--unpack") || args.Contains("-u") || args.Contains("--extract") || args.Contains("-e");
            bool showProgress = args.Contains("--progress") || args.Contains("-c");
            bool overwriteFlag = args.Contains("--overwrite") || args.Contains("-o");

            if (legacyDump && !rawMode)
            {
                Logger.LogWarning("--unpack/--extract are deprecated: a project is always built now; raw asset files are ONLY written with --save-assets.");
            }

            bool saveAssets = rawMode || legacyDump;

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

            if (packMode)
            {
                string outputPath = pathArgs.Length > 1 ? pathArgs[1] : GetDefaultPackPath(inputPath);

                RunPack(inputPath, outputPath, overwriteFlag);
            }
            else
            {
                string outputDir = pathArgs.Length > 1 ? pathArgs[1] : GetDefaultProjectPath(inputPath);

                RunProject(inputPath, outputDir, showProgress, saveAssets);
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

    static string GetDefaultProjectPath(string inputPath)
    {
        string folderName = Path.GetFileNameWithoutExtension(inputPath);
        if (File.Exists(inputPath))
        {
            return Path.Combine(Path.GetDirectoryName(inputPath) ?? Directory.GetCurrentDirectory(), folderName + "_proj");
        }

        string fullDir = Path.GetFullPath(inputPath.TrimEnd('/', '\\'));
        return fullDir + "_project";
    }

    static void ShowUsage()
    {
        string ns = typeof(HIP2Json.Program).Namespace;

        Logger.LogInfo("Usage:");
        Logger.LogInfo($"  {ns} <input_path> [output_path] [flags]   build a JSON project (default; ALWAYS makes a project)");
        Logger.LogInfo($"  {ns} --pack <project_path> [-o out.hip]  repack a project folder back into a binary archive");
        Logger.LogInfo("");
        Logger.LogInfo("Input can be a single .hip/.hop archive OR an entire game files directory. A project");
        Logger.LogInfo("(project.json + assets.json + mod_assets.json) is always built in memory; nothing is written");
        Logger.LogInfo("unless you ask: only JSON goes to disk by default, so no raw bytes = 0 MB of dumps.");
        Logger.LogInfo("  single archive -> <archive-name>_proj/   (next to the archive)");
        Logger.LogInfo("  game directory -> <folder>_project/<archive>_proj/   (next to the game folder)");
        Logger.LogInfo("");
        Logger.LogInfo("Flags:");
        Logger.LogInfo("  --save-assets   ALSO write raw per-asset files + Settings.ini to disk under <project>/unpacked/.");
        Logger.LogInfo("                  Opt-in on purpose -- this is the only thing that dumps big game bytes. (alias: --debug)");
        Logger.LogInfo("  --game, -g      Override target game format (BFBB or TSSM). [auto-detected from archive when omitted]");
        Logger.LogInfo("  --platform, -p  Override target platform format (GC, PS2, or XBOX). [auto-detected from archive when omitted]");
        Logger.LogInfo("  --overwrite, -o When packing a *_proj folder, overwrite the original source archive in its own spot (sha-256 verified).");
        Logger.LogInfo("  --progress, -c  Show parsing coverage stats.");
        Logger.LogInfo("  --help, -h      Show this help message.");
        Logger.LogInfo("");
        Logger.LogInfo("Deprecated aliases: --unpack/-u and --extract/-e (now: project + --save-assets), --project/-j (now: the default).");
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

    static void RunProject(string targetPath, string outputDir, bool showProgress, bool saveAssets)
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
            ProcessSingleArchiveProject(targetPath, outputDir, showProgress, saveAssets);
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
                ProcessSingleArchiveProject(file, projectDir, showProgress, saveAssets);
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

    static void LogParseReport()
    {
        Logger.LogInfo("====================================");
        Logger.LogInfo(" PARSE FINAL REPORT");
        Logger.LogInfo("====================================");
        Logger.LogInfo("Project mode builds full projects in memory; no per-asset parse stats are tracked here.");
        Logger.LogInfo("====================================");
    }

    static string GetAssetId(string fileName)
    {
        int start = fileName.LastIndexOf('[');
        int end = fileName.LastIndexOf(']');
        if (start >= 0 && end > start)
            return "0x" + fileName.Substring(start + 1, end - start - 1);
        return null;
    }

}
