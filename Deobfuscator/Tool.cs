using CliWrap;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Deobfuscator
{
    public abstract class Tool
    {
        #region Properties
        internal ILogger Logger { get; }

        internal string ToolPath { get; private set; }
        internal string BuildPath { get; private set; }
        internal string? BuildPathDirectory => Path.GetDirectoryName(BuildPath);


        internal string SlnName { get; private set; }
        internal string RepoUrl { get; private set; }
        internal string? TargetCommit { get; private set; }
        internal bool RestoreNugetPackages { get; private set; }
        internal bool ResolveSubmodules { get; private set; }
        internal bool IsEmpty
        {
            get
            {
                if (Directory.Exists(ToolPath))
                {
                    if (Directory.GetFiles(ToolPath).Length > 0)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
        #endregion

        internal Tool(ILogger logger, string path, string buildPath, string slnName, string repoUrl, string? targetCommit = null, bool restoreNugetPackages = false, bool resolveSubmodules = false)
        {
            Logger = logger;
            ToolPath = path;
            BuildPath = buildPath;
            SlnName = slnName;
            RepoUrl = repoUrl;
            TargetCommit = targetCommit;
            RestoreNugetPackages = restoreNugetPackages;
            ResolveSubmodules = resolveSubmodules;
        }

        #region Utilities
        public override string ToString()
        {
            return ToolPath;
        }
        #endregion

        #region Setup
        /// <summary>
        /// Clones all the repos required to devirtualize, absolutely 0 error handling
        /// </summary>
        internal async Task Clone()
        {
            if (!IsEmpty) return;

            Logger.LogInformation("Cloning...");
            await Cli.Wrap("git")
                .WithArguments($"clone \"{RepoUrl}\" \"{ToolPath}\"")
                .WithValidation(CommandResultValidation.None)
                .ExecuteFallible();

            if (TargetCommit != string.Empty)
            {
                await Cli.Wrap("git")
                    .WithArguments($"reset --hard {TargetCommit}")
                    .WithWorkingDirectory(ToolPath)
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteFallible();

                Logger.LogInformation($"Repo reset to {TargetCommit}");
            }

            if (ResolveSubmodules)
            {
                Logger.LogInformation("Resolving submodules...");
                await Cli.Wrap("git")
                    .WithArguments("submodule init")
                    .WithWorkingDirectory(ToolPath)
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteFallible();

                await Cli.Wrap("git")
                    .WithArguments("submodule update")
                    .WithWorkingDirectory(ToolPath)
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteFallible();

                Logger.LogInformation("Submodules resolved.");
            }

            Logger.LogInformation("Cloned.");
        }

        /// <summary>
        /// Builds all the newly cloned repos, once again no error handling, good luck
        /// </summary>
        internal async Task Build()
        {
            if (File.Exists(BuildPath)) return;

            if (RestoreNugetPackages)
            {
                Logger.LogInformation("Restoring Nuget packages...");
                await Cli.Wrap("msbuild")
                    .WithWorkingDirectory(ToolPath)
                    .WithArguments("-t:restore")
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteFallible();

                Logger.LogInformation("Nuget packages resolved");
            }

            Logger.LogInformation("Building...");
            await Cli.Wrap("msbuild")
                .WithArguments($"\"{ToolPath}\\{SlnName}.sln\" /p:Configuration=Release")
                .WithValidation(CommandResultValidation.None)
                .ExecuteFallible();

            Logger.LogInformation($"Built.");
        }
        #endregion

        #region Processing
        protected abstract Task<string> ExecuteInternal(Deobfuscator deobfuscator, string path, string fileName);

        internal async Task<string> Execute(Deobfuscator deobfuscator, string inputFile)
        {
            using (deobfuscator.Logger.BeginScope(SlnName))
            {
                string path = Path.Combine(deobfuscator.WorkingDirectory, inputFile);
                string fileName = Path.GetFileNameWithoutExtension(inputFile);
                string outputFile = await ExecuteInternal(deobfuscator, path, fileName);

                string outputPath = Path.Combine(deobfuscator.WorkingDirectory, outputFile);
                EnsureOutput(deobfuscator, outputPath);

                return outputFile;
            }
        }

        public class OutputNotExistsException : Exception { }
        private static void EnsureOutput(Deobfuscator deobfuscator, string outputPath)
        {
            if (!File.Exists(outputPath))
            {
                deobfuscator.Logger.LogError("Failed, aborting...");
                throw new OutputNotExistsException();
            }
        }
        #endregion
    }
}
