# ScoreSaber Deobfuscator

_This tool will clone and build any required tools, and then attempt to deobfuscate a ScoreSaber DLL file._

## Prerequisites (Toolchain)

- [.NET SDK (version 6 or later)](https://dotnet.microsoft.com/en-us/download)
- [.NET Framework v4.7.2 Developer Pack](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net472)
- `git` in your PATH
- `msbuild` in your PATH

## Prerequisites (Deobfuscation)

- Obfuscated ScoreSaber `.dll`
- All dependencies for the ScoreSaber version
  - Game Assemblies
  - Mod Assemblies
  - Library Assemblies

## Example Usage:

> **Note**  
> Currently the CLI can only read from a single dependency directory. You will need to merge all dependencies into a single directory for now.

```
Deobfuscator.Cli.exe -i <input file> -d <dependency dir> -p <symbol encryption key>
```

If there are problems with the deobfuscation process, run the tool with the optional verbosity flag `-v` and send the results to `Umbranox#0001` on Discord, I'll resolve it.
