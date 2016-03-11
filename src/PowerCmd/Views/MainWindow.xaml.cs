using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using MyToolkit.Serialization;
using MyToolkit.Storage;
using MyToolkit.Utilities;
using PowerCmd.Communication;
using PowerCmd.ViewModels;

namespace PowerCmd.Views
{
    public partial class MainWindow : Window
    {
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

            _cmdProcessController = new UiCmdProcessController(this, Model, Dispatcher);
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
        }

        private bool WriteCommand(string command)
        {
            if (!Model.IsRunning)
            {
                if (Model.IsRunning)
                    Input.Text = command;
                else
                {
                    _cmdProcessController.Write(command);
                    Model.RunCommand(command);
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

        public void SetOutput(string output)
        {
            Output.Text = output;
            Output.ScrollToEnd();
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
    }
}
