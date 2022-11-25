using CliWrap;
using CliWrap.Buffered;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Deobfuscator.Tools
{
    internal class EazDevirt : Tool
    {
        internal EazDevirt() : base(
            path: Path.Combine(Environment.CurrentDirectory, "eazdevirt"),
            buildPath: Path.Combine(Environment.CurrentDirectory, "eazdevirt", "bin", "Release", "eazdevirt.exe"),
            slnName: "eazdevirt",
            repoUrl: "https://github.com/Umbranoxio/eazdevirt",
            resolveSubmodules: true
        )
        { }

        protected override async Task<string> ExecuteInternal(Deobfuscator deobfuscator, string path, string fileName)
        {
            Log("Running...");

            var results = await Cli.Wrap(BuildPath)
                 .WithArguments($"-d \"{path}\"")
                 .WithValidation(CommandResultValidation.None)
                 .ExecuteBufferedAsync();

            if (deobfuscator.Verbose)
            {
                Log(results.StandardOutput);
            }
            else
            {
                var lines = results.StandardOutput.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                foreach (var line in lines)
                {
                    if (line.Contains("methods"))
                    {
                        Log(line);
                    }
                }
            }

            Log("Done.");
            return $"{fileName}-devirtualized.dll";
        }
    }
}
