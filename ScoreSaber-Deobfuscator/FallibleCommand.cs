using CliWrap;
using System.Text;
using System.Threading.Tasks;

namespace ScoreSaber_Deobfuscator
{
    internal static class FallibleCommand
    {
        public class Exception : System.Exception
        {
            public string StdOutput { get; private set; }
            public string StdError { get; private set; }

            public Exception(StringBuilder stdOutput, StringBuilder stdError) : base($"stdout:\n{stdOutput}\n\nstderr:\n{stdError}")
            {
                StdOutput = stdOutput.ToString();
                StdError = stdError.ToString();
            }
        }

        public static async Task<CommandResult?> ExecuteFallible(this Command command)
        {
            var stdOut = new StringBuilder();
            var stdErr = new StringBuilder();

            var result = await command
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOut))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErr))
                .ExecuteAsync();

            if (result.ExitCode != 0)
            {
                throw new Exception(stdOut, stdErr);
            }

            return result;
        }
    }
}
