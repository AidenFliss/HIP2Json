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

public static class BlobRunner
{
    static readonly JsonSerializerOptions CompareOpts = new()
    {
        WriteIndented = true,
        IncludeFields = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static int Run(string blobDir, string only)
    {
        if (!Directory.Exists(blobDir))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"BLOB DIR NOT FOUND: {blobDir}");
            Console.ResetColor();
            return 2;
        }

        int totalTests = 0, totalFails = 0, totalNoBlob = 0;
        int kindBinaryPack = 0, kindSerialization = 0, kindCrash = 0, kindSyntax = 0;

        if (only != null)
        {
            only = only.ToUpperInvariant();
        }

        foreach (string file in Directory.GetFiles(blobDir, "*.txt").OrderBy(f => f, StringComparer.Ordinal))
        {
            string meta = Path.GetFileNameWithoutExtension(file); // {GAME}.{TYPE}.{maybeN}
            string[] parts = meta.Split('.');
            string gameLabel = parts[0];
            string type = parts[1];

            if (only != null && type != only)
            {
                continue;
            }

            string[] lines = File.ReadAllLines(file);

            var table = new UnitTest($"{gameLabel}.{type}");
            totalTests++;

            string base64 = ReadSection(lines, "BLOB:");
            string expectedJson = ReadSection(lines, "EXPECTED_JSON:");

            ApplyFixtureState(lines[0], gameLabel);

            if (string.IsNullOrWhiteSpace(base64))
            {
                totalNoBlob++;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  NOBLOB {gameLabel,-5} {type,-8} (unimplemented / null parse) — skipping");
                Console.ResetColor();
                continue;
            }

            try
            {
                RunBlobTest(table, type, lines[0], base64, expectedJson);
            }
            catch (Exception ex)
            {
                table.Failures.Add($"runner threw: {ex.GetType().Name}: {ex.Message}");
                table.FailKinds.Add(FailKind.Crash);
                table.Failed++;
                table.Passed = 0;
            }

            bool ok = table.AllPass;
            totalFails += table.Failed;
            foreach (FailKind fk in table.FailKinds)
            {
                if (fk == FailKind.BinaryPack) kindBinaryPack++;
                else if (fk == FailKind.Serialization) kindSerialization++;
                else if (fk == FailKind.Crash) kindCrash++;
                else if (fk == FailKind.Syntax) kindSyntax++;
            }
            Console.ForegroundColor = ok ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine($"  {(ok ? "OK  " : "FAIL")}  {gameLabel,-5} {type,-8} asserts={table.Passed + table.Failed} failed={table.Failed}");
            Console.ResetColor();
            foreach (string f in table.Failures)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"        FAIL => {f}");
                Console.ResetColor();
            }
        }

        if (only == null)
        {
            var registered = ParserMaps.AssetToParser.Keys
                .Concat(ParserMaps.DYNAToParser.Keys)
                .Select(t => t.Split(':')[^1].ToUpperInvariant())
                .Distinct()
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();

            var blobbed = new HashSet<string>(Directory.GetFiles(blobDir, "*.txt")
                .Select(f => Path.GetFileNameWithoutExtension(f).Split('.')[1])
                .Distinct());
            var missingTests = registered.Where(t => !blobbed.Contains(t)).ToArray();

            if (missingTests.Length > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  WARNING: NO FIXTURE FOR REGISTERED TYPE(S) ({missingTests.Length}): {string.Join(", ", missingTests)} — not a failure; only types with a real production blob are tested");
                Console.ResetColor();
            }
        }

        Console.WriteLine(new string('-', 58));
        Console.WriteLine($"  blob tests: {totalTests}   failed-asserts: {totalFails}   missing/no-blob: {totalNoBlob}");
        Console.WriteLine($"  fail-kind: binary-pack={kindBinaryPack}  serialization(values)={kindSerialization}  crash={kindCrash}  syntax={kindSyntax}");

        bool allOk = totalFails == 0 && totalNoBlob == 0;
        Console.ForegroundColor = allOk ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine(allOk ? "RESULT: ALL PASS  (exit 0)" : $"RESULT: FAILED  (exit 1)");
        Console.ResetColor();
        return allOk ? 0 : 1;
    }

    static void RunBlobTest(UnitTest table, string type, string header, string base64, string expectedJson)
    {
        string call = headerValue(header, "call=");
        if (string.IsNullOrWhiteSpace(call))
        {
            call = type;
        }

        byte[] blob;
        try
        {
            blob = Convert.FromBase64String(base64);
        }
        catch (Exception ex)
        {
            table.Failures.Add($"blob base64 invalid: {ex.Message}");
            table.FailKinds.Add(FailKind.Syntax);
            table.Failed++;
            return;
        }

        var rawExpected = expectedJson == "null" ? null : JsonDocument.Parse(expectedJson);

        ParsedAsset? a;
        try
        {
            a = HIPProg.ParseAssetBytes(blob, call, "blob_" + type);
        }
        catch (Exception ex)
        {
            table.Failures.Add($"binary->values parse CRASH: {ex.GetType().Name}: {ex.Message}");
            table.FailKinds.Add(FailKind.Crash);
            table.Failed++;
            return;
        }

        if (a == null)
        {
            table.Failures.Add($"binary->values: parse returned null (unparsed) for a real production blob");
            table.FailKinds.Add(FailKind.Serialization);
            table.Failed++;
            return;
        }

        JsonElement elV = JsonSerializer.SerializeToElement(a, CompareOpts);
        string valuesJson = stripEnvelope(elV).GetRawText();

        if (rawExpected != null)
        {
            string expText = stripEnvelope(rawExpected.RootElement).GetRawText();
            if (expText != valuesJson)
            {
                table.Failures.Add("binary->values JSON does NOT match expected (parser read the real blob differently)");
                table.FailKinds.Add(FailKind.Serialization);
                table.Failed++;
            }
            else
            {
                table.Assert(true, "binary->values matches expected", FailKind.Serialization);
            }
        }

        byte[] repacked;
        try
        {
            var el = JsonSerializer.SerializeToElement(a, CompareOpts);
            repacked = HIPProg.SerializeModdedAsset(el, out _, out _);
        }
        catch (Exception ex)
        {
            table.Failures.Add($"values->binary serialize CRASH: {ex.GetType().Name}: {ex.Message}");
            table.FailKinds.Add(FailKind.Crash);
            table.Failed++;
            return;
        }

        if (repacked == null || repacked.Length == 0)
        {
            table.Failures.Add("values->binary: serialize returned empty");
            table.FailKinds.Add(FailKind.BinaryPack);
            table.Failed++;
            return;
        }

        bool binaryEq = blob.AsSpan().SequenceEqual(repacked);
        table.Assert(binaryEq, "values->binary byte-IDENTICAL to original production blob", FailKind.BinaryPack);
        if (!binaryEq)
        {
            int diffAt = -1;
            int n = Math.Min(blob.Length, repacked.Length);
            for (int k = 0; k < n; k++)
                if (blob[k] != repacked[k]) { diffAt = k; break; }
            table.Failures.Add($"  values->binary differs at byte {diffAt}  blob={blob.Length}B pack={repacked.Length}B  (expected byte-identical repack of a real asset)");
            table.Failures.Add($"      blob: {HexDump(blob, Math.Max(0, diffAt - 4), 36)}");
            table.Failures.Add($"      pack: {HexDump(repacked, Math.Max(0, diffAt - 4), 36)}");
        }
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

    static string headerValue(string header, string keyword)
    {
        int idx = header.IndexOf(keyword, StringComparison.Ordinal);
        if (idx < 0)
        {
            return "";
        }

        string rest = header.Substring(idx + keyword.Length);
        int sp = rest.IndexOf(' ');
        return sp < 0 ? rest : rest.Substring(0, sp);
    }

    static void ApplyFixtureState(string header, string gameLabel)
    {
        GameType game = gameLabel == "BFBB" ? GameType.BFBB : GameType.TSSM;
        GamePlatform platform = GamePlatform.GC;

        int pIdx = header.IndexOf("platform=", StringComparison.Ordinal);
        if (pIdx >= 0)
        {
            string rest = header.Substring(pIdx + "platform=".Length);
            rest = rest.Split(' ', ' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
            platform = rest switch
            {
                "PS2" => GamePlatform.PS2,
                "Xbox" => GamePlatform.XBOX,
                _ => GamePlatform.GC,
            };
        }

        HIPProg.CurrentGame = game;
        HIPProg.CurrentPlatform = platform;
        HIPProg.BigEndian = platform == GamePlatform.GC;
    }

    static JsonElement stripEnvelope(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return root;
        }

        var keep = new Dictionary<string, JsonElement>();
        foreach (var prop in root.EnumerateObject())
        {
            switch (prop.Name)
            {
                case "Type":
                case "AssetID":
                case "AssetFlags":
                case "Alignment":
                case "AssetName":
                case "AssetFileName":
                case "AssetTypeName":
                case "AssetChecksum":
                case "AssetFriendlyName":
                case "FileName":
                case "RawBase64":
                    break;
                default:
                    keep[prop.Name] = prop.Value.Clone();
                    break;
            }
        }

        return JsonSerializer.SerializeToElement(keep, CompareOpts);
    }

    static string ReadSection(string[] lines, string header)
    {
        int idx = Array.FindIndex(lines, l => l.Trim() == header);
        if (idx < 0)
        {
            return "";
        }

        var sb = new StringBuilder();
        for (int i = idx + 1; i < lines.Length; i++)
        {
            string l = lines[i];
            if (l.Trim() == "EXPECTED_JSON:")
            {
                break;
            }

            if (l.StartsWith("#"))
            {
                continue;
            }

            sb.Append(l);
        }

        return sb.ToString().Trim();
    }
}