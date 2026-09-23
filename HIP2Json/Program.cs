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

    internal static readonly HashSet<string> BLACKLIST_ASSETS = new HashSet<string> { "BSP", "JSP", "MODL", "TEXS", "ANIM", "SHRP" };
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
    public static bool GameProvided;
    public static bool PlatformProvided;
    public static int _unimplemented;
    public static Dictionary<string, int> _unimplByType = new();
    internal static string GetDefaultPackPath(string inputPath)
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

    internal static Game GetGameFromCli()
    {
        if (!GameProvided)
            return Game.Unknown;
        if (CurrentGame == GameType.BFBB)
            return Game.BFBB;
        if (CurrentGame == GameType.TSSM)
            return Game.Incredibles;
        return Game.Unknown;
    }

    internal static void ResolveGamePlatform(Game game, Platform platform)
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

    internal static void ProcessSingleArchiveProject(string filePath, string projectDir, bool showProgress, bool saveAssets = false)
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

            ApplySoundRatesFromSndi(assets);

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

            if (saveAssets)
            {
                try
                {
                    string rawDir = Path.Combine(projectDir, "unpacked", archiveName);
                    hipfile.ToIni(game, rawDir, true, true);
                    Logger.LogWarning($"--save-assets: wrote raw per-asset files to {rawDir}");
                }
                catch (Exception rawEx)
                {
                    Logger.LogWarning($"--save-assets raw dump failed: {rawEx.Message}");
                }
            }

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

    private static void ApplySoundRatesFromSndi(List<ParsedAsset> assets)
    {
        Dictionary<string, uint> rates = new Dictionary<string, uint>();

        foreach (ParsedAsset asset in assets)
        {
            if (asset.AssetData.TryGetValue("SNDI", out object sndiObj) && sndiObj is SNDI sndi)
            {
                foreach (SNDIEntry entry in sndi.entries ?? Array.Empty<SNDIEntry>())
                {
                    string key = "0x" + entry.assetID.ToString("X8");
                    if (!rates.ContainsKey(key))
                        rates[key] = entry.sampleRate;
                }
            }
        }

        if (rates.Count == 0)
            return;

        foreach (ParsedAsset asset in assets)
        {
            if (!asset.AssetData.TryGetValue("SND", out object sndObj) && !asset.AssetData.TryGetValue("SNDS", out sndObj))
                continue;

            if (sndObj is not SoundAssetBase snd)
                continue;

            if (!rates.TryGetValue(asset.AssetID, out uint rate))
                continue;

            if (snd.sampleRate == (int)rate)
                continue;

            snd.sampleRate = (int)rate;

            if (!string.IsNullOrEmpty(snd.wavBase64))
            {
                short[] samples = WavCodec.Decode(Convert.FromBase64String(snd.wavBase64), out _);
                snd.wavBase64 = Convert.ToBase64String(WavCodec.Encode(samples, (int)rate));
            }
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
            Type payloadType = ParserMaps.GetDYNAPayloadType(dyna.typeNameInternal, ns);

            if (payloadType != null)
                dyna.dynaSpecificData = dynaElem.Deserialize(payloadType, serOpts)!;
        }

        object serialized = assetParser.Serialize(obj);

        if (serialized is byte[] coreBytes)
            return SerializeAssetElement(modElem, detectedAssetType, obj, coreBytes, asm, ns);

        throw new Exception("serialize_returned_non_bytes");
    }

    internal static void RunPackProject(string projectDir, string outputPath, bool overwriteFlag)
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

    internal static void __TestRunProjectSingle(string filePath, string projectDir)
    {
        Directory.CreateDirectory(projectDir);
        ProcessSingleArchiveProject(filePath, projectDir, false);
    }

    internal static void __TestRunPackProject(string projectDir, string outputPath, bool overwriteFlag)
    {
        RunPackProject(projectDir, outputPath, overwriteFlag);
    }

    static byte[] ResolveAssetBytes(JsonElement elem)
    {
        if (elem.TryGetProperty("RawBase64", out var raw) && raw.ValueKind == JsonValueKind.String)
            return Convert.FromBase64String(raw.GetString());

        return SerializeModdedAsset(elem, out _, out _);
    }

    internal static byte[] SerializeAssetElement(JsonElement modElem, string detectedAssetType, object obj, byte[] coreBytes, Assembly asm, string ns)
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
                else if (baseProp.TryGetProperty("baseFlags", out bf) && bf.ValueKind == JsonValueKind.Number)
                    baseFlags = bf.GetUInt16();

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
                    Util.WriteFloat(bw, ax);
                    Util.WriteFloat(bw, ay);
                    Util.WriteFloat(bw, az);
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

    public static byte[] SerializeParsedAsset(ParsedAsset parsed)
    {
        string detectedAssetType = null;
        foreach (KeyValuePair<string, object> pair in parsed.AssetData)
        {
            if (ParserMaps.AssetToParser.ContainsKey(pair.Key))
            {
                detectedAssetType = pair.Key;
                break;
            }
        }

        if (string.IsNullOrEmpty(detectedAssetType))
            throw new Exception("asset_type_not_detected");

        if (!ParserMaps.AssetToParser.TryGetValue(detectedAssetType, out AssetParser assetParser))
            throw new Exception("no_parser");

        object obj = parsed.AssetData[detectedAssetType];
        object serialized = assetParser.Serialize(obj);

        if (serialized is not byte[] coreBytes)
            throw new Exception("serialize_returned_non_bytes");

        byte[] fullBytes;
        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            if (parsed.Base != null)
            {
                xBaseAsset baseAsset = parsed.Base.Value;
                uint idVal = baseAsset.id;
                byte baseTypeByte = 0;
                string baseTypeStr = baseAsset.baseType;
                if (!string.IsNullOrEmpty(baseTypeStr) && baseTypeStr.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    baseTypeByte = Convert.ToByte(baseTypeStr.Substring(2), 16);
                else if (!string.IsNullOrEmpty(baseTypeStr))
                    baseTypeByte = Convert.ToByte(Convert.ToInt32(baseTypeStr));

                byte linkCountByte = (byte)(parsed.Links?.Length ?? 0);
                ushort baseFlags = (ushort)baseAsset.baseFlags;

                Util.WriteUInt32(bw, idVal);
                bw.Write(baseTypeByte);
                bw.Write(linkCountByte);
                Util.WriteUInt16(bw, baseFlags);
            }

            if (parsed.Entity != null)
            {
                xEntAsset ent = parsed.Entity.Value;

                bw.Write((byte)ent.flags);
                bw.Write(ent.subtype);
                bw.Write(ent.pflags);
                bw.Write((byte)ent.moreFlags);

                if (CurrentGame == GameType.BFBB)
                    bw.Write(new byte[4]);

                Util.WriteUInt32(bw, ent.surfaceID);

                Util.WriteFloat(bw, ent.ang.x);
                Util.WriteFloat(bw, ent.ang.y);
                Util.WriteFloat(bw, ent.ang.z);

                Util.WriteFloat(bw, ent.pos.x);
                Util.WriteFloat(bw, ent.pos.y);
                Util.WriteFloat(bw, ent.pos.z);

                Util.WriteFloat(bw, ent.scale.x);
                Util.WriteFloat(bw, ent.scale.y);
                Util.WriteFloat(bw, ent.scale.z);

                Util.WriteFloat(bw, ent.redMult);
                Util.WriteFloat(bw, ent.greenMult);
                Util.WriteFloat(bw, ent.blueMult);
                Util.WriteFloat(bw, ent.seeThru);
                Util.WriteFloat(bw, ent.seeThruSpeed);

                Util.WriteUInt32(bw, ent.modelInfoID);
                Util.WriteUInt32(bw, ent.animListID);
            }

            if (detectedAssetType != "PLYR")
                bw.Write(coreBytes);

            Type eventType = CurrentGame switch
            {
                GameType.BFBB => typeof(EventBFBB),
                GameType.TSSM => typeof(EventTSSM),
                _ => throw new Exception($"Unsupported game type: {CurrentGame}"),
            };

            if (parsed.Links != null)
            {
                foreach (xLinkAsset link in parsed.Links)
                {
                    long p = ms.Position;
                    long aligned = (p + 3) & ~3L;
                    while (ms.Position < aligned)
                        bw.Write((byte)0);

                    ushort srcEventCode = 0;
                    ushort dstEventCode = 0;

                    if (!string.IsNullOrEmpty(link.srcEvent))
                    {
                        try
                        {
                            object parsedEvent = Enum.Parse(eventType, link.srcEvent, ignoreCase: true);
                            srcEventCode = Convert.ToUInt16(parsedEvent);
                        }
                        catch
                        {
                            throw new Exception($"Unknown srcEvent type alignment failure: {link.srcEvent}");
                        }
                    }

                    if (!string.IsNullOrEmpty(link.dstEvent))
                    {
                        try
                        {
                            object parsedEvent = Enum.Parse(eventType, link.dstEvent, ignoreCase: true);
                            dstEventCode = Convert.ToUInt16(parsedEvent);
                        }
                        catch
                        {
                            throw new Exception($"Unknown dstEvent type alignment failure: {link.dstEvent}");
                        }
                    }

                    Util.WriteUInt16(bw, srcEventCode);
                    Util.WriteUInt16(bw, dstEventCode);
                    Util.WriteUInt32(bw, link.dstAssetID);

                    uint[] pU32 = new uint[4];
                    if (link.paramU32 != null)
                    {
                        for (int i = 0; i < link.paramU32.Length && i < 4; i++)
                            pU32[i] = link.paramU32[i];
                    }
                    else if (link.paramF32 != null)
                    {
                        int i = 0;
                        foreach (float fv in link.paramF32)
                        {
                            if (i >= 4)
                                break;
                            if (float.IsFinite(fv))
                            {
                                int iv = BitConverter.SingleToInt32Bits(fv);
                                pU32[i] = unchecked((uint)iv);
                                i++;
                            }
                        }
                    }

                    for (int i = 0; i < 4; i++)
                        Util.WriteUInt32(bw, pU32[i]);

                    Util.WriteUInt32(bw, link.paramWidgetAssetID);
                    Util.WriteUInt32(bw, link.chkAssetID);
                }
            }

            if (detectedAssetType == "PLYR")
                bw.Write(coreBytes);

            if (detectedAssetType == "TRIG")
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

    internal static string GetCategory(string assetType)
    {
        if (ENTITY_ASSETS.Contains(assetType))
            return "entity";

        if (assetType.Contains("dyna", StringComparison.OrdinalIgnoreCase))
            return "dyna";

        if (BASE_ASSETS.Contains(assetType))
            return "base";

        return "binary";
    }

    internal static xEntAsset ParseEntityChunk(BinaryReader br)
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

        xVec3 ang = new xVec3(Util.ReadFloat(br.ReadBytes(4), 0), Util.ReadFloat(br.ReadBytes(4), 0), Util.ReadFloat(br.ReadBytes(4), 0));

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

    internal static xLinkAsset ReadLinkAsset(BinaryReader br)
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
}
