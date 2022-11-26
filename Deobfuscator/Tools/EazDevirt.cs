using CliWrap;
using CliWrap.Buffered;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
            repoUrl: "https://github.com/lolPants/eazdevirt",
            targetCommit: "0d287d2ab0423f0079c9aaad177762670079b3a6",
            resolveSubmodules: true
        )
        { }

        protected override async Task<string> ExecuteInternal(Deobfuscator deobfuscator, string path, string fileName)
        {
            var log = deobfuscator.Logger;
            var results = await Cli.Wrap(BuildPath)
                 .WithArguments($"-d \"{path}\"")
                 .WithValidation(CommandResultValidation.None)
                 .ExecuteBufferedAsync();

            var output = ParseOutput(results.StandardOutput);
            if (output is not null)
            {
                var ok = output.MethodProgress;
                var total = output.MethodTotal;

                if (ok != total)
                {
                    log.LogError("Devirtualized {ok} / {total} methods", ok, total);

                    if (output.MissingTypes.Count > 0)
                    {
                        foreach (var type in output.MissingTypes)
                        {
                            log.LogWarning("Missing type: {type}", type);
                        }
                    }
                    else
                    {
                        log.LogWarning("Extra errors detected! Run with --verbose for a full stack trace.");
                    }

                    log.LogDebug("{stdout}", results.StandardOutput);
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

            return $"{fileName}-devirtualized.dll";
        }

        internal record Output
        {
            public int MethodProgress { get; init; }
            public int MethodTotal { get; init; }

            public HashSet<string> MissingTypes { get; init; } = null!;
        }

        private static readonly Regex SuccessRX = new(@"Devirtualized (?<ok>\d+)/(?<total>\d+) methods", RegexOptions.Compiled);
        private static readonly Regex TypeErrorRX = new(@"dnlib\.DotNet\.TypeResolveException: Could not resolve type: (?<type>.+)", RegexOptions.Compiled);

        private static Output? ParseOutput(string stdout)
        {
            (int, int)? progress = null;
            HashSet<string> missingTypes = new();

            var lines = stdout.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            foreach (var line in lines)
            {
                var successMatch = SuccessRX.Match(line);
                if (successMatch.Success)
                {
                    var success = int.Parse(successMatch.Groups["ok"].Value);
                    var total = int.Parse(successMatch.Groups["total"].Value);

                    progress = (success, total);
                    continue;
                }

                var typeErrorMatch = TypeErrorRX.Match(line);
                if (typeErrorMatch.Success)
                {
                    string type = line.Replace(@"dnlib.DotNet.TypeResolveException: Could not resolve type: ", "").Trim();
                    missingTypes.Add(type);

                    continue;
                }
            }

            Output? output = null;
            if (progress is not null)
            {
                var (methodProgress, methodTotal) = ((int, int))progress;
                output = new Output
                {
                    MethodProgress = methodProgress,
                    MethodTotal = methodTotal,

                    MissingTypes = missingTypes,
                };
            }

            return output;
        }
    }
}
