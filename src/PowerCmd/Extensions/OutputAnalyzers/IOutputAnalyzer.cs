using System.Threading.Tasks;
using PowerCmd.Models;
using PowerCmd.ViewModels;

namespace PowerCmd.Extensions.OutputAnalyzers
{
    public interface IOutputAnalyzer
    {
        Task<bool> SupportsCommandAsync(CommandExecutionInfo command);

        Task AnalyzeAsync(CommandExecutionInfo command);
    }
}