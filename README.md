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

To use the program you will need [a copy of the game you will be modding extracted into a folder.](https://heavyironmodding.org/wiki/Setting_up_Dolphin_for_modding)

### Extracting:
Run ```HIP2Json``` with a project directory and the games ```files``` folder. Make sure to specify if the game is BFBB or TSSM.

You should see folders such as ```parsed``` and ```unpacked``` be created. If there is an error, you might have a corrupted / beta file or there is an issue that needs to be reported.

### Packing:

Run ```HIP2Json``` with the project directory and you should see the folder ```packed``` appear. This contains the modified HIP and HOP files to reimport into a copy of the game.

> [!NOTE]
Note: Some asset types do not have an implemented parser so they will be ignored when editing json.
