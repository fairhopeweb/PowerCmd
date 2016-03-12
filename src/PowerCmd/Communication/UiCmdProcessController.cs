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

        public UiCmdProcessController(MainWindow view, MainWindowModel model)
        {
            _view = view; 
            _model = model; 
        }

        protected override void AppendOutput(string output)
        {
            _view.AppendOutput(output);
        }
        
        public override void OnClose()
        {
            _view.Dispatcher.InvokeAsync(() =>
            {
                _view.Close();
            });
        }

        public override void OnError()
        {
            _view.Dispatcher.InvokeAsync(() =>
            {
                if (_model.LastCommand != null)
                    _model.LastCommand.HasErrors = true;
            });
        }
    }
}