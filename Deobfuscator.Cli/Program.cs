using CommandLine;
using System;
using System.Threading.Tasks;

namespace Deobfuscator.Cli
{
    internal class Program
    {
        internal class CliOptions
        {
            [Option('i', HelpText = "Path of the input file")]
            public string Input { get; set; } = null!;

            [Option('d', HelpText = "Path of the ScoreSaber Dependency files")]
            public string DependencyPath { get; set; } = null!;

            [Option('p', HelpText = "Symbol password")]
            public string Password { get; set; } = null!;

            [Option('v', HelpText = "Verbose logging")]
            public bool Verbose { get; set; } = false;
        }

        static async Task Main(string[] args)
        {
            var options = Parser.Default.ParseArguments<CliOptions>(args).Value;
            if (options is null) return;

            if (options.Input == null || options.DependencyPath == null || options.Password == null)
            {
                Console.WriteLine("Missing arguments, please try running with --help");
                Environment.Exit(1);
            }

            var deobfuscator = new Deobfuscator(options.Input, options.Password, options.Verbose, new() { options.DependencyPath });
            await deobfuscator.Deobfuscate();
        }
    }
}