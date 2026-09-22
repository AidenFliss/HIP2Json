using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using HIP2Json;
using HIPProg = HIP2Json.Program;

namespace HIP2Json.Tests;

public static class Harvester
{
    static readonly JsonSerializerOptions Opts = new()
    {
        WriteIndented = true,
        IncludeFields = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static int Harvest(string bfbbRoot, string tssmRoot, string outDir, long seed)
    {
        Directory.CreateDirectory(outDir);

        var entries = new List<(string display, string callType, string dynaFilter)>();
        foreach (string t in ParserMaps.AssetToParser.Keys)
        {
            entries.Add((t, t, null));
        }

        foreach (string t in ParserMaps.DYNAToParser.Keys)
        {
            string display = t.Split(':')[^1].ToUpperInvariant();
            entries.Add((display, "DYNA", t));
        }

        entries = entries.Distinct().OrderBy(x => x.display, StringComparer.Ordinal).ToList();

        var seen = new HashSet<string>();
        int harvested = 0, missing = 0;

        foreach ((string display, string callType, string dynaFilter) in entries)
        {
            string code = display;
            foreach ((string gameLabel, string root) in new[] { ("BFBB", bfbbRoot), ("TSSM", tssmRoot) })
            {
                string key = $"{gameLabel}:{code}";
                if (string.IsNullOrWhiteSpace(root))
                {
                    continue;
                }

                string text = HarvestOne(root, code, callType, dynaFilter, gameLabel, seed, key);
                if (text == null)
                {
                    Console.WriteLine($"  MISS  {gameLabel,-5} {code,-8} no archive found");
                    missing++;
                    continue;
                }

                string file = Path.Combine(outDir, $"{gameLabel}.{code}.txt");
                if (seen.Contains(key))
                {
                    file = Path.Combine(outDir, $"{gameLabel}.{code}.{harvested}.txt");
                }

                File.WriteAllText(file, text, new UTF8Encoding(false));
                Console.WriteLine($"  OK    {gameLabel,-5} {code,-8} {Path.GetFileName(file)}");
                seen.Add(key);
                harvested++;
            }
        }

        Console.WriteLine($"harvested={harvested}  missing-cap-archives={missing}");
        return 0;
    }

    static string HarvestOne(string root, string code, string callType, string dynaFilter, string gameLabel, long seed, string key)
    {
        var archives = Directory.GetFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".HIP", StringComparison.OrdinalIgnoreCase)
                     || f.EndsWith(".HOP", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var candidates = new List<(string archive, Section_AHDR ahdr, Game game, Platform platform, byte[] data)>();

        var skipFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "font2", "db05", "b301" };

        foreach (string archive in archives)
        {
            if (skipFiles.Contains(Path.GetFileNameWithoutExtension(archive)))
            {
                continue;
            }

            try
            {
                var (hip, archGame, archPlatform) = HipFile.FromPath(archive);
                if (hip?.DICT?.ATOC?.AHDRList == null)
                {
                    continue;
                }

                foreach (var ahdr in hip.DICT.ATOC.AHDRList)
                {
                    if (ahdr.data == null || ahdr.data.Length == 0)
                    {
                        continue;
                    }

                    string c = ahdr.assetType.GetCode().ToUpperInvariant();

                    bool fits;
                    if (callType == "DYNA")
                    {
                        fits = c.Equals("DYNA", StringComparison.OrdinalIgnoreCase)
                               && ResolvesToSubgroup(ahdr.data, archPlatform, dynaFilter);
                    }
                    else
                    {
                        fits = c == code;
                    }

                    if (!fits)
                    {
                        continue;
                    }

                    candidates.Add((archive, ahdr, archGame, archPlatform, ahdr.data));
                }
            }
            catch
            {
                // skip unreadable archive; never fail the harvest for one container
            }
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        var capped = candidates.Where(c => c.data.Length <= 320000).ToList();
        if (capped.Count > 0)
        {
            candidates = capped;
        }

        var rng = new Random(unchecked((int)(seed ^ key.GetHashCode(StringComparison.Ordinal))));
        var order = Enumerable.Range(0, candidates.Count).OrderBy(_ => rng.Next()).ToArray();

        string lastFail = null;
        foreach (int pick in order)
        {
            var (aPath, chosen, game, platform, data) = candidates[pick];

            HIPProg.CurrentGame = game == Game.BFBB ? GameType.BFBB : GameType.TSSM;
            HIPProg.CurrentPlatform = platform switch
            {
                Platform.GameCube => GamePlatform.GC,
                Platform.PS2 => GamePlatform.PS2,
                Platform.Xbox => GamePlatform.XBOX,
                _ => GamePlatform.GC,
            };
            HIPProg.BigEndian = HIPProg.CurrentPlatform == GamePlatform.GC;

            string assetName = chosen.ADBG?.assetName ?? $"0x{chosen.assetID:X8}";
            ParsedAsset parsed = null;
            try
            {
                parsed = HIPProg.ParseAssetBytes(data, callType, assetName);
            }
            catch (Exception ex)
            {
                lastFail = $"{Path.GetFileName(aPath)} 0x{chosen.assetID:X8}: {ex.GetType().Name}: {ex.Message}";
                continue;
            }

            if (parsed == null)
            {
                lastFail = $"{Path.GetFileName(aPath)} 0x{chosen.assetID:X8}: unparsed";
                continue;
            }

            parsed.Type = code;
            parsed.AssetID = $"0x{chosen.assetID:X8}";
            parsed.AssetName = assetName;
            parsed.AssetFileName = chosen.ADBG?.assetFileName ?? assetName;
            parsed.AssetTypeName = code;
            parsed.AssetChecksum = chosen.ADBG?.checksum ?? 0;
            parsed.AssetFriendlyName = assetName;
            parsed.FileName = assetName;
            JsonElement expected = JsonSerializer.SerializeToElement(parsed, Opts);

            var sb = new StringBuilder();

            sb.AppendLine($"# game={gameLabel} platform={platform} archive={Path.GetFileName(aPath)} asset=0x{chosen.assetID:X8} type={code} call={callType} size={data.Length} checksum={chosen.ADBG?.checksum ?? 0} name={assetName}");
            sb.AppendLine("BLOB:");
            sb.AppendLine(Convert.ToBase64String(data));
            sb.AppendLine("EXPECTED_JSON:");
            sb.AppendLine(expected.ValueKind == JsonValueKind.Undefined ? "null" : expected.GetRawText());
            return sb.ToString();
        }

        Console.WriteLine($"  SKIP  {gameLabel,-5} {code,-8} no parseable candidate (last: {lastFail})");
        return null;
    }

    static bool ResolvesToSubgroup(byte[] data, Platform platform, string dynaFilter)
    {
        if (data.Length < 0x0C || string.IsNullOrEmpty(dynaFilter))
        {
            return false;
        }

        byte[] typeBytes = new byte[] { data[0x08], data[0x09], data[0x0A], data[0x0B] };
        if (platform == Platform.GameCube)
        {
            Array.Reverse(typeBytes);
        }

        string typeHex = BitConverter.ToUInt32(typeBytes, 0).ToString("X8");
        if (!Dictionaries.DYNA_TO_NAME_MAPPING.TryGetValue(typeHex, out string typeName))
        {
            return false;
        }

        if (!ParserMaps.TryGetDYNAParser(typeName, out AbstractDYNAParser candidate))
        {
            return false;
        }

        if (!ParserMaps.TryGetDYNAParser(dynaFilter, out AbstractDYNAParser filter))
        {
            return false;
        }

        return candidate.GetType() == filter.GetType();
    }
}