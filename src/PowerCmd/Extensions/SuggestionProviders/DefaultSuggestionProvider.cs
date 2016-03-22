using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PowerCmd.ViewModels;

namespace PowerCmd.Extensions.SuggestionProviders
{
    public class DefaultSuggestionProvider : ISuggestionProvider
    {
        private readonly MainWindowModel _model;

        public DefaultSuggestionProvider(MainWindowModel model)
        {
            _model = model;
        }

        public bool SupportsCommand(string command)
        {
            return true;
        }

        public async Task<IEnumerable<string>> GetSuggestionsAsync(string command)
        {
            var suggestions = new List<string>();
            try
            {
                suggestions.AddRange(Directory.GetFiles(_model.CurrentWorkingDirectory).Select(Path.GetFileName));
            }
            catch { }
            suggestions.AddRange(_model.CommandHistory.Select(c => c.Command));
            return suggestions;
        }
    }
}