# HIP2Json Codebase Guide

Tool that extracts Heavy Iron engine (EvilEngine) HIP/HOP game archives (Battle for Bikini Bottom, SpongeBob Movie / tssm) into raw + parsed-JSON asset projects, and packs edited JSON back into binary archives.

## Solution layout

- `HipHopTool/HipHopFile` — legacy grain-size container library (reads + writes HIP/HOP). Decodes the block/section tree but treats asset payloads as opaque bytes.
- `HIP2Json` — the CLI app (`Program.cs` + `dotnet run`) and all JSON parsing. Holds per-asset-type parsers, dictionaries, conversion, and serialization.
- `HIP2Json/Parsers` — one `AssetParser` subclass per asset type (TRIG, PLYR, PLAT, HANG, NPC, SIMP, VIL, etc.) plus the `AbstractDYNAParser` family. Entry points are regiestered in `ParserMaps.cs`.
- `HipHopFile/HipHopFile.csproj` targets `netstandard2.0` (no dependencies); `HIP2Json` is `net10.0` exe with no package refs. Build/verify with `dotnet build HIP2Json.sln -c Release`.

## The HIP/HOP container (per heavyironmodding.org/wiki/EvilEngine/HIP)

Block tree. Every block is: 4-char ASCII magic + `uint32` length covering itself and its children (not the magic/length header), then data, then children.

- Outer `HIPA` (or `TAIH` reverse-order). Contains `PACK`, then `DICT`.
- `PACK` — metadata sub-blocks, in order: `PVER` (version uint32), `PFLG` (flags uint32), `PCNT` (asset counts uint8[6]), `PCRT` (crc32 verifier uint32), `PMOD` (module-type flag uint32), optional `PLAT` (platform flag uint32).
- `DICT` — text dictionary. Holds `ATOC` (asset table: `AINF` header + one `AHDR` per asset) and `LTOC` (layer table: `LINF` header + one `LHDR` per layer). `AHDR` carries asset data info (id `uint32`, type 4-char `assetType`, `offset` (absolute file offset), `size`, `plus`, `flags`, then `ADBG`: alignment, name string, filename string, crc32 checksum). `LHDR` has layer `flags`, `layerType` (the asset type char, e.g. `J`PS), and `sizeofLayerData`.
- `STRM` — stream block holding the raw asset data. Contains `DHDR` header (padding type) followed by `DPAK`: `paddingAmount` `uint32` + `0x33`-byte stale padding + all asset bytes (concatenated). Data is aligned per 4CC layer padding (platform/alex). Layers themselves are ordered inside the stream.
- `HIPB` — optional custom tooling block at the end (Heavy Iron PackageBuilder). Ignore unless repacking.

Rules that matter:

- **All integers in the container (HE/HIGA container structure) are BIG-ENDIAN, regardless of platform.** This includes block magics/lengths, AHDR fields, PVER/PFLG/PCNT/PCRT/PMOD, PLAT, LHDR fields, DPAK paddingAmount. Always use big-endian readers for the section layer.
- **Asset payload bytes are platform-endian.** GC = big-endian; PS2 and XBOX = little-endian. HIP2Json decodes payloads with `Util`/`Read*BE` helpers that honor `Program.BigEndian`. The library does NOT do this — it stores payload bytes raw. This is why a HIP2Json "GC" extraction has different (parsed) JSON than a "PS2" one.
- AHDR `offset` is absolute from the start of the .hip/.hop file (not relative to the stream block).
- `ADBG.checksum` is CRC-32 (MPEG-2 polynomial).
- Scooby (non-BFBB) archives use a different `noAHDR` path with no entry padding: the library compensates via `Section_ATOC.noAHDR` (see gotchas) and first-padding. Don't repack Scooby.
- Localized Incredibles PC archives have wrong STRM size values (wiki); ignore STRM header size and always slice asset data by AHDR offset/size.

## Endianness model (HIP2Json)

- `Program.BigEndian` defaults `true`; set `BigEndian = CurrentPlatform == GamePlatform.GC` in `Main`.
- Container reading/writing in HipHopFile is always big-endian (never consults `Program.BigEndian`).
- Asset payload reading in HIP2Json uses `AssetParser.Read*/Write*` helpers which swap when `BigEndian == false`. `ReadFloatLE/WriteFloatLE/ReadInt32LE` are always little-endian (used where the format fixes it, e.g. some parsers).
- `AssetParser.GetAssetIDConverter` produces the string/int forms of asset ids, and `Util` maps JHG in/out for id handling. Never assume little-endian on GC.
- Linked (sub) headers use id (`AssetID`) forms that swap on little-endian platforms.

## How unpacking works

`Main --unpack` → `RunUnpack` → per .hip/.hop file `ProcessSingleArchiveUnpack`:

