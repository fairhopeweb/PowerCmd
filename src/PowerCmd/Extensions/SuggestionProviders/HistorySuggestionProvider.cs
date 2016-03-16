using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PowerCmd.ViewModels;

namespace PowerCmd.Extensions.SuggestionProviders
{
    public class HistorySuggestionProvider : ISuggestionProvider
    {
        private readonly MainWindowModel _model;

        public HistorySuggestionProvider(MainWindowModel model)
        {
            _model = model;
        }

        public bool SupportsCommand(string command)
        {
            return true;
        }

        public async Task<IEnumerable<string>> GetSuggestionsAsync(string command)
        {
            return _model.CommandHistory.Select(c => c.Command);
        }
    }
}