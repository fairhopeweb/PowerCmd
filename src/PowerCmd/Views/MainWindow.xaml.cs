using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using MyToolkit.Serialization;
using MyToolkit.Storage;
using MyToolkit.UI;
using MyToolkit.Utilities;
using PowerCmd.Communication;
using PowerCmd.ViewModels;

namespace PowerCmd.Views
{
    public partial class MainWindow : Window
    {
        private int _maxOutputLength = 1024 * 32;
        private UiCmdProcessController _cmdProcessController;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            Closed += OnClosed;

            CheckForApplicationUpdate();
            LoadSettings();

            Activated += (sender, args) => { Input.Focus(); };
        }

        private void LoadSettings()
        {
            Width = ApplicationSettings.GetSetting("WindowWidth", Width);
            Height = ApplicationSettings.GetSetting("WindowHeight", Height);
            Left = ApplicationSettings.GetSetting("WindowLeft", Left);
            Top = ApplicationSettings.GetSetting("WindowTop", Top);
            WindowState = ApplicationSettings.GetSetting("WindowState", WindowState);

            if (Left == double.NaN)
                WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var defaultCommandButtons = CreateDefaultCommandButtons();
            try
            {
                Model.CommandButtons = new ObservableCollection<CommandButton>(ApplicationSettings.GetSettingWithXmlSerializer("CommandButtons", defaultCommandButtons));
            }
            catch
            {
                Model.CommandButtons = new ObservableCollection<CommandButton>(defaultCommandButtons);
            }
        }

        private static List<CommandButton> CreateDefaultCommandButtons()
        {
            var defaultButtons = new List<CommandButton>
            {
                new CommandButton
                {
                    Title = "VS2015",
                    Subtitle = "Developer Prompt",
                    Alias = "vs2015",
                    Text = @"""C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools\VsDevCmd.bat"""
                },
                new CommandButton
                {
                    Title = "VS2013",
                    Subtitle = "Developer Prompt",
                    Alias = "vs2013",
                    Text = @"""C:\Program Files (x86)\Microsoft Visual Studio 12.0\Common7\Tools\VsDevCmd.bat"""
                },
                new CommandButton
                {
                    Title = "VS2012",
                    Subtitle = "Developer Prompt",
                    Alias = "vs2012",
                    Text = @"""C:\Program Files (x86)\Microsoft Visual Studio 11.0\Common7\Tools\VsDevCmd.bat"""
                }
            };
            return defaultButtons;
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

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            Input.Focus();

            var currentDirectory = ApplicationSettings.GetSetting("CurrentDirectory", "C:/");
            if (Directory.Exists(currentDirectory))
            {
                Directory.SetCurrentDirectory(currentDirectory);
                Model.CurrentWorkingDirectory = currentDirectory;
            }
            else
                Model.CurrentWorkingDirectory = Directory.GetCurrentDirectory();

            _cmdProcessController = new UiCmdProcessController(this, Model);
            _cmdProcessController.Run();
        }

        private void OnClosed(object sender, EventArgs eventArgs)
        {
            var match = Regex.Match(Output.Text, "^.*?(\n(.*))>$", RegexOptions.Multiline);
            if (match.Success)
                ApplicationSettings.SetSetting("CurrentDirectory", match.Groups[2].Value, false, true);

            ApplicationSettings.SetSetting("WindowWidth", Width);
            ApplicationSettings.SetSetting("WindowHeight", Height);
            ApplicationSettings.SetSetting("WindowLeft", Left);
            ApplicationSettings.SetSetting("WindowTop", Top);
            ApplicationSettings.SetSetting("WindowState", WindowState);

            ApplicationSettings.SetSettingWithXmlSerializer("CommandButtons", new List<CommandButton>(Model.CommandButtons));
        }

        private void OnInputKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var commandButton = Model.CommandButtons.FirstOrDefault(b => b.Alias == Input.Text.ToLowerInvariant());
                if (commandButton != null)
                    Input.Text = commandButton.Text;

                if (WriteCommand(Input.Text))
                    Input.Text = "";
            }
            else if (e.Key == Key.C && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                e.Handled = true; 
                OnStopScript(null, null);
            }
        }

        private bool WriteCommand(string command)
        {
            if (!Model.IsRunning)
            {
                if (Model.IsRunning)
                    Input.Text = command;
                else
                {
                    Model.RunCommand(command);
                    _cmdProcessController.WriteLine(command);
                }
                return true;
            }
            return false;
        }

        private void OnCommandButtonClicked(object sender, RoutedEventArgs e)
        {
            var command = (CommandButton)((Button)sender).Tag;
            WriteCommand(command.Text);
            Input.Focus();
        }

        private bool _updateRequested = false;
        private readonly StringBuilder _outputCache = new StringBuilder();
        private DateTime _lastUpdate = DateTime.MinValue;

        public async void AppendOutput(string output)
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                if ((DateTime.Now - _lastUpdate).TotalMilliseconds > 200)
                {
                    AppendOutputDirectly(_outputCache + output);
                    _outputCache.Clear();

                    _lastUpdate = DateTime.Now;
                    _updateRequested = false;
                }
                else if (!_updateRequested)
                {
                    _outputCache.Append(output);
                    _updateRequested = true;

                    await Task.Delay(300);
                    AppendOutput(string.Empty);
                }
                else
                    _outputCache.Append(output);
            });
        }

        private async void AppendOutputDirectly(string output)
        {
            var currentWorkingDirectory = TryFindCurrentWorkingDirectory(output);
            if (currentWorkingDirectory != null)
            {
                Model.CurrentWorkingDirectory = currentWorkingDirectory;
                Model.IsRunning = false;
            }
            else
                Model.IsRunning = true;
            Model.LastCommand?.AppendOutput(output);

            Output.BeginChange();
            Output.AppendText(output);
            if (Output.Document.TextLength > _maxOutputLength)
                Output.Document.Remove(0, Output.Document.TextLength - _maxOutputLength);
            Output.EndChange();

            Output.ScrollToVerticalOffset(double.MaxValue);

            // TODO: Remove hack (used to always scroll to end)
            await Task.Delay(1000);
            Output.ScrollToVerticalOffset(double.MaxValue);
        }

        private string TryFindCurrentWorkingDirectory(string text)
        {
            var match = Regex.Match("\n" + text, "^.*?(\n(.*))>$", RegexOptions.Multiline);
            if (match.Success)
            {
                var path = match.Groups[2].Value;
                if (Directory.Exists(path))
                    return path;
            }
            return null;
        }

        private void OnSaveCommandButtons(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog();
            dlg.Filter = "PowerCmd command buttons (*.pcmdb)|*.pcmdb";
            dlg.RestoreDirectory = true;
            dlg.AddExtension = true;
            if (dlg.ShowDialog() == true)
                File.WriteAllText(dlg.FileName, XmlSerialization.Serialize(Model.CommandButtons.ToList()));
        }

        private void OnLoadCommandButtons(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Title = "Open PowerCmd command buttons file";
            dlg.Filter = "PowerCmd command buttons (*.pcmdb)|*.pcmdb";
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog() == true)
            {
                Model.CommandButtons = new ObservableCollection<CommandButton>(
                    XmlSerialization.Deserialize<List<CommandButton>>(File.ReadAllText(dlg.FileName))
                );
            }
        }

        private void OnStopScript(object sender, RoutedEventArgs e)
        {
            _cmdProcessController.StopScript();
        }

        private void OnCopyPath(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(Model.CurrentWorkingDirectory);
        }
    }
}
