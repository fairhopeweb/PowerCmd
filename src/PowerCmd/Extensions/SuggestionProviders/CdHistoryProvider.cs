using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PowerCmd.ViewModels;

namespace PowerCmd.Extensions.SuggestionProviders
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
                command = command.Replace("\\", "/");

                var cwd = _model.CurrentWorkingDirectory;
                var segments = command.Length > 3
                    ? command.Substring(3).Split('/')
                    : new string[] { };

                var prefix = string.Join("/", segments.Take(segments.Length - 1));
                cwd = segments.Length > 1 ? Path.Combine(cwd, prefix) : cwd;

                var directories = (await Task.Run(() => Directory.GetDirectories(cwd)))
                    .Select(p => "cd " + (!string.IsNullOrEmpty(prefix) ? prefix + "/" + Path.GetFileName(p) : Path.GetFileName(p))).ToList();

                if (string.IsNullOrEmpty(prefix))
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