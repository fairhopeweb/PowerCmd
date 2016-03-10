using MyToolkit.Storage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
        private int _maxTextLength = 1024 * 128;
        private Process _process;

        public ObservableCollection<string> History { get; } = new ObservableCollection<string>();

        public MainWindow()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            Closed += OnClosed;

            DataContext = this;
            //Input.Loaded += myCombo_Loaded;
        }

        //private void myCombo_Loaded(object sender, System.Windows.RoutedEventArgs e)
        //{
        //    ControlTemplate ct = Input.Template;
        //    Popup pop = ct.FindName("PART_Popup", Input) as Popup;
        //    pop.Placement = PlacementMode.Top;
        //}

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            var currentDirectory = ApplicationSettings.GetSetting("CurrentDirectory", "C:/");
            if (Directory.Exists(currentDirectory))
                Directory.SetCurrentDirectory(currentDirectory);

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
                var buffer = new char[1024 * 128];
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
                var buffer = new char[1024 * 128];
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
                Dispatcher.InvokeAsync(() =>
                {
                    Close();
                });
            };
        }

        private void OnClosed(object sender, EventArgs eventArgs)
        {
            var match = Regex.Match(Output.Text, "^.*?(\n(.*))>$", RegexOptions.Multiline);
            if (match.Success)
                ApplicationSettings.SetSetting("CurrentDirectory", match.Groups[2].Value, false, true);
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
                            text = _output.Length > _maxTextLength ? _output.ToString(_output.Length - _maxTextLength, _maxTextLength) : _output.ToString();
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
                WriteCommand(Input.Text);
                Input.Text = "";
            }
        }

        public void RunCommand(Command command)
        {
            WriteCommand(command.Text);
        }

        private void WriteCommand(string command)
        {
            History.Insert(0, command);
            _process.StandardInput.WriteLine(command);
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            RunCommand(new Command
            {
                Text = @"""C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools\VsDevCmd.bat"""
            });
            Input.Focus();
        }
        private void ButtonBase_OnClick2(object sender, RoutedEventArgs e)
        {
            RunCommand(new Command
            {
                Text = @"""C:\Program Files (x86)\Microsoft Visual Studio 12.0\Common7\Tools\VsDevCmd.bat"""
            });
            Input.Focus();
        }

        private void ButtonBase_OnClick3(object sender, RoutedEventArgs e)
        {
            RunCommand(new Command
            {
                Text = @"""C:\Program Files (x86)\Microsoft Visual Studio 11.0\Common7\Tools\VsDevCmd.bat"""
            });
            Input.Focus();
        }
    }

    public class Command
    {
        public string Title { get; set; }

        public string Text { get; set; }
    }
}
