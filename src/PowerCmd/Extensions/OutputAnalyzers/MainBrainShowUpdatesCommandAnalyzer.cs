using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PowerCmd.Models;
using PowerCmd.ViewModels;

namespace PowerCmd.Extensions.OutputAnalyzers
{
    public class MainBrainShowUpdatesCommandAnalyzer : IOutputAnalyzer
    {
        public async Task<bool> SupportsCommandAsync(CommandExecutionInfo command)
        {
            return command.Command.ToLowerInvariant().StartsWith("msbuild /t:su") && !command.HasErrors;
        }

        public async Task AnalyzeAsync(CommandExecutionInfo command)
        {
            command.Results.Add(new CommandExecutionResult
            {
                Key = "Number of NuGet Package Updates",
                Value = ReadNumberOfUpdates(command).ToString()
            });
        }

        private int ReadNumberOfUpdates(CommandExecutionInfo command)
        {
            return Regex.Matches(command.Output, "Repository Version:", RegexOptions.Multiline)
                .Cast<Match>()
                .Count(match => match.Success);
        }
    }
}