using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using MyToolkit.Mvvm;

namespace PowerCmd.Communication
{
    public abstract class CmdProcessController
    {
        private readonly StringBuilder _output = new StringBuilder("\n", 4 * 1024 * 1024);
        private Process _process;

        protected abstract void AppendOutput(string output);

        public abstract void OnClose();

        public abstract void OnError();

        public void Run()
        {
            CreateCmdProcess();
            RegisterStandardOutputListener();
            RegisterStandardErrorListener();
        }

        public void StopScript()
        {
            NativeConsoleUtilities.StopProgramByAttachingToItsConsoleAndIssuingCtrlCEvent(_process);
        }

        public bool WriteLine(string command)
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

                    lock (_output)
                        _output.Append(buffer, 0, count);

                    AppendOutput(new string(buffer, 0, count));
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
                    OnError();
                    lock (_output)
                        _output.Append(buffer, 0, count);

                    AppendOutput(new string(buffer, 0, count));
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
                OnClose();
            };
        }
    }
}