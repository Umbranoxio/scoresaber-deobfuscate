using Deobfuscator.Tools;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Deobfuscator
{
    public static class Toolchain
    {
        public static readonly Tool EazDevirt = new EazDevirt();
        public static readonly Tool de4dot = new de4dot();
        public static readonly Tool OsuDecoder = new OsuDecoder();
        public static readonly Tool EazFixer = new Eazfixer();

        private static bool IsSetup = false;
        private static readonly List<Tool> Tools = new()
        {
            EazDevirt,
            de4dot,
            OsuDecoder,
            EazFixer,
        };

        public static async Task Setup()
        {
            if (IsSetup) return;

            foreach (var tool in Tools)
            {
                await tool.Clone();
                await tool.Build();
            }

            IsSetup = true;
        }
    }
}