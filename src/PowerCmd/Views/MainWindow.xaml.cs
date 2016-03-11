using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MyToolkit.Storage;
using MyToolkit.Utilities;
using PowerCmd.ViewModels;

namespace PowerCmd.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int _maxTextLength = 1024 * 128;
        private Process _process;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            Closed += OnClosed;

            DataContext = this;
            //Input.Loaded += myCombo_Loaded;

            CheckForApplicationUpdate();
        }

        private async void CheckForApplicationUpdate()
        {
            var updater = new ApplicationUpdater(
                "PowerCmd.msi",
                GetType().Assembly,
                "http://rsuter.com/Projects/PowerCmd/updates.php");

            await updater.CheckForUpdate(this);
        }

        public MainWindowModel Model => (MainWindowModel)Resources["Model"];

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
            {
                Directory.SetCurrentDirectory(currentDirectory);
                Model.CurrentWorkingDirectory = currentDirectory;
            }
            else
                Model.CurrentWorkingDirectory = Directory.GetCurrentDirectory();

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
                var buffer = new char[1024 * 512];
                while (true)
                {
                    var count = _process.StandardOutput.Read(buffer, 0, buffer.Length);
                    AddText(buffer, count, false);
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
                var buffer = new char[1024 * 512];
                while (true)
                {
                    var count = _process.StandardError.Read(buffer, 0, buffer.Length);
                    AddText(buffer, count, true);
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

        private StringBuilder _output = new StringBuilder("\n", 4 * 1024 * 1024);
        private bool _updating = false;

        private void AddText(char[] buffer, int count, bool isError)
        {
            lock (_output)
            {
                _output.Append(buffer, 0, count);
                if (!_updating)
                {
                    _updating = true;

                    Dispatcher.InvokeAsync(() =>
                    {
                        if (Model.LastCommand != null && isError)
                            Model.LastCommand.HasErrors = true; 

                        var text = "";
                        lock (_output)
                        {
                            text = _output.Length > _maxTextLength ? _output.ToString(_output.Length - _maxTextLength, _maxTextLength) : _output.ToString();
                            _updating = false;
                        }

                        var currentWorkingDirectory = TryFindCurrentWorkingDirectory(text);
                        if (currentWorkingDirectory != null)
                        {
                            Model.CurrentWorkingDirectory = currentWorkingDirectory;
                            Model.IsRunning = false;
                        }
                        else
                            Model.IsRunning = true; 

                        Output.Text = text;
                        Output.ScrollToEnd();
                    });
                }
            }
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

        private void Input_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (WriteCommand(Input.Text))
                    Input.Text = "";
            }
        }

        public void RunCommand(CommandButton commandButton)
        {
            WriteCommand(commandButton.Text);
        }

        private bool WriteCommand(string command)
        {
            if (!Model.IsRunning)
            {
                _process.StandardInput.WriteLine(command);
                Model.RunCommand(command);
                return true; 
            }
            return false; 
        }
        
        private void OnCommandButtonClicked(object sender, RoutedEventArgs e)
        {
            var command = (CommandButton)((Button) sender).Tag;
            RunCommand(command);
            Input.Focus();
        }
    }
}
