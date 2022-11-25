using CliWrap;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Deobfuscator.Tools
{
    internal class Eazfixer : Tool
    {
        internal Eazfixer() : base(
            path: Path.Combine(Environment.CurrentDirectory, "EazFixer"),
            buildPath: Path.Combine(Environment.CurrentDirectory, "EazFixer", "EazFixer", "bin", "Release", "net472", "EazFixer.exe"),
            slnName: "EazFixer",
            repoUrl: "https://github.com/holly-hacker/EazFixer",
            restoreNugetPackages: true
        )
        { }

        protected override async Task<string> ExecuteInternal(Deobfuscator deobfuscator, string path, string fileName)
        {
            Log("Running...");

            var results = await Cli.Wrap(BuildPath)
                .WithArguments($"--file \"{path}\"")
                .WithValidation(CommandResultValidation.None)
                .ExecuteFallible();

            if (deobfuscator.Verbose && results?.StandardOutput is not null)
            {
                Log(results.StandardOutput);
            }

            Log("Done.");
            return $"{fileName}-eazfix.dll";
        }
    }
}
