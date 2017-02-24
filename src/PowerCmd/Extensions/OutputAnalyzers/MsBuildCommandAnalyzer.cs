using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using PowerCmd.Models;
using PowerCmd.ViewModels;

namespace PowerCmd.Extensions.OutputAnalyzers
{
    public class MsBuildCommandAnalyzer : IOutputAnalyzer
    {
        public async Task<bool> SupportsCommandAsync(CommandExecutionInfo command)
        {
            return command.Command.ToLowerInvariant().StartsWith("msbuild") && !command.HasErrors;
        }

        public async Task AnalyzeAsync(CommandExecutionInfo command)
        {
            command.Results.Add(new CommandExecutionResult
            {
                Key = "Warnings",
                Color = Colors.Khaki,
                Value = ReadMsBuildCounter(command, "Warning").ToString()
            });

            var errors = ReadMsBuildCounter(command, "Error");
            command.Results.Add(new CommandExecutionResult
            {
                Key = "Errors",
                Color = Colors.IndianRed,
                Value = errors.ToString()
            });

            if (errors > 0 || command.Output.Contains("Build FAILED."))
                command.HasErrors = true;
        }

        private int ReadMsBuildCounter(CommandExecutionInfo command, string type)
        {
            var count = 0;
            foreach (Match match in Regex.Matches(command.Output, "    ([0-9]*) " + type + "\\(s\\)", RegexOptions.Multiline))
            {
                if (match.Success)
                {
                    var matchCount = 0;
                    if (int.TryParse(match.Groups[1].Value, out matchCount))
                        count += matchCount;
                }
            }
            return count;
        }
    }
}