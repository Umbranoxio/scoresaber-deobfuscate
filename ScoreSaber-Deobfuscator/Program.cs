﻿using CliWrap;
using CliWrap.Buffered;
using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ScoreSaber_Deobfuscator
{
    internal class Program
    {
        internal static CliOptions Options = new();

        static void Main(string[] args)
        {
            Options = Parser.Default.ParseArguments<CliOptions>(args).Value;
            if (Options is null) return;

            if (Options.Input == null && Options.DependencyPath == null && Options.Password == null)
            {
                Console.WriteLine("Missing arguments, please try running with -help");
                Environment.Exit(0);
            }

            if (!File.Exists(Options.Input))
            {
                Console.WriteLine($"File {Options.Input} doesn't exist");
            }

            MainAsync().Wait();
        }

        static async Task MainAsync()
        {
            var tools = SetupTools();

            foreach (var tool in tools)
            {
                await CloneTool(tool);
                await BuildTool(tool);
            }

            await Deobfuscate(tools);
        }


        /// <summary>
        /// Sets up all the tools required, creates target directories if needed
        /// </summary>
        static List<ToolInformation> SetupTools()
        {
            var tools = new List<ToolInformation>
            {
                new ToolInformation()
                {
                    Path = Path.Combine(Environment.CurrentDirectory, "eazdevirt"),
                    BuildPath = Path.Combine(Environment.CurrentDirectory, "eazdevirt", "bin", "Release", "eazdevirt.exe"),
                    SlnName = "eazdevirt",
                    RepoUrl = "https://github.com/Umbranoxio/eazdevirt",
                    RestoreNugetPackages = false,
                    ResolveSubmodules = true,
                },

                new ToolInformation() {
                    Path = Path.Combine(Environment.CurrentDirectory, "de4dot"),
                    BuildPath = Path.Combine(Environment.CurrentDirectory, "de4dot", "Release", "de4dot.exe"),
                    SlnName = "de4dot",
                    RepoUrl = "https://github.com/de4dot/de4dot",
                    RestoreNugetPackages = false,
                    ResolveSubmodules = true,
                    TargetCommit = "f279bed1ed5b65d3243ed21cb4e4ad7048e6abb1"
                },

                new ToolInformation() {
                    Path = Path.Combine(Environment.CurrentDirectory, "osu-decoder"),
                    BuildPath = Path.Combine(Environment.CurrentDirectory, "osu-decoder", "osu!decoder", "bin", "Release", "osu!decoder.exe"),
                    SlnName = "osu!decoder",
                    RepoUrl = "https://github.com/Umbranoxio/osu-decoder",
                    RestoreNugetPackages = true,
                    ResolveSubmodules = true
                },

                new ToolInformation() {
                    Path = Path.Combine(Environment.CurrentDirectory, "EazFixer"),
                    BuildPath = Path.Combine(Environment.CurrentDirectory, "EazFixer", "EazFixer", "bin", "Release", "net472", "EazFixer.exe"),
                    SlnName= "EazFixer",
                    RepoUrl= "https://github.com/holly-hacker/EazFixer",
                    RestoreNugetPackages= true,
                    ResolveSubmodules= false,
                }
            };

            foreach (var tool in tools)
            {
                Directory.CreateDirectory(tool.Path);
            }

            return tools;
        }

        /// <summary>
        /// Clones all the repos required to devirtualize ScoreSaber, absolutely 0 error handling
        /// </summary>
        static async Task CloneTool(ToolInformation tool)
        {
            if (!tool.IsEmpty) return;

            tool.Log("Cloning...");

            await Cli.Wrap("git").WithArguments($"clone {tool.RepoUrl} {tool.Path}").WithValidation(CommandResultValidation.None).ExecuteAsync();

            if (tool.TargetCommit != String.Empty)
            {
                await Cli.Wrap("git")
                    .WithArguments($"reset --hard {tool.TargetCommit}")
                    .WithWorkingDirectory(tool.Path)
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteAsync();
                tool.Log($"Repo reset to {tool.TargetCommit}");
            }

            if (tool.ResolveSubmodules)
            {
                tool.Log("Resolving submodules...");
                await Cli.Wrap("git")
                    .WithArguments("submodule init")
                    .WithWorkingDirectory(tool.Path)
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteAsync();
                await Cli.Wrap("git")
                    .WithArguments("submodule update")
                    .WithWorkingDirectory(tool.Path)
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteAsync();
                tool.Log("Submodules resolved.");
            }

            tool.Log("Cloned.");
        }

        /// <summary>
        /// Builds all the newly cloned repos, once again no error handling, good luck
        /// </summary>
        static async Task BuildTool(ToolInformation tool)
        {
            if (File.Exists(tool.BuildPath)) return;

            var msBuildCommand = Options.DotnetMSBuild ? "dotnet msbuild" : "msbuild";

            if (tool.RestoreNugetPackages)
            {
                tool.Log("Restoring Nuget packages...");
                await Cli.Wrap(msBuildCommand)
                    .WithWorkingDirectory(tool.Path)
                    .WithArguments("-t:restore")
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteBufferedAsync();
                tool.Log("Nuget packages resolved");
            }

            tool.Log("Building...");
            await Cli.Wrap(msBuildCommand)
                .WithArguments($"{tool.Path}\\{tool.SlnName}.sln /p:Configuration=Release")
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync();

            tool.Log($"Built.");
        }

        /// <summary>
        /// Runs through all of the tools in correct order, didn't feel like doing this in an iterator, feel free to PR
        /// </summary>
        static async Task Deobfuscate(List<ToolInformation> tools)
        {
            var de4dot = tools.Where(x => x.SlnName == "de4dot").First();
            var eazdevirt = tools.Where(x => x.SlnName == "eazdevirt").First();
            var EazFixer = tools.Where(x => x.SlnName == "EazFixer").First();
            var osuDecoder = tools.Where(x => x.SlnName == "osu!decoder").First();

            string fileName = Path.GetFileNameWithoutExtension(Options.Input);
            string currentPath = Path.Combine(Options.DependencyPath, fileName + ".dll");

            if (!File.Exists(currentPath))
            {
                File.Copy(Options.Input, currentPath);
            }

            de4dot.Log("Running...");

            var de4dotResults = await Cli.Wrap(de4dot.BuildPath)
                .WithArguments($"--dont-rename --keep-types --preserve-tokens {currentPath}")
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync();

            if (Options.Verbose) de4dot.Log(de4dotResults.StandardOutput);

            de4dot.Log("Done.");

            currentPath = Path.Combine(Options.DependencyPath, $"{fileName}-cleaned.dll");
            CheckPath(de4dot, currentPath);

            eazdevirt.Log("Running...");

            var devirtResults = await Cli.Wrap(eazdevirt.BuildPath)
                 .WithArguments($"-d {currentPath}")
                 .WithValidation(CommandResultValidation.None)
                 .ExecuteBufferedAsync();

            if (Options.Verbose)
            {
                eazdevirt.Log(devirtResults.StandardOutput);
            }
            else
            {
                PrintDevirtBasicInfo(eazdevirt, devirtResults.StandardOutput);
            }

            eazdevirt.Log("Done.");

            currentPath = Path.Combine(Options.DependencyPath, $"{fileName}-cleaned-devirtualized.dll");
            CheckPath(eazdevirt, currentPath);

            EazFixer.Log("Running...");

            var eazFixerResults = await Cli.Wrap(EazFixer.BuildPath)
                .WithArguments($"--file {currentPath}")
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync();

            if (Options.Verbose) EazFixer.Log(eazFixerResults.StandardOutput);

            EazFixer.Log("Done.");

            currentPath = Path.Combine(Options.DependencyPath, $"{fileName}-cleaned-devirtualized-eazfix.dll");
            CheckPath(EazFixer, currentPath);

            osuDecoder.Log("Running...");

            var osuDecoderResults = await Cli.Wrap(osuDecoder.BuildPath)
               .WithArguments($"-i {currentPath} -p {Options.Password}")
               .WithValidation(CommandResultValidation.None)
               .ExecuteBufferedAsync();

            if (Options.Verbose) osuDecoder.Log(osuDecoderResults.StandardOutput);

            osuDecoder.Log("Done.");

            CleanUp(true);
        }

        /// <summary>
        /// Checks the next expected path based on the tool provided, if it doesn't exist, we assume the tool failed
        /// </summary>
        static void CheckPath(ToolInformation tool, string expectedPath)
        {
            if (!File.Exists(expectedPath))
            {
                tool.Log("Failed, aborting...");
                CleanUp(false);
                Environment.Exit(0);
            }
        }

        /// <summary>
        /// Yep
        /// </summary>
        static void CleanUp(bool success)
        {
            string fileName = Path.GetFileNameWithoutExtension(Options.Input);

            if (Path.GetDirectoryName(Options.Input) != Options.DependencyPath.Trim())
            {
                BlindDelete(Path.Combine(Options.DependencyPath, fileName + ".dll"));
            }

            BlindDelete(Path.Combine(Options.DependencyPath, $"{fileName}-cleaned.dll"));
            BlindDelete(Path.Combine(Options.DependencyPath, $"{fileName}-cleaned-devirtualized.dll"));
            BlindDelete(Path.Combine(Options.DependencyPath, $"{fileName}-cleaned-devirtualized-eazfix.dll"));

            string lastFile = Path.Combine(Options.DependencyPath, $"{fileName}-cleaned-devirtualized-eazfix-decrypted.dll");
            if (File.Exists(lastFile))
            {
                var dir = Path.GetDirectoryName(Options.Input);
                if (dir is null)
                {
                    throw new Exception("file is not in a directory");
                }

                string finishedFile = Path.Combine(dir, $"{fileName}-Deobfuscated.dll");
                BlindDelete(finishedFile);
                File.Move(lastFile, finishedFile);
            }
        }

        /// <summary>
        /// I forget if File.Delete makes this check, so doing it myself
        /// </summary>
        static void BlindDelete(string fileName)
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }

        /// <summary>
        /// Prints the amount of methods devirtualized if we're not verbosely logging
        /// </summary>
        static void PrintDevirtBasicInfo(ToolInformation tool, string devirtResults)
        {
            var lines = devirtResults.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            foreach (var line in lines)
            {
                if (line.Contains("methods"))
                {
                    tool.Log(line);
                }
            }
        }

    }

    internal class ToolInformation
    {
        internal string Path { get; init; } = null!;
        internal string BuildPath { get; init; } = null!;
        internal string? BuildPathDirectory => System.IO.Path.GetDirectoryName(BuildPath);


        internal string SlnName { get; init; } = null!;
        internal string RepoUrl { get; init; } = null!;
        internal string TargetCommit { get; init; } = null!;
        internal bool RestoreNugetPackages { get; init; }
        internal bool ResolveSubmodules { get; init; }
        internal bool IsEmpty
        {
            get
            {
                if (Directory.Exists(Path))
                {
                    if (Directory.GetFiles(Path).Length > 0)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        internal void Log(string log)
        {
            Console.WriteLine($"[{SlnName}] {log}");
        }

        public override string ToString()
        {
            return Path;
        }
    }
}
