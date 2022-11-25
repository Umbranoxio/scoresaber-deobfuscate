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

            public Exception(StringBuilder stdOutput, StringBuilder stdError) : base(Output(stdOutput, stdError))
            {
                StdOutput = stdOutput.ToString();
                StdError = stdError.ToString();
            }

            private static string Output(StringBuilder stdOutput, StringBuilder stdError)
            {
                var builder = new StringBuilder();

                var output = stdOutput.ToString();
                if (output != string.Empty)
                {
                    builder.Append($"\nstdout:\n{output}\n");
                }

                var error = stdError.ToString();
                if (error != string.Empty)
                {
                    builder.Append($"\nstderr:\n{error}\n");
                }

                return builder.ToString();
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
