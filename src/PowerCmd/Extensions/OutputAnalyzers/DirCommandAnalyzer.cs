using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PowerCmd.Models;
using PowerCmd.ViewModels;

namespace PowerCmd.Extensions.OutputAnalyzers
{
    public class DirCommandAnalyzer : IOutputAnalyzer
    {
        public async Task<bool> SupportsCommandAsync(CommandExecutionInfo command)
        {
            return command.Command.ToLowerInvariant().StartsWith("dir") && !command.HasErrors;
        }

        public async Task AnalyzeAsync(CommandExecutionInfo command)
        {
            command.Results.Add(new CommandExecutionResult
            {
                Key = "Number of Directories",
                Value = ReadNumberOfDirectories(command).ToString()
            });

            command.Results.Add(new CommandExecutionResult
            {
                Key = "Number of Files",
                Value = ReadNumberOfFiles(command).ToString()
            });
        }

        private int ReadNumberOfDirectories(CommandExecutionInfo command)
        {
            return Regex.Matches(command.Output, "<DIR>", RegexOptions.Multiline)
                .Cast<Match>()
                .Count(match => match.Success);
        }

        private int ReadNumberOfFiles(CommandExecutionInfo command)
        {
            return Regex.Matches(command.Output, "(AM|PM)             ", RegexOptions.Multiline)
                .Cast<Match>()
                .Count(match => match.Success);
        }
    }
}