1. Game/platform are auto-detected from the container (`PVER/PFLG/PCRT.date/PLAT`) via `HipFile.FromPath`; the CLI `-g`/`-p` flags are optional and only used as an override when detection yields `Unknown`.
2. `hipfile.ToIni(game, extractDir, true, true)` writes the raw per-asset files and a `Settings.ini` (same step as `--extract`), then stops — **no parsed JSON, no og/mod writing, no repack**.
3. Each archive is wrapped in try/catch so one bad file doesn't kill the batch, and `Section_ATOC.noAHDR` is reset between files (its static state must not leak across archives in one process).

## How extraction works

`Main --extract` → `RunExtract` → per .hip/.hop file `ProcessSingleArchiveExtract`:

1. `HipHopFile.HipFile.FromPath(file)` — detects `Game` + `Platform` from container bytes, builds the section tree, slices each asset payload (by AHDR offset/size) into `asset.data`, and fills `asset.FileName`.
2. `hipfile.ToIni(...)` — writes raw per-asset files into `unpacked/<Archive>_<HIP|HOP>/<assetType>/[<id>] <name>` plus a `Settings.ini` describing archive name/game/platform, and a `HLSAVE*` placeholder if present.
3. HIP2Json re-reads those raw files, runs the per-type `AssetParser`, and writes a parsed JSON array to BOTH `parsed/og/<Archive>_assets.json` and `parsed/mod/<Archive>_assets.json`.

Blacklisted payload types are stored in `ParserMaps`/`Program.BLACKLIST_ASSETS` and never parsed into JSON (BSP/JSP/MODL/RWTX/TEXS/ANIM/SND stack). `BASE_ASSETS`/`ENTITY_ASSETS` decide the `AssetType` (Base vs Entity). Remaining/passthrough types parse as Binary.

## Adding/missing a parser

`ParserMaps.cs` statis anatomy:
- `BASE_ASSETS`, `ENTITY_ASSETS` (respectively know their flag masks).
- `ASSET_TO_ASSETTYPE`, `ASSET_ASSETTYPE_TO_PARSER`, `ASSET_EXTRA_HEADER` (per asset header size adjusters), `ASSET_TO_FRIENDLY_NAME`, `BASETYPE_TO_FRIENDLY_NAME`, `ParseAssetDispatcher` mapping type→handler for base/entity/binary json parsing.
- `Dictionaries.DYNA_TO_NAME_MAPPING` maps DYNA type4CC hashes → friendly names; unknown ones will throw `KeyNotFoundException` in `DYNA.cs` (see TODO list) — any new DYNA type must be added there AND to `DYNAParsersAssembler`/`AbstractDYNAParser` family.
- When you add a new `AssetParser` subclass it must be registered in `ParserMaps.cs` (in `ParseAssetDispatcher`) and its `_assetType` must match the ASCII 4CC.

`_unimplemented`/`_unimplByType` counters (Program) track unknown types for `--progress`; unimplemented base/entity types get dumped as binary and logged.

## How packing works

`Main --pack` → `RunPack(inputPath, outputPath, overwriteFlag)`. If a `project.json` exists in inputPath it is a `*_proj` folder → `RunPackProject` (below); otherwise it is an `*_unpacked` folder:
1. (optional) `ScanJsonKeyDifferences(og, unpacked)` — diffs `parsed/og/*.json` vs `parsed/mod/*.json`, and for each changed property path writes a `mod_<asset>_overrides.json` (the custom JSON edit format tracks edits by path). Those override JSONs are then re-merged into each archive.
2. `HipFile.FromINI(unpacked/Settings.ini)` assembles a new `HipFile` from the Settings + per-asset binary files, with a `HIPB` placed automatically by `HipFileHelpers.ReadHipBin`.
3. `hipFile.ToBytes(game, platform)` serializes container (always BE). Final output written as `<archive>.hip`.

Repacking a BFBB archive with `--platform PS2` produces a valid but *changed* container: payload should have already been edited in the JSON and reparsed with the same `CurrentPlatform` used by the user's tools.

## How project mode works

`Main --project`/`-j` → `RunProject` → per archive `ProcessSingleArchiveProject`. This is the in-memory flow (no raw dump, no Settings.ini):

1. `HipFile.FromPath(file)` builds the section tree; `ResolveGamePlatform(game, platform)` applies the detected game/platform to `Program` globals unless `-g`/`-p` override.
2. Every `AHDR` gets one `ParsedAsset` entry in `assets.json` + `mod_assets.json` (identical on export, `mod_` is the editable one). `ParseAssetBytes(data, type, name)` parses entity/base/binary payloads into Base/Links/typed JSON; anything blacklisted, unparsered, DYNA-without-subparser, or throwing is stored as `RawBase64` (models/textures/animation/sound transplant byte-for-byte).
3. Each asset carries `Type` (4CC), `AssetID` ("0x..."), `AssetFlags`, `Alignment`, `AssetName`, `AssetFileName`, `AssetTypeName` (the concrete `AHDR.assetType.ToString()`, e.g. `EnemySB` for a DYNA — needed to rebuild a DYNA `AHDR` with the same concrete enum), `AssetChecksum`.
4. `project.json` records archiveName/type, absolute `sourceFile` + `sourceFileSHA256`, game/platform, PVER/PFLG/pcn* hints/PCRT/PMOD/PLAT, AINF/LINF/DHDR, layers (layerType/ldbg/assetIDs in LHDR order), and `hipb` ONLY when the source file actually contains a `HIPB` magic (FromPath fabricates a default one otherwise — never store/emit it if absent).

