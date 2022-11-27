using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Deobfuscator
{
    public class Deobfuscator
    {
        private string InputPath { get; }
        private string InputDir { get; }
        private List<string> DependencyDirectories { get; }
        internal string Password { get; }

        /// <summary>
        /// Working directory to perform deobfuscation
        /// </summary>
        internal string WorkingDirectory { get; }

        internal ILogger Logger { get; }

        public Deobfuscator(ILoggerFactory loggerFactory, string inputPath, string password, List<string>? dependencyDirectories = null)
        {
            InputPath = inputPath;
            DependencyDirectories = dependencyDirectories ?? new List<string>();
            Password = password;

            var inputDir = Path.GetDirectoryName(inputPath);
            if (inputDir is null)
            {
                throw new NullReferenceException(nameof(inputDir));
            }

            InputDir = inputDir;

            string fileName = Path.GetFileNameWithoutExtension(inputPath);
            WorkingDirectory = Path.Combine(inputDir, $"{fileName}-deobfuscation");

            Logger = loggerFactory.CreateLogger(fileName);
        }

        public class InputNotExistsException : Exception
        {
            public InputNotExistsException(string path) : base(path) { }
        }

        public class DependencyDirNotExistsException : Exception
        {
            public DependencyDirNotExistsException(string path) : base(path) { }
        }

        public async Task Deobfuscate(Toolchain toolchain, bool dryRun = false)
        {
            if (!File.Exists(InputPath))
            {
                throw new InputNotExistsException(InputPath);
            }

            foreach (var dependencyDir in DependencyDirectories)
            {
                if (!Directory.Exists(dependencyDir))
                {
                    throw new DependencyDirNotExistsException(dependencyDir);
                }
            }

            await toolchain.Setup();

            var wd = WorkingDirectory;
            if (Directory.Exists(wd)) Directory.Delete(wd, true);
            Directory.CreateDirectory(wd);

            foreach (var dependencyDir in DependencyDirectories)
            {
                var source = new DirectoryInfo(dependencyDir);
                source.DeepCopy(wd);
            }

            string fileName = Path.GetFileName(InputPath);
            string input = Path.Combine(wd, fileName);
            File.Copy(InputPath, input);

            try
            {
                string cleaned = await toolchain.de4dot.Execute(this, input);
                string devirt = await toolchain.EazDevirt.Execute(this, cleaned);
                string eazfixed = await toolchain.EazFixer.Execute(this, devirt);
                string output = await toolchain.OsuDecoder.Execute(this, eazfixed);

                string nameWithoutExtension = Path.GetFileNameWithoutExtension(InputPath);
                string finalFilename = $"{nameWithoutExtension}-deobfuscated.dll";

                string outputPath = Path.Combine(wd, output);
                string finalPath = Path.Combine(InputDir, finalFilename);

                if (!dryRun) File.Copy(outputPath, finalPath, true);
            }
            catch (Tool.OutputNotExistsException)
            {
                // Pass
            }
            finally
            {
                Directory.Delete(WorkingDirectory, true);
            }
        }
    }
}
