using CliWrap;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Deobfuscator.Tools
{
    internal class de4dot : Tool
    {
        internal de4dot() : base(
            path: Path.Combine(Environment.CurrentDirectory, "de4dot"),
            buildPath: Path.Combine(Environment.CurrentDirectory, "de4dot", "Release", "de4dot.exe"),
            slnName: "de4dot",
            repoUrl: "https://github.com/lolPants/de4dot",
            resolveSubmodules: true,
            targetCommit: "22bc21240115e8572c8a702288f2e26fd4a51ca8"
        )
        { }

        protected override async Task<string> ExecuteInternal(Deobfuscator deobfuscator, string path, string fileName)
        {
            Log("Running...");

            var results = await Cli.Wrap(BuildPath)
                .WithArguments($"--dont-rename --keep-types --preserve-tokens \"{path}\"")
                .WithValidation(CommandResultValidation.None)
                .ExecuteFallible();

            if (deobfuscator.Verbose && results?.StandardOutput is not null)
            {
                Log(results.StandardOutput);
            }

            Log("Done.");
            return $"{fileName}-cleaned.dll";
        }
    }
}
