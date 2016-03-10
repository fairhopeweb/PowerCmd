using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PowerCmd
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Process _process;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            Input.Focus();

            _process = Process.Start(new ProcessStartInfo("cmd.exe")
            {
                CreateNoWindow = true,
                UseShellExecute = false,

                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = true
            });

            _process.EnableRaisingEvents = true;

            var outputThread = new Thread(new ParameterizedThreadStart(delegate
            {
                var buffer = new char[1024 * 10];
                while (true)
                {
                    var count = _process.StandardOutput.Read(buffer, 0, buffer.Length);
                    AddText(buffer, count);
                }
            }));
            outputThread.IsBackground = true;
            outputThread.Start();


            AppDomain.CurrentDomain.ProcessExit += (s, eventArgs) =>
            {
                Environment.Exit(0);
            };

            var errorThread = new Thread(new ParameterizedThreadStart(delegate
            {
                var buffer = new char[1024];
                while (true)
                {
                    var count = _process.StandardError.Read(buffer, 0, buffer.Length);
                    AddText(buffer, count);
                }
            }));
            errorThread.IsBackground = true;
            errorThread.Start();

            _process.Exited += (s, eventArgs) =>
            {
                Close();
            };
        }

        private StringBuilder _output = new StringBuilder();
        private bool _updating = false; 

        private void AddText(char[] buffer, int count)
        {
            lock (_output)
            {
                _output.Append(buffer, 0, count);
                if (!_updating)
                {
                    _updating = true;

                    Dispatcher.InvokeAsync(() =>
                    {
                        var text = ""; 
                        lock (_output)
                        {
                            text = _output.Length > 100 ? _output.ToString(_output.Length - 100, 100) : _output.ToString();
                            _updating = false;
                        }

                        Output.Text = text; 
                        Output.ScrollToEnd();
                    });
                }
            }
        }

        private void Input_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _process.StandardInput.WriteLine(Input.Text);
                Input.Text = "";
            }
        }

        public void RunCommand(Command command)
        {
            _process.StandardInput.WriteLine(command.Text);
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            RunCommand(new Command
            {
                Text = @"""C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools\VsDevCmd.bat"""
            });
        }
    }

    public class Command
    {
        public string Title { get; set; }

        public string Text { get; set; }
    }
}
