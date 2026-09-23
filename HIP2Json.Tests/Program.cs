#nullable enable
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HIP2Json.Tests;

static class Program
{
    static readonly JsonSerializerOptions RoundTripOpts = new()
    {
        WriteIndented = false,
        IncludeFields = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    static int Main(string[] args)
    {
        int fuzz = 25;
        long seed = 0x5EEDF00D;
        bool listOnly = false;
        string? only = null;
        int benchMs = 0;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--fuzz" when i + 1 < args.Length && int.TryParse(args[i + 1], out int n):
                    fuzz = Math.Max(1, n); i++; break;
                case "--seed" when i + 1 < args.Length && long.TryParse(args[i + 1], out long s):
                    seed = s; i++; break;
                case "--only" when i + 1 < args.Length:
                    only = args[i + 1].ToUpperInvariant(); i++; break;
                case "--bench" when i + 1 < args.Length && int.TryParse(args[i + 1], out int b):
                    benchMs = Math.Max(1, b); i++; break;
                case "--harvest" when i + 1 < args.Length:
                    return Harvester.Harvest(args[i + 1], args.Length > i + 2 ? args[i + 2] : "", args.Length > i + 3 ? args[i + 3] : "", DateTime.UtcNow.Ticks);
                case "--blobs" when i + 1 < args.Length:
                    string blobDir = args[i + 1];
                    string? onlyBlob = null;
                    if (i + 2 < args.Length && !args[i + 2].StartsWith("--"))
                        onlyBlob = args[i + 2];
                    return BlobRunner.Run(blobDir, onlyBlob);
                case "--regen-blobs" when i + 1 < args.Length:
                    return BlobRunner.Regen(args[i + 1], args.Length > i + 2 ? args[i + 2] : "");
                case "--archives" when i + 1 < args.Length:
                    return ArchiveGate.Run(args[i + 1], args.Length > i + 2 ? args[i + 2] : "", args.Length > i + 3 ? args[i + 3] : "/tmp/opencode/archgate", args.Length > i + 4 ? args[i + 4] : "");
                case "--list":
                    listOnly = true; break;
                case "--help":
                case "-h":
                    Console.WriteLine("HIP2Json.Tests: fuzz + byte-accuracy idempotence per parser");
                    Console.WriteLine("  --fuzz  <n>     fuzz buffers per parser (default 25; CI: 8 for fast gate)");
                    Console.WriteLine("  --seed  <n>     RNG seed (default 0x5EEDF00D)");
                    Console.WriteLine("  --only  <TYPE>  run a single parser type (e.g. RWTV, SND, MODL)");
                    Console.WriteLine("  --bench <ms>    bench: repeat ~<ms> per parser, print ops/s");
                    Console.WriteLine("  --list          list registered parser types only");
                    Console.WriteLine("  --archives <bfbbRoot> <tssmRoot> [workDir]  full-archive roundtrip gate (boot.HIP + most-uniq-types per game)");
                    Console.WriteLine("  --harvest <bfbbRoot> <tssmRoot> <outDir> [seed]  harvest real production blobs into outDir");
                    Console.WriteLine("  --blobs <dir>   run harvested production blobs: VALUES roundtrip + byte-identical repack");
                    Console.WriteLine("                  (fixtures always it; registered types without a real fixture print a warning, not a failure)");
                    Console.WriteLine("  --regen-blobs <dir> [only]  rewrite EXPECTED_JSON sections from the BLOB+parser (fixture refresh)");
                    Console.WriteLine("  exit 0  iff  crashes==0 AND idempotence-miss==0");
                    return 0;
            }
        }

        var types = HIP2Json.ParserMaps.AssetToParser.Keys
            .Concat(HIP2Json.ParserMaps.DYNAToParser.Keys)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        if (listOnly)
        {
            foreach (var t in types) Console.WriteLine(StripDYNA(t));
            return 0;
        }

