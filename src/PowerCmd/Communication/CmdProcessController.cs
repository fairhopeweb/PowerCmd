using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using MyToolkit.Mvvm;

namespace PowerCmd.Communication
{
    public abstract class CmdProcessController
    {
        private int _maxTextLength = 1024 * 128;

        private readonly IDispatcher _dispatcher;
        private readonly StringBuilder _output = new StringBuilder("\n", 4 * 1024 * 1024);

        private System.Diagnostics.Process _process;
        private bool _updating = false;

        public CmdProcessController(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public abstract void OnOutputChanged(string output);

        public abstract void OnClose();

        public abstract void OnError();

        public void Run()
        {
            CreateCmdProcess();
            RegisterStandardOutputListener();
            RegisterStandardErrorListener();
        }

        public bool Write(string command)
        {
            try
            {
                _process.StandardInput.WriteLine(command);
                return true; 
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception.ToString());
                return false; 
            }
        }

        private void RegisterStandardOutputListener()
        {
            var outputThread = new Thread(new ParameterizedThreadStart(delegate
            {
                var buffer = new char[1024 * 512];
                while (true)
                {
                    var count = _process.StandardOutput.Read(buffer, 0, buffer.Length);
                    AppendLineInternal(buffer, count);
                }

            }));
            outputThread.IsBackground = true;
            outputThread.Start();
        }

        private void RegisterStandardErrorListener()
        {
            var errorThread = new Thread(new ParameterizedThreadStart(delegate
            {
                var buffer = new char[1024 * 512];
                while (true)
                {
                    var count = _process.StandardError.Read(buffer, 0, buffer.Length);
                    AppendLineInternal(buffer, count);
                    _dispatcher.InvokeAsync(OnError);
                }
            }));
            errorThread.IsBackground = true;
            errorThread.Start();
        }

        private void CreateCmdProcess()
        {
            _process = Process.Start(new ProcessStartInfo("cmd.exe")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = true
            });

            _process.EnableRaisingEvents = true;
            _process.Exited += (s, eventArgs) =>
            {
                _dispatcher.InvokeAsync(OnClose);
            };
        }

        private void AppendLineInternal(char[] buffer, int count)
        {
            lock (_output)
            {
                _output.Append(buffer, 0, count);
                if (!_updating)
                {
                    _updating = true;
                    _dispatcher.InvokeAsync(() =>
                    {
                        var text = "";

                        lock (_output)
                        {
                            text = _output.Length > _maxTextLength ? 
                                _output.ToString(_output.Length - _maxTextLength, _maxTextLength) : 
                                _output.ToString();

                            _updating = false;
                        }

                        OnOutputChanged(text);
                    });
                }
            }
        }
    }
}