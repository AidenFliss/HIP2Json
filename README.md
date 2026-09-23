# HIP2Json
[![.NET](https://img.shields.io/badge/.NET-512BD4?logo=dotnet&logoColor=fff)](#)
![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/AidenFliss/HIP2Json/build.yml)

<img width="840" height="630" alt="Project Logo" src="https://github.com/user-attachments/assets/80f2fb9e-d9b9-4551-958a-b52f4e49e74e" />

___

A tool that allows you to extract .HIP and .HOP files to a set of json files, edit those files, and reimport your changes back into the game.

# Projects

- `HIP2Json` — the **library**. Asset parsers, dictionaries, conversion, serialization, the embedded HIP/HOP container, and the typed `HipArchive` API (`HipArchive.Open` → mutate `Assets` / `AddAsset` / `RemoveAssets` → `Save` / `Overwrite`).
- `HIP2Json.CLI` — the **command-line app** built on the library (builds JSON projects by default; `--save-assets` for raw dumps; `--pack`/`-k` to repack).
- `HIP2Json.Tests` — the gates (see [Testing](#testing-hiplargejson2jsonapptests) below).

# Features

- Parse .HIP and .HOP files in the games files
- Extract as .json to be edited
- Reimport them back into .HIP and .HOP files
- Embed HIP parsing/serialization in your own tool via the `HIP2Json` library

# Supported Games:
- ❌ Scooby
- ✅ BFBB
- ✅ TSSM
- ❌ Incredibles
- ❌ ROTU

> Support for Scooby, Incredibles, and ROTU are not planned at this time.

# Supported Platforms:
- ✅ Gamecube
- ✅ PS2
- ✅ Xbox

# Usage

To use the program, you will need [a copy of the game you will be modding extracted into a folder.](https://heavyironmodding.org/wiki/Setting_up_Dolphin_for_modding)

### Command Line Interface

The CLI binary is `HIP2Json.CLI` (run it via `dotnet run` in `HIP2Json.CLI/` during development, or the published `HIP2Json.CLI` executable):

```text
Usage:
  HIP2Json.CLI <input_path> [output_path] [flags]   build a JSON project (default; ALWAYS makes a project)
  HIP2Json.CLI --pack <project_path> [-o out.hip]  repack a project folder back into a binary archive

A project (project.json + assets.json + mod_assets.json) is always built, whether the input is a single
.hip/.hop file or an entire game files directory. Only JSON goes to disk by default (no raw bytes = 0 MB).
  single archive -> <archive-name>_proj/          (next to the archive)
  game directory -> <folder>_project/<archive>_proj/   (next to the game folder)

Flags:
  --save-assets   ALSO write raw per-asset files + Settings.ini to disk under <project>/unpacked/.
                  Opt-in on purpose -- this is the only thing that dumps big game bytes. (alias: --debug)
  --game, -g       Specify target game format (BFBB or TSSM). [optional; auto-detected from the archive when omitted]
  --platform, -p   Specify target platform format (GC, PS2, or XBOX). [optional; auto-detected from the archive when omitted]
  --overwrite, -o  When packing a *_proj folder, overwrite the original source archive in its own spot (sha-256 verified).
  --progress, -c   Show parsing coverage stats.
  --help, -h       Show this help message.

Deprecated aliases: --unpack/-u and --extract/-e (now: project + --save-assets), --project/-j (now: the default).
```

### Building a Project (default):

Just point the CLI at a `.hip`/`.hop` file or an entire `files` folder. No mode flag is needed — a JSON project is ALWAYS built. The game and platform are auto-detected from the archive contents, so `-g`/`-p` are required only to force an override.

* **Single File Example:**
  ```bash
  HIP2Json.CLI path/to/jf01.HIP
  ```
  Creates `<source_dir>/jf01_proj/` with `project.json` (source path + sha-256, game/platform, layers) and `assets.json` + `mod_assets.json`.

* **Full Directory Example:**
  ```bash
  HIP2Json.CLI path/to/game/files/
  ```
  Creates `path/to/files_project/` containing a `<archive>_proj/` project folder for every `.hip`/`.hop` in the tree.

Unsupported payload types (models, textures, animation, sound stacks, BSP/JSP) are stored inline as base64 in `mod_assets.json`, so they transplant byte-for-byte. No raw asset files are written unless you ask for them.

* **Including Raw Asset Files:**
  ```bash
  HIP2Json.CLI path/to/jf01.HIP --save-assets
  ```
  Same as above, plus raw per-asset files and a `Settings.ini` under `<project>/unpacked/`. This is the ONLY way to dump raw game bytes, and it is opt-in on purpose.

### Packing:

Run `HIP2Json.CLI` with `--pack` (`-k`) on the generated project directory to reimport your changes back into `.hip` and `.hop` binary files.

* **Full Game Project:**
  ```bash
  HIP2Json.CLI -k files_project/ -g BFBB -p GC
  ```
  Packs all modified archives into `files_project/packed/` ready to replace in your game build.

A `*_proj` project folder is packed back into its original spot, next to the source:

* **In-Memory Project (no overwrite):**
  ```bash
  HIP2Json.CLI -k jf01_proj/
  ```
  Writes `jf01.new.hip` next to the original source archive — the source file is never touched.

* **In-Memory Project (overwrite source):**
  ```bash
  HIP2Json.CLI -k jf01_proj/ -o
  ```
  Repacks straight into the original `jf01.HIP`. This only succeeds if the current source archive still matches the sha-256 recorded in `project.json`; a modified/renamed source is refused to protect your original.

> [!NOTE]
> Note: Some asset types do not have an implemented parser so they will be ignored when editing json.

---

## Testing (HIP2Json.Tests)

`HIP2Json.Tests` ships three gates that must pass before CI is green:

| Gate | Command | Fails on |
| --- | --- | --- |
| Fuzz | `dotnet run --project HIP2Json.Tests -- -c Release --fuzz 8` | any parser crash or non-idempotent round-trip on randomized bytes |
| Blob | `dotnet run --project HIP2Json.Tests -- -c Release --blobs HIP2Json.Tests/Blobs` | a production blob that fails the values round-trip or repacks byte-differently |
| Archives | `--archives <bfbbRoot> <tssmRoot>` | a full-archive repack that is not byte-identical |

The **blob gate** replays real production assets (harvested from game files) and requires binary→values→binary to round-trip **byte-identically**. Running it with no failures is the release door.

- Fixtures live in `HIP2Json.Tests/Blobs/` (one `.txt` per `GAME.TYPE`, header + `BLOB:` base64 + `EXPECTED_JSON:`). Refresh them from a stock game-files folder with `dotnet run --project HIP2Json.Tests -- -c Release --harvest <bfbbRoot> <tssmRoot> <outDir>`.
- Registered parser types that have **no real production asset** in the harvested games (e.g. `LITE`, bare `DYNA`) print a **warning, not a failure** — the handful of exotic types with no in-game instance are documented by that warning rather than faked.
- `--seed` pins the fuzz RNG; CI uses `--fuzz 8 --seed 0x5EEDF00D`. The blob gate does not depend on any seed.