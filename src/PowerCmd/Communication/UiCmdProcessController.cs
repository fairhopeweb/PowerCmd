using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using MyToolkit.UI;
using PowerCmd.ViewModels;
using PowerCmd.Views;

namespace PowerCmd.Communication
{
    public class UiCmdProcessController : CmdProcessController
    {
        private readonly MainWindow _view;
        private readonly MainWindowModel _model;

        public UiCmdProcessController(MainWindow view, MainWindowModel model, Dispatcher dispatcher) : base(new UiDispatcher(dispatcher))
        {
            _view = view; 
            _model = model; 
        }

        public override void OnOutputChanged(string output)
        {
            var currentWorkingDirectory = TryFindCurrentWorkingDirectory(output);
            if (currentWorkingDirectory != null)
            {
                _model.CurrentWorkingDirectory = currentWorkingDirectory;
                _model.IsRunning = false;
            }
            else
                _model.IsRunning = true;

            _view.SetOutput(output);
        }

        public override void OnClose()
        {
            _view.Close();
        }

        public override void OnError()
        {
            if (_model.LastCommand != null)
                _model.LastCommand.HasErrors = true;
        }

        private string TryFindCurrentWorkingDirectory(string text)
        {
            var match = Regex.Match(text, "^.*?(\n(.*))>$", RegexOptions.Multiline);
            if (match.Success)
            {
                var path = match.Groups[2].Value;
                if (Directory.Exists(path))
                    return path;
            }
            return null;
        }
    }
}