# NosCore.Archive

Read/write support for the legacy NosTale `.NOS` archive format used by `NScliData_*.NOS` and friends (e.g. `conststring.dat`, `langdata.dat`).

The newer "NT Data" DEFLATE variant is **not** supported — this library targets the legacy layout only.

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

// Mutate an inner file
var conststring = entries.Single(e => e.Name == "conststring.dat");
var patched = new NosArchive.Entry(
    conststring.Id,
    conststring.Name,
    conststring.Unknown,
    Patch(conststring.Content));

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
    int32   unknown        (preserve verbatim on round-trip)
    int32   encLen
    byte[]  enc            (XOR-obfuscated payload, encLen bytes)
```

The inner payload is XOR-obfuscated. `NosArchive.Write` emits the simple `0x33`-XOR mode; `NosArchive.Read` handles both that and the packed-nibble mode produced by the real client.

## You like our work? ##
<a href='https://github.com/sponsors/0Lucifer0' target='_blank'><img height='48' style='border:0px;height:46px;' src='https://i.gyazo.com/47b2ca2eb6e1ce38d02b04c410e1c82a.png' border='0' alt='Sponsor me!' /></a>
[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/A3562BQV)
<a href='https://www.patreon.com/bePatron?u=6503887' target='_blank'><img height='46' style='border:0px;height:46px;' src='https://c5.patreon.com/external/logo/become_a_patron_button@2x.png' border='0' alt='Become a Patron!' /></a>