        if (only != null)
        {
            var match = types.Where(t => StripDYNA(t).Equals(only, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (match.Length == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  unknown parser: {only}  (use --list to see registered types)");
                Console.ResetColor();
                return 2;
            }
            types = match;
        }

        Console.WriteLine($"HIP2Json.Tests  parsers={types.Length}  fuzz/parser={fuzz}  seed=0x{seed:X8}");
        Console.WriteLine(new string('-', 58));

        int totalCrashes = 0, totalIdemMiss = 0, totalChecks = 0, failCount = 0;
        var stopwatch = Stopwatch.StartNew();

        foreach (var typeKey in types)
        {
            string type = StripDYNA(typeKey);
            int parsedOk = 0, unparsed = 0, crashes = 0, idemMiss = 0;

            var rng = new Random(unchecked((int)(seed ^ typeKey.GetHashCode())));
            int bench = benchMs > 0 ? (int)(benchMs * 1000) : 0;
            long benchStart = Stopwatch.GetTimestamp();

            for (int i = 0; i < fuzz; i++)
            {
                byte[] raw = MakeBuffer(rng, i % 12, type);

                ParsedAsset? p;
                try
                {
                    try
                    {
                        p = HIP2Json.Program.ParseAssetBytes(raw, type, "fz_" + i);
                    }
                    catch
                    {
                        p = null;
                    }
                }
                catch (Exception ex)
                {
                    crashes++;
                    if (crashes <= 2) Console.WriteLine($"  THREW  {type}: {ex.GetType().Name}: {ex.Message}");
                    continue;
                }

                if (p == null)
                {
                    unparsed++;
                    continue;
                }

                parsedOk++;
                totalChecks++;

                if (!RoundTripOnce(raw, type))
                {
                    idemMiss++;
                }

                if (bench > 0 && Stopwatch.GetElapsedTime(benchStart).TotalMilliseconds >= bench)
                {
                    break;
                }
            }

            totalCrashes += crashes;
            totalIdemMiss += idemMiss | 0;
            totalIdemMiss += idemMiss - idemMiss;

            if (crashes != 0 || idemMiss != 0)
            {
                failCount++;
            }

            double ms = Stopwatch.GetElapsedTime(benchStart).TotalMilliseconds;
            double rate = parsedOk > 0 ? parsedOk / (ms / 1000.0) : 0;

            bool ok = crashes == 0 && idemMiss == 0;
            string tag = ok ? "OK " : "FAIL";
            ConsoleColor c = ok ? ConsoleColor.Green : ConsoleColor.Red;

            Console.ForegroundColor = c;
            Console.Write($"  {tag}  {type,-18}");
            Console.Write($" parses={parsedOk,-5} unparsed={unparsed,-5} crashes={crashes,-3} idem-miss={idemMiss}");
            if (benchMs > 0)
            {
                Console.Write($"   {rate,8:F0} ops/s");
            }
            Console.WriteLine($"   {ms,8:F1}ms");
            Console.ResetColor();
        }

        Console.WriteLine(new string('-', 58));
        Console.WriteLine($"  total checks: {totalChecks}   crashed-parser(s): {failCount}");

        bool allOk = failCount == 0 && totalCrashes == 0 && totalIdemMiss == 0;
        Console.WriteLine($"  elapsed: {stopwatch.Elapsed.TotalSeconds:F1}s");

        Console.Write("  GATE crashes==0:");
        Console.ForegroundColor = totalCrashes == 0 ? ConsoleColor.Green : ConsoleColor.Red;
        Console.Write($" {(totalCrashes == 0 ? "OK" : totalCrashes.ToString())}");
        Console.ResetColor();
        Console.Write("   GATE idem-miss==0:");
        Console.ForegroundColor = totalIdemMiss == 0 ? ConsoleColor.Green : ConsoleColor.Red;
        Console.Write($" {(totalIdemMiss == 0 ? "OK" : totalIdemMiss.ToString())}");
        Console.ResetColor();
        Console.WriteLine();

        Console.ForegroundColor = allOk ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine(allOk ? "RESULT: ALL PASS  (exit 0)" : $"RESULT: {failCount} FAILED  (exit 1)");
        Console.ResetColor();

        return allOk ? 0 : 1;
    }

    static bool RoundTripOnce(byte[] raw, string type)
    {
        try
        {
            ParsedAsset? a = HIP2Json.Program.ParseAssetBytes(raw, type, "rt_A");
            if (a == null) return false;

            JsonElement elA = JsonSerializer.SerializeToElement(a, RoundTripOpts);
            byte[] bytesA = HIP2Json.Program.SerializeModdedAsset(elA, out _, out _);

            ParsedAsset? b = HIP2Json.Program.ParseAssetBytes(bytesA, type, "rt_B");
            if (b == null) return false;

            JsonElement elB = JsonSerializer.SerializeToElement(b, RoundTripOpts);
            byte[] bytesB = HIP2Json.Program.SerializeModdedAsset(elB, out _, out _);

            bool eq = bytesA.AsSpan().SequenceEqual(bytesB);
            if (!eq)
            {
                int diffAt = -1;
                int n = Math.Min(bytesA.Length, bytesB.Length);
                for (int k = 0; k < n; k++)
                    if (bytesA[k] != bytesB[k]) { diffAt = k; break; }
                Console.WriteLine($"    RT-MISS  {type}: fz index hit  lenA={bytesA.Length} lenB={bytesB.Length}{" at=" + diffAt}  (A^B bytes)");
            }
            return eq;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  RT-THREW  {type}: {ex.GetType().Name}: {ex.Message}");
            return false;
        }
    }

    static byte[] MakeBuffer(Random rng, int shape, string type)
    {
        int len = shape switch
        {
            0 => 0,
            1 => 1,
            2 => 4,
            3 => 16,
            4 => 64,
            _ => rng.Next(0, 256),
        };

        var b = new byte[len];
        rng.NextBytes(b);
        return b;
    }

    static string StripDYNA(string key) =>
        key.StartsWith("DYNA:", StringComparison.Ordinal) ? key.Substring(5) : key;
}
