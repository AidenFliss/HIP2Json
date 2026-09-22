using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HIP2Json;
using HIPProg = HIP2Json.Program;

namespace HIP2Json.Tests;

public static class ArchiveGate
{
    public static int Run(string bfbbRoot, string tssmRoot, string workDir, string extra)
    {
        if (!Directory.Exists(bfbbRoot) && !Directory.Exists(tssmRoot))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ARCHIVE GATE: neither game root exists");
            Console.ResetColor();
            return 2;
        }

        Directory.CreateDirectory(workDir);

        var picks = new List<(string game, string file, string tag)>();
        foreach ((string game, string root) in new[] { ("BFBB", bfbbRoot), ("TSSM", tssmRoot) })
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            {
                continue;
            }

            string boot = Directory.GetFiles(root, "boot.HIP", SearchOption.AllDirectories).FirstOrDefault();
            if (boot != null)
            {
                picks.Add((game, boot, "BOOT"));
            }

            string mostTypes = null;
            int mostTypesCount = -1;
            foreach (string hip in Directory.GetFiles(root, "*.*", SearchOption.AllDirectories)
                         .Where(f => f.EndsWith(".HIP", StringComparison.OrdinalIgnoreCase)
                                  || f.EndsWith(".HOP", StringComparison.OrdinalIgnoreCase)))
            {
                int types = CountDistinctTypes(hip);
                if (types > mostTypesCount)
                {
                    mostTypesCount = types;
                    mostTypes = hip;
                }
            }

