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

        public override void OnOutputAppended(string output)
        {
            _view.Dispatcher.InvokeAsync(() =>
            {
                _view.AppendOutput(output);
            });
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
    }
}