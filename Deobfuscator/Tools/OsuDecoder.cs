using CliWrap;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Deobfuscator.Tools
{
    internal class OsuDecoder : Tool
    {
        internal OsuDecoder(ILogger logger) : base(
            logger: logger,
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
            var log = deobfuscator.Logger;
            var results = await Cli.Wrap(BuildPath)
               .WithArguments($"-i \"{path}\" -p {deobfuscator.Password}")
               .WithValidation(CommandResultValidation.None)
               .ExecuteFallible();

            if (results?.StandardOutput is not null)
            {
                log.LogDebug("{stdout}", results.StandardOutput);
            }

            log.LogInformation("Decoded encrypted symbols.");
            return $"{fileName}-decrypted.dll";
        }
    }
}
