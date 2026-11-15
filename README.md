<p align="center">
  <img width="200" alt="NosCore" src="icon.png"/>
</p>

<h1 align="center">NosCore.Archive</h1>

<p align="center">
  <a href="https://www.nuget.org/packages/NosCore.Archive/"><img alt="NuGet" src="https://img.shields.io/nuget/v/NosCore.Archive.svg?style=flat-square"/></a>
  <a href="https://github.com/NosCoreIO/NosCore.Archive/actions/workflows/dotnet.yml"><img alt="Build" src="https://github.com/NosCoreIO/NosCore.Archive/actions/workflows/dotnet.yml/badge.svg?branch=master"/></a>
  <a href="https://discord.gg/Eu3ETSw"><img alt="Discord" src="https://img.shields.io/badge/Discord-join-5865F2?style=flat-square&logo=discord&logoColor=white"/></a>
  <a href="LICENSE"><img alt="MIT License" src="https://img.shields.io/badge/license-MIT-blue?style=flat-square"/></a>
</p>

Reads and writes the NosTale `.NOS` archives (`NScliData_*.NOS`, `NSlangData_*.NOS` and the like) that hold the
client's data files, such as `conststring.dat` and `item.dat`. Only the legacy layout is supported, not the newer
DEFLATE-based "NT Data" variant.

## Install

```bash
dotnet add package NosCore.Archive
```

## Usage

```csharp
using NosCore.Archive;

// Read
var bytes = File.ReadAllBytes("NScliData_conststring_en.NOS");
List<NosArchive.Entry> entries = NosArchive.Read(bytes);

// Change one inner file
var conststring = entries.Single(e => e.Name == "conststring.dat");
var patched = new NosArchive.Entry(conststring.Id, conststring.Name, conststring.Unknown, Patch(conststring.Content));

// Write back
var replaced = entries.Select(e => e.Id == conststring.Id ? patched : e).ToList();
File.WriteAllBytes("NScliData_conststring_en.NOS", NosArchive.Write(replaced));
```

## Layout

```
int32 fileCount
repeat fileCount times:
    int32   id
    int32   nameLen
    byte[]  name           (ASCII, nameLen bytes)
    int32   unknown        (kept as is on a round trip)
    int32   encLen
    byte[]  enc            (XOR-obfuscated payload, encLen bytes)
```

`NosArchive.Write` emits the simple `0x33` XOR mode; `NosArchive.Read` handles both that and the packed-nibble mode
the client's own archives use.

## Building

```bash
dotnet build
dotnet test
```

## Contributing

Questions, ideas and pull requests are welcome; most of the discussion happens on [Discord](https://discord.gg/Eu3ETSw).
Any editor that handles .NET 11 works: `dotnet build` and `dotnet test` run on Windows, Linux and macOS.
In Visual Studio, [Roslynator](https://github.com/dotnet/roslynator) and
[ReSharper](https://www.jetbrains.com/resharper/) help keep to the code style.

## Supporting NosCore

NosCore is a spare-time open source project. Sponsoring buys time spent on the emulator and the packages around it,
and gives the **Supporter** role on Discord.

<p>
  <a href="https://github.com/sponsors/0Lucifer0"><img alt="Sponsor on GitHub" src="https://img.shields.io/badge/GitHub-sponsor-EA4AAA?style=for-the-badge&logo=githubsponsors&logoColor=white"/></a>
  <a href="https://ko-fi.com/A3562BQV"><img alt="Support on Ko-fi" src="https://img.shields.io/badge/Ko--fi-support-FF5E5B?style=for-the-badge&logo=kofi&logoColor=white"/></a>
  <a href="https://www.patreon.com/bePatron?u=6503887"><img alt="Become a patron" src="https://img.shields.io/badge/Patreon-become_a_patron-F96854?style=for-the-badge&logo=patreon&logoColor=white"/></a>
</p>

Thanks to [Navicat](https://www.navicat.com) for supporting the project.

<a href="https://www.navicat.com"><img height="60" alt="Navicat" src="https://user-images.githubusercontent.com/35202750/207230064-dcf23adc-9e96-4481-9a53-cd212f5bd60e.png"/></a>

## Legal

NosCore is an independent, unofficial project for educational use. It is not affiliated with, authorized, maintained,
sponsored or endorsed by Gameforge or any of its affiliates; NosTale is a trademark of Gameforge. Using it may be
against the terms of service of NosTale servers.

Released under the [MIT License](LICENSE).