`RunPackProject(projectDir, outputPath, overwriteFlag)` packs a `*_proj` folder:
1. Reads `project.json`; uses `mod_assets.json` if present else `assets.json`.
2. Rebuilds sections exactly (PVER/PFLG/PCNT/PCRT/PMOD/PLAT if stored, AINF/LINF/DHDR, LHDRList with stored layerType/assetIDs/LDBG, HIPB only if stored). Raw entries decode `RawBase64`; parsed entries go through `SerializeModdedAsset` (type-detect via ParserMaps, resolve parser, DYNA payload reshuffle, `SerializeAssetElement`) to produce full bytes.
3. `AHDR.data` holds those bytes; checksum recomputed via `Crc32Mpeg2`; `AssetTypeName` round-trips through `Enum.GetValues` in the `Section_AHDR(string)` ctor.
4. `hipFile.ToBytes(game, platform)` drives `HipFile.SetupSTRM`/`BuildStream`; `SetupSTRM` preserves the PCNT size-hint fields (pcnA/pcnL/pcnV) if they were set before `ToBytes` — they survive the zeroed temp pass.
5. Output resolution: explicit out path > `-o`/`--overwrite` (writes to `sourceFile` ONLY if current SHA-256 still matches `sourceFileSHA256`, else refuses; missing source → refuse) > default `sourceDir/<archive>.new.<ext>` (og-spot sibling, never touches the source).

Round-trip fidelity: raw/base64 assets repack byte-identically (verified). Parsed payloads go through JSON parse→serialize, so byte diffs vs the og archive are expected for assets whose parser read/write aren't symmetric (e.g. MINF param length math) — same as legacy pack. `HipFile.SetupSTRM` and `SerializeAssetElement` PLYR special-case (coreBytes written after links) are shared by both flows.

## Project conventions

- `.editorconfig` enforces file-scoped `namespace` blocks, UTF-8 (with BOM) C# files, strict visibility. Follow the file-scoped + `using System...;` ordering as-is.
- Logging is via `Logger.LogInfo/LogWarning/LogError` (shunt to console).
- Do not use `var` for new code; only for existing patterns. No package dependencies allowed in HIP2Json — JSON is `System.Text.Json`.
- C#/dotnet version is blocking: do not add modern-only C# lang features to HipHopTool (netstandard2.0).

## Known gotchas / glaring potential issues (avoid-new)

- **`HipFile.FromINI` only works reliably for a single archive** — `SetupFromINI` iterates `DICT.ATOC.AHDRList` with `Remove` inside the loop, so a missing/mismatched asset file throws `InvalidOperationException`. The `unpacked/` per-archive folders each have their own Settings.ini (safe individually).
- `HipFile.FromPath` throws on unrecognized top-level section (`else throw new Exception(currentSection)`) and can NRE when a JSP asset has no `LTOC` layer (`LayerType`).
- `Section_ATOC.noAHDR` is a **static mutable field** shared across all `HipFile` instances in one process. A batch extraction of multiple archives that mixes formats will leak this flag and mis-size DPAK padding on subsequent archives.
- **Platform/endianness used to be decided once from CLI flags in `Main`; now `ResolveGamePlatform(game, platform)` sets `Program.CurrentGame`/`CurrentPlatform`/`BigEndian` per archive from `FromPath` detection (CLI `-g`/`-p` override it).** So mixing archives of different platforms in one run parses each payload with the right endianness. The pre-existing caveat stands that a BFBB-style payload on an Xbox container is under-modeled.
- The BFBB-vs-TSSM entity header length (`0x54` vs `0x50`) is decided by `CurrentGame == GameType.BFBB`; the pun on platform differences (a BFBB-style level on Xbox with tssm headers) is currently under-modeled — see `ParseEntityChunk` and the `[解析]` helpers.
- `PLYR` is a special-case asset type with an extra (lightKit) field following its links; generic link parsing (`GetLinksOffset = end - 32*count`) treats it like every other type and can mis-read `lightKitID`.
- `SetupFromINI` uses `assetDataDictionary[assetID].data = File.ReadAllBytes(foundPath)`; keys collide if two asset files share an id, or if a filename id-prefix overlaps (assetDataDictionary keys are truncated from `[id]` filename).
- Repack padding: `Section_DPAK` writes padding via `0x33` zero-pad header + `paddingAmount`. Only matching platform alignment (GC 0x20, PS2/XBOX 0x800) reproduces a faithful archive; library writes GC padding by default regardless of platform.