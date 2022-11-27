using CliWrap;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Deobfuscator.Tools
{
    internal class Eazfixer : Tool
    {
        internal Eazfixer(ILogger logger) : base(
            logger: logger,
            path: Path.Combine(Environment.CurrentDirectory, "EazFixer"),
            buildPath: Path.Combine(Environment.CurrentDirectory, "EazFixer", "EazFixer", "bin", "Release", "net472", "EazFixer.exe"),
            slnName: "EazFixer",
            repoUrl: "https://github.com/holly-hacker/EazFixer",
            restoreNugetPackages: true
        )
        { }

        protected override async Task<string> ExecuteInternal(Deobfuscator deobfuscator, string path, string fileName)
        {
            var log = deobfuscator.Logger;
            var results = await Cli.Wrap(BuildPath)
                .WithArguments($"--file \"{path}\"")
                .WithValidation(CommandResultValidation.None)
                .ExecuteFallible();

            if (results?.StandardOutput is not null)
            {
                log.LogDebug("{stdout}", results.StandardOutput);
            }

            log.LogInformation("Deobfuscated assembly.");
            return $"{fileName}-eazfix.dll";
        }
    }
}
