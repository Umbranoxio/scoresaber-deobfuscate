using CliWrap;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Deobfuscator.Tools
{
    internal class OsuDecoder : Tool
    {
        internal OsuDecoder() : base(
            path: Path.Combine(Environment.CurrentDirectory, "osu-decoder"),
            buildPath: Path.Combine(Environment.CurrentDirectory, "osu-decoder", "osu!decoder", "bin", "Release", "osu!decoder.exe"),
            slnName: "osu!decoder",
            repoUrl: "https://github.com/Umbranoxio/osu-decoder",
            restoreNugetPackages: true,
            resolveSubmodules: true
        )
        { }

        protected override async Task<string> ExecuteInternal(Deobfuscator deobfuscator, string path, string fileName)
        {
            Log("Running...");

            var results = await Cli.Wrap(BuildPath)
               .WithArguments($"-i \"{path}\" -p {deobfuscator.Password}")
               .WithValidation(CommandResultValidation.None)
               .ExecuteFallible();

            if (deobfuscator.Verbose && results?.StandardOutput is not null)
            {
                Log(results.StandardOutput);
            }

            Log("Done.");
            return $"{fileName}-decrypted.dll";
        }
    }
}
