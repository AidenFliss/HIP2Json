# HIP2Json
[![.NET](https://img.shields.io/badge/.NET-512BD4?logo=dotnet&logoColor=fff)](#)
![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/AidenFliss/HIP2Json/build.yml)

<img width="840" height="630" alt="Project Logo" src="https://github.com/user-attachments/assets/80f2fb9e-d9b9-4551-958a-b52f4e49e74e" />

___

A tool that allows you to extract .HIP and .HOP files to a set of json files, edit those files, and reimport your changes back into the game.

# Features

- Parse .HIP and .HOP files in the games files
- Extract as .json to be edited
- Reimport them back into .HIP and .HOP files

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

```text
Usage:
  HIP2Json --unpack  <input_path> [output_path] [options]
  HIP2Json --extract <input_path> [output_path] [options]
  HIP2Json --project <input_path> [output_path] [options]
  HIP2Json --pack    <input_path> [output_path] [options]

Modes:
  --unpack, -u   Unpack .hip/.hop archive(s) to raw asset files + Settings.ini only (no JSON/repack).
  --extract, -e  Extract a single .hip/.hop archive OR an entire game files directory.
  --project, -j  Build an in-memory project from .hip/.hop (project.json + assets.json + mod_assets.json, raw bytes as base64).
  --pack, -k     Pack a project folder (*_unpacked or *_project) back into binary archive(s).

Options:
  --game, -g       Specify target game format (BFBB or TSSM). [optional; auto-detected from the archive when omitted]
  --platform, -p   Specify target platform format (GC, PS2, or XBOX). [optional; auto-detected from the archive when omitted]
  --overwrite, -o  When packing a *_project folder, overwrite the original source archive in its own spot (sha-256 verified).
  --progress, -c   Show parsing coverage stats.
  --help, -h       Show this help message.
```

### Unpacking:

Run `HIP2Json` with `--unpack` (`-u`) to dump the raw contents of `.hip`/`.hop` archives into per-asset binary files and a `Settings.ini` — the game and platform are auto-detected from the archive contents, so `-g`/`-p` are not required.

* **Single File Example:**
  ```bash
  HIP2Json -u path/to/jf01.HIP
  ```
  Creates a `jf01_raw/` folder containing `jf01_HIP/` with the raw asset files (`[id] name`) and `Settings.ini`.

* **Full Directory Example:**
  ```bash
  HIP2Json -u path/to/game/files/
  ```
  Unpacks every `.hip`/`.hop` in the tree into `<output_path>/<archive>_<HIP|HOP>/...`. Pass `-g`/`-p` to force a game/platform override on archives whose container doesn't say.

### Extracting:

Run `HIP2Json` with `--extract` (`-e`), passing either a single `.hip`/`.hop` file or an entire `files` folder. The game and platform are auto-detected from the archive contents; pass `-g`/`-p` only to force an override.

* **Single File Example:**
  ```bash
  HIP2Json -e path/to/jf01.HIP
  ```
  Creates a `jf01_unpacked/` project folder containing `og/`, `mod/`, and `unpacked/` directly inside the folder.

* **Full Directory Example:**
  ```bash
  HIP2Json -e path/to/game/files/
  ```
  Creates a `files_project/` folder containing `parsed/` (with the `og/` and `mod/` folder structure) and `unpacked/`.

If there is an error during extraction, you might have a corrupted/beta file or an issue that needs to be reported.

### Project Building:

Run `HIP2Json` with `--project` (`-j`) on a `.hip`/`.hop` file or a whole `files` folder to build a lightweight in-memory project. Unlike `--extract`, this reads assets straight from the archive — no raw file dump (and no `Settings.ini`) is written. Unsupported payload types (models, textures, animation, sound stacks, BSP/JSP) are stored inline as base64 in `mod_assets.json`, so they transplant byte-for-byte.

* **Single File Example:**
  ```bash
  HIP2Json -j path/to/jf01.HIP
  ```
  Creates `<source_dir>/jf01_proj/` with `project.json` (source path + sha-256, game/platform, layers) and `assets.json` + `mod_assets.json`.

* **Full Directory Example:**
  ```bash
  HIP2Json -j path/to/game/files/
  ```
  Builds a `<name>_proj/` project folder for every archive in the tree.

### Packing:

Run `HIP2Json` with `--pack` (`-k`) on the generated project directory to reimport your changes back into `.hip` and `.hop` binary files.

* **Single File Project:**
  ```bash
  HIP2Json -k jf01_unpacked/ -g BFBB -p GC
  ```
  Packs your modifications directly into `jf01_unpacked/jf01.hip`.

* **Full Game Project:**
  ```bash
  HIP2Json -k files_project/ -g BFBB -p GC
  ```
  Packs all modified archives into `files_project/packed/` ready to replace in your game build.

A `*_proj` project folder is packed back into its original spot, next to the source:

* **In-Memory Project (no overwrite):**
  ```bash
  HIP2Json -k jf01_proj/
  ```
  Writes `jf01.new.hip` next to the original source archive — the source file is never touched.

* **In-Memory Project (overwrite source):**
  ```bash
  HIP2Json -k jf01_proj/ -o
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