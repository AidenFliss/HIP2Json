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
  HIP2Json --extract <input_path> [output_path] [options]
  HIP2Json --pack    <input_path> [output_path] [options]

Modes:
  --extract, -e   Extract a single .hip/.hop archive OR an entire game files directory.
  --pack, -k      Pack a project folder (*_unpacked or *_project) back into binary archive(s).

Options:
  --game, -g      Specify target game format (BFBB or TSSM). [Required]
  --platform, -p  Specify target platform format (GC, PS2, or XBOX). [Required]
  --progress, -c  Show parsing coverage stats.
  --help, -h      Show this help message.
```

### Extracting:

Run `HIP2Json` with `--extract` (`-e`), passing either a single `.hip`/`.hop` file or an entire `files` folder. Make sure to specify the game (`-g`) and platform (`-p`).

* **Single File Example:**
  ```bash
  HIP2Json -e path/to/jf01.HIP -g BFBB -p GC
  ```
  Creates a `jf01_unpacked/` project folder containing `og/`, `mod/`, and `unpacked/`.

* **Full Directory Example:**
  ```bash
  HIP2Json -e path/to/game/files/ -g BFBB -p GC
  ```
  Creates a `files_project/` folder containing all game archives unpacked into `og/`, `mod/`, and `unpacked/`.

If there is an error during extraction, you might have a corrupted/beta file or an issue that needs to be reported.

### Packing:

Run `HIP2Json` with `--pack` (`-k`) on the generated project directory to reimport your changes back into `.hip` and `.hop` binary files.

* **Single File Project:**
  ```bash
  HIP2Json -k jf01_unpacked/ -g BFBB -p GC
  ```
  Packs your modifications back into `jf01_unpacked/jf01.hip`.

* **Full Game Project:**
  ```bash
  HIP2Json -k files_project/ -g BFBB -p GC
  ```
  Packs all modified archives into a `files_packed/` folder ready to replace in your game build.

> [!NOTE]
> Note: Some asset types do not have an implemented parser so they will be ignored when editing json.