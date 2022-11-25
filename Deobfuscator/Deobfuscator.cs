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
        internal bool Verbose { get; }

        /// <summary>
        /// Working directory to perform deobfuscation
        /// </summary>
        internal string WorkingDirectory { get; }

        public Deobfuscator(string inputPath, string password, bool verbose, List<string>? dependencyDirectories = null)
        {
            InputPath = inputPath;
            DependencyDirectories = dependencyDirectories ?? new List<string>();
            Password = password;
            Verbose = verbose;

            var inputDir = Path.GetDirectoryName(inputPath);
            if (inputDir is null)
            {
                throw new NullReferenceException(nameof(inputDir));
            }

            InputDir = inputDir;
            WorkingDirectory = Path.Combine(inputDir, "deobfuscation");
        }

        public class InputNotExistsException : Exception
        {
            public InputNotExistsException(string path) : base(path) { }
        }

        public class DependencyDirNotExistsException : Exception
        {
            public DependencyDirNotExistsException(string path) : base(path) { }
        }

        public async Task Deobfuscate()
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

            await Toolchain.Setup();

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
                string cleaned = await Toolchain.de4dot.Execute(this, input);
                string devirt = await Toolchain.EazDevirt.Execute(this, cleaned);
                string eazfixed = await Toolchain.EazFixer.Execute(this, devirt);
                string output = await Toolchain.OsuDecoder.Execute(this, eazfixed);

                string nameWithoutExtension = Path.GetFileNameWithoutExtension(InputPath);
                string finalFilename = $"{nameWithoutExtension}-deobfuscated.dll";

                string outputPath = Path.Combine(wd, output);
                string finalPath = Path.Combine(InputDir, finalFilename);

                File.Copy(outputPath, finalPath, true);
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
