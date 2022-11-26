using System;
using System.IO;

namespace Deobfuscator.Bulk
{
    internal class VersionInfo
    {
        public enum EPlatform
        {
            Steam,
            Oculus,
            Universal,
        }

        internal string Filename { get; }
        internal string Version { get; }
        internal EPlatform Platform { get; }
        internal DateTime? ReleaseDate { get; }
        internal string GameVersion { get; }
        internal string SHA1 { get; }

        private string Root { get; }
        internal string? Filepath
        {
            get
            {
                var path = Path.Combine(Root, GameVersion, Filename);
                if (File.Exists(path)) return path;

                return null;
            }
        }

        internal string? GameAssembliesDep { get => DependencyDirectory("Game Assemblies"); }
        internal string? LibsDep { get => DependencyDirectory("Libs"); }
        internal string? PluginsDep { get => DependencyDirectory("Plugins"); }

        private string? DependencyDirectory(string name)
        {
            var path = Path.Combine(Root, GameVersion, name);
            if (Directory.Exists(path)) return path;

            return null;
        }

        public VersionInfo(string root, string line)
        {
            Root = root;

            string[] lines = line.Split('\t');
            if (lines.Length != 6)
            {
                throw new Exception("not enough data for this line");
            }

            Filename = lines[0];
            Version = lines[1];
            GameVersion = lines[4];
            SHA1 = lines[5];

            Platform = lines[2] switch
            {
                "steam" => EPlatform.Steam,
                "oculus" => EPlatform.Oculus,
                "universal" => EPlatform.Universal,
                _ => throw new Exception("unknown platform"),
            };

            string releaseDate = lines[3];
            ReleaseDate = releaseDate switch
            {
                "unknown" => null,
                _ => DateTime.Parse(releaseDate),
            };
        }

        public override string ToString() => Filename;
    }
}
