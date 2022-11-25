using System;
using CommandLine.Text;
using CommandLine;

namespace ScoreSaber_Deobfuscator
{
    internal class CliOptions
    {
        [Option('i', HelpText = "Path of the input file")]
        public string Input { get; set; }

        [Option('d',  HelpText = "Path of the ScoreSaber Dependency files")]
        public string DependencyPath { get; set; }

        [Option('p',  HelpText = "Symbol password")]
        public string Password { get; set; }

        [Option('v', HelpText = "Verbose logging")]
        public bool Verbose { get; set; } = false;
    }
}
