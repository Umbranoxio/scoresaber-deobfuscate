using CommandLine;

namespace ScoreSaber_Deobfuscator
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

        [Option('l', "luluislazy", HelpText = "Uses dotnet msbuild instead of msbuild because lulu is lazy")]
        public bool DotnetMSBuild { get; set; } = false;
    }
}