            if (mostTypes != null)
            {
                picks.Add((game, mostTypes, $"MOSTTYPES({mostTypesCount})"));
            }
        }

        if (picks.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ARCHIVE GATE: no archives found");
            Console.ResetColor();
            return 2;
        }

        int totalTests = 0, totalFails = 0;
        var failKinds = new Dictionary<FailKind, int>();
        foreach (var fk in Enum.GetValues<FailKind>())
        {
            failKinds[fk] = 0;
        }

        foreach ((string game, string file, string tag) in picks)
        {
            var table = new UnitTest($"{game}.{tag}.{Path.GetFileName(file)}");
            totalTests++;

            try
            {
                RunArchive(table, game, file, workDir);
            }
            catch (Exception ex)
            {
                table.Failures.Add($"archive runner threw: {ex.GetType().Name}: {ex.Message}");
                table.FailKinds.Add(FailKind.Crash);
                table.Failed++;
                table.Passed = 0;
            }

            totalFails += table.Failed;
            foreach (FailKind fk in table.FailKinds)
            {
                failKinds[fk]++;
            }

            bool ok = table.AllPass;
            Console.ForegroundColor = ok ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine($"  {(ok ? "OK  " : "FAIL")}  {game,-4} {tag,-16} asserts={table.Passed + table.Failed} failed={table.Failed}");
            Console.ResetColor();
            foreach (string f in table.Failures)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"        FAIL => {f}");
                Console.ResetColor();
            }
        }

        Console.WriteLine(new string('-', 58));
        Console.WriteLine($"  archive tests: {totalTests}   failed-asserts: {totalFails}");
        Console.WriteLine($"  fail-kind: binary-pack={failKinds[FailKind.BinaryPack]}  serialization(values)={failKinds[FailKind.Serialization]}  crash={failKinds[FailKind.Crash]}  syntax={failKinds[FailKind.Syntax]}");

        bool allOk = totalFails == 0;
        Console.ForegroundColor = allOk ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine(allOk ? "RESULT: ALL PASS  (exit 0)" : $"RESULT: FAILED  (exit 1)");
        Console.ResetColor();
        return allOk ? 0 : 1;
    }

    static void RunArchive(UnitTest table, string game, string file, string workDir)
    {
        var (hip, archGame, archPlatform) = HipFile.FromPath(file);
        HIPProg.CurrentGame = archGame == Game.BFBB ? GameType.BFBB : GameType.TSSM;
        HIPProg.CurrentPlatform = archPlatform switch
        {
            Platform.GameCube => GamePlatform.GC,
            Platform.PS2 => GamePlatform.PS2,
            Platform.Xbox => GamePlatform.XBOX,
            _ => GamePlatform.GC,
        };
        HIPProg.BigEndian = HIPProg.CurrentPlatform == GamePlatform.GC;

        var original = new Dictionary<uint, byte[]>();
        foreach (var ahdr in hip.DICT.ATOC.AHDRList)
        {
            if (ahdr.data != null)
            {
                original[ahdr.assetID] = ahdr.data;
            }
        }

        string projDir = Path.Combine(workDir, $"p_{Path.GetFileName(file)}");

        if (Directory.Exists(projDir))
        {
            Directory.Delete(projDir, true);
        }

        HIPProg.__TestRunProjectSingle(file, projDir);

        string outFile = Path.Combine(workDir, Path.GetFileNameWithoutExtension(file) + ".re.hip");
        if (File.Exists(outFile))
        {
            File.Delete(outFile);
        }

        HIPProg.__TestRunPackProject(projDir, outFile, false);

        var (reHip, reGame, rePlatform) = HipFile.FromPath(outFile);
        HIPProg.CurrentGame = reGame == Game.BFBB ? GameType.BFBB : GameType.TSSM;
        HIPProg.CurrentPlatform = rePlatform switch
        {
            Platform.GameCube => GamePlatform.GC,
            Platform.PS2 => GamePlatform.PS2,
            Platform.Xbox => GamePlatform.XBOX,
            _ => GamePlatform.GC,
        };
        HIPProg.BigEndian = HIPProg.CurrentPlatform == GamePlatform.GC;

        var repacked = new Dictionary<uint, byte[]>();
        foreach (var ahdr in reHip.DICT.ATOC.AHDRList)
        {
            if (ahdr.data != null)
            {
                repacked[ahdr.assetID] = ahdr.data;
            }
        }

        table.Assert(original.Keys.Count == repacked.Keys.Count,
            $"asset count preserved (orig={original.Count} re={repacked.Count})", FailKind.Serialization);

        var missingTypes = new Dictionary<string, int>();
        var hexDumped = new HashSet<string>();
        foreach (var kv in original.OrderBy(k => k.Key))
        {
            if (!repacked.TryGetValue(kv.Key, out byte[] newBytes))
            {
                missingTypes[HipTypeName(hip, kv.Key)] = missingTypes.GetValueOrDefault(HipTypeName(hip, kv.Key)) + 1;
                continue;
            }

            bool same = newBytes.AsSpan().SequenceEqual(kv.Value);
            table.Assert(same, $"asset 0x{kv.Key:X8} bytes match after full project roundtrip",
                FailKind.BinaryPack);
            if (!same)
            {
                int diffAt = -1;
                int n = Math.Min(kv.Value.Length, newBytes.Length);
                for (int k = 0; k < n; k++)
                {
                    if (kv.Value[k] != newBytes[k]) { diffAt = k; break; }
                }

                table.Failures.Add($"      0x{kv.Key:X8} {GetType(hip, kv.Key)} differs at byte {diffAt}  orig={kv.Value.Length}B re={newBytes.Length}B");

                string typeName = GetType(hip, kv.Key);
                if (hexDumped.Add(typeName))
                {
                    table.Failures.Add($"      orig: {HexDump(kv.Value, Math.Max(0, diffAt - 8), 40)}");
                    table.Failures.Add($"      repk: {HexDump(newBytes, Math.Max(0, diffAt - 8), 40)}");
                }
            }
        }

        foreach (var (t, c) in missingTypes.OrderBy(x => x.Key))
        {
            table.Failures.Add($"      asset type {t} MISSING {c} asset(s) after repack");
        }
    }

    static string GetType(HipFile hip, uint id)
    {
        var ahdr = hip.DICT.ATOC.AHDRList.FirstOrDefault(a => a.assetID == id);
        return ahdr?.assetType.ToString() ?? "?";
    }

    static string HipTypeName(HipFile hip, uint id)
    {
        return GetType(hip, id);
    }

    static string HexDump(byte[] b, int start, int len)
    {
        if (b.Length == 0)
        {
            return "(empty)";
        }

        start = Math.Max(0, Math.Min(start, b.Length - 1));
        int take = Math.Min(len, b.Length - start);
        var parts = new List<string>(take);
        for (int i = 0; i < take; i++)
        {
            parts.Add(b[start + i].ToString("X2"));
        }

        return $"@{start} " + string.Join(" ", parts);
    }

    static int CountDistinctTypes(string file)
    {
        try
        {
            var (hip, _, _) = HipFile.FromPath(file);
            if (hip?.DICT?.ATOC?.AHDRList == null)
            {
                return 0;
            }

            return hip.DICT.ATOC.AHDRList.Select(a => a.assetType.ToString()).Distinct().Count();
        }
        catch
        {
            return 0;
        }
    }
}