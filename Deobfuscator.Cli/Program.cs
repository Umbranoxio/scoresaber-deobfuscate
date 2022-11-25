using CommandLine;
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

            [Option('v', "verbose", Required = false, HelpText = "Verbose logging")]
            public bool Verbose { get; set; } = false;
        }

        static async Task Main(string[] args)
        {
            var options = Parser.Default.ParseArguments<Options>(args).Value;
            if (options is null) return;

            if (options.Input == null || options.Password == null)
            {
                Console.WriteLine("Missing arguments, please try running with --help");
                Environment.Exit(1);
            }

            var deobfuscator = new Deobfuscator(options.Input, options.Password, options.Verbose, options.DependencyDirectories.ToList());
            await deobfuscator.Deobfuscate();
        }
    }
}