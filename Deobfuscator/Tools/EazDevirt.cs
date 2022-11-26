using CliWrap;
using CliWrap.Buffered;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Deobfuscator.Tools
{
    internal class EazDevirt : Tool
    {
        internal EazDevirt(ILogger logger) : base(
            logger: logger,
            path: Path.Combine(Environment.CurrentDirectory, "eazdevirt"),
            buildPath: Path.Combine(Environment.CurrentDirectory, "eazdevirt", "bin", "Release", "eazdevirt.exe"),
            slnName: "eazdevirt",
            repoUrl: "https://github.com/Umbranoxio/eazdevirt",
            resolveSubmodules: true
        )
        { }

        protected override async Task<string> ExecuteInternal(Deobfuscator deobfuscator, string path, string fileName)
        {
            var log = deobfuscator.Logger;
            log.LogInformation("Running...");

            var results = await Cli.Wrap(BuildPath)
                 .WithArguments($"-d \"{path}\"")
                 .WithValidation(CommandResultValidation.None)
                 .ExecuteBufferedAsync();

            var output = ParseOutput(results.StandardOutput);
            if (output is not null)
            {
                var (ok, total) = ((int, int))output;
                if (ok != total)
                {
                    log.LogError("Devirtualized {ok} / {total} methods", ok, total);
                    log.LogError("{stdout}", results.StandardOutput);
                }
                else
                {
                    log.LogInformation("Devirtualized {ok} / {total} methods", ok, total);
                }
            }
            else
            {
                log.LogInformation("{stdout}", results.StandardOutput);
            }

            log.LogInformation("Done.");
            return $"{fileName}-devirtualized.dll";
        }

        private static readonly Regex DevirtRX = new(@"Devirtualized (?<ok>\d+)/(?<total>\d+) methods", RegexOptions.Compiled);
        private static (int, int)? ParseOutput(string stdout)
        {
            var lines = stdout.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            foreach (var line in lines)
            {
                var match = DevirtRX.Match(line);
                if (match.Success)
                {
                    string ok = match.Groups["ok"].Value;
                    string total = match.Groups["total"].Value;

                    return (int.Parse(ok), int.Parse(total));
                }
            }

            return null;
        }
    }
}
