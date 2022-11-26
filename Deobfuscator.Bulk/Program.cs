using CommandLine;
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
            [Option('f', "versions", Required = true, HelpText = "Path to versions tsv file")]
            public string VersionsFile { get; set; } = null!;

            [Option('p', "password", Required = true, HelpText = "Symbol password")]
            public string Password { get; set; } = null!;

            [Option('v', "verbose", Required = false, HelpText = "Verbose logging")]
            public bool Verbose { get; set; } = false;
        }

        static async Task Main(string[] args)
        {
            var options = Parser.Default.ParseArguments<Options>(args).Value;
            if (options is null) return;

            if (!File.Exists(options.VersionsFile))
            {
                Console.WriteLine("Versions file does not exist!");
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

            await Toolchain.Setup();
            foreach (var version in versions)
            {
                if (!version.Exists)
                {
                    Console.WriteLine($"{version} does not exist!");
                    continue;
                }

                var deobfuscator = new Deobfuscator(version.Filepath, options.Password, options.Verbose);
                await deobfuscator.Deobfuscate();
            }
        }
    }
}