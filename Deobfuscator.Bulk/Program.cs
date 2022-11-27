using CommandLine;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Deobfuscator.Bulk
{
    internal class Program
    {
        internal class Options
        {
            [Option('f', "versions", Required = true, HelpText = "Path to versions.tsv file")]
            public string VersionsFile { get; set; } = null!;

            [Option('V', "version", Required = false, HelpText = "Only run on a single version")]
            public string? Version { get; set; }

            [Option('p', "password", Required = true, HelpText = "Symbol password")]
            public string Password { get; set; } = null!;

            [Option('d', "dry-run", Required = false, HelpText = "Don't output a deobfuscated DLL")]
            public bool DryRun { get; set; } = false;

            [Option('v', "verbose", Required = false, HelpText = "Verbose logging")]
            public bool Verbose { get; set; } = false;
        }

        static async Task Main(string[] args)
        {
            var options = Parser.Default.ParseArguments<Options>(args).Value;
            if (options is null) return;

            using var loggerFactory = LoggerFactory.Create(builder =>
                builder.AddFilter(null, options.Verbose ? LogLevel.Trace : LogLevel.Information)
                .AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.TimestampFormat = "HH:mm:ss ";
                })
            );

            var log = loggerFactory.CreateLogger("Program");

            if (!File.Exists(options.VersionsFile))
            {
                log.LogCritical("Versions file does not exist!");
                Environment.Exit(1);
            }

            string? root = Path.GetDirectoryName(options.VersionsFile);
            if (root is null)
            {
                throw new NullReferenceException(nameof(root));
            }

            string versionsString = await File.ReadAllTextAsync(options.VersionsFile);
            List<VersionInfo> versions = versionsString.Split('\n')
                .Skip(1)
                .Where(line => line != string.Empty)
                .Select(line => new VersionInfo(root, line))
                .ToList();

            var toolchain = new Toolchain(loggerFactory);
            await toolchain.Setup();

            foreach (var version in versions)
            {
                if (options.Version is not null && version.Version != options.Version) continue;

                var path = version.Filepath;
                if (path is null)
                {
                    log.LogWarning("{version} does not exist!", version);
                    continue;
                }

                List<string?> dependencies = new()
                {
                    version.GameAssembliesDep,
                    version.LibsDep,
                    version.PluginsDep,
                };

                var deps = dependencies.Where(x => x is not null).Cast<string>().ToList();
                var deobfuscator = new Deobfuscator(loggerFactory, path, options.Password, deps);

                await deobfuscator.Deobfuscate(toolchain, options.DryRun);
            }
        }
    }
}
