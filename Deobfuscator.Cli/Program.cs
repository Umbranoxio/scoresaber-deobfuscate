using CommandLine;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Deobfuscator.Cli
{
    internal class Program
    {
        internal class Options
        {
            [Option('i', "input", Required = true, HelpText = "Path of the input file")]
            public string Input { get; set; } = null!;

            [Option('d', "dependency-directory", HelpText = "Path of the ScoreSaber Dependency files")]
            public IEnumerable<string> DependencyDirectories { get; set; } = null!;

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

            if (options.Input == null || options.Password == null)
            {
                log.LogCritical("Missing arguments, please try running with --help");
                Environment.Exit(1);
            }

            var toolchain = new Toolchain(loggerFactory);
            await toolchain.Setup();

            var deobfuscator = new Deobfuscator(loggerFactory, options.Input, options.Password, options.DependencyDirectories.ToList());
            await deobfuscator.Deobfuscate(toolchain, options.DryRun);
        }
    }
}