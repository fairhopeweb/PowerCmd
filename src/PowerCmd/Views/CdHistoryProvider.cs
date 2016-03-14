using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PowerCmd.ViewModels;

namespace PowerCmd.Views
{
    public class CdHistoryProvider : ISuggestionProvider
    {
        private readonly MainWindowModel _model;

        public CdHistoryProvider(MainWindowModel model)
        {
            _model = model;
        }

        public bool SupportsCommand(string command)
        {
            return command == "cd" || command.StartsWith("cd ");
        }

        public async Task<IEnumerable<string>> GetSuggestionsAsync(string command)
        {
            try
            {
                var cwd = _model.CurrentWorkingDirectory;
                var directories = (await Task.Run(() => Directory.GetDirectories(cwd))).Select(p => "cd " + Path.GetFileName(p)).ToList();
                directories.Insert(0, "cd ..");
                return directories;
            }
            catch
            {
                return new string[] { };
            }
        }
    }
}