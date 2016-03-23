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
using MyToolkit.Utilities;
using PowerCmd.Communication;
using PowerCmd.Extensions.SuggestionProviders;
using PowerCmd.Models;
using PowerCmd.ViewModels;

namespace PowerCmd.Views
{
    public partial class MainWindow : Window
    {
        private int _maxOutputLength = 1024 * 32;
        private UiCmdProcessController _cmdProcessController;
        private readonly List<ISuggestionProvider> _suggestionProviders;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            Closed += OnClosed;

            CheckForApplicationUpdate();
            LoadSettings();

            Activated += (sender, args) => { Input.Focus(); };

            _suggestionProviders = new List<ISuggestionProvider>
            {
                new CdHistoryProvider(Model),
                new DefaultSuggestionProvider(Model)
            };
        }

        private void LoadSettings()
        {
            Model.RootDirectory = ApplicationSettings.GetSetting("RootDirectory", "C:/");

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

            var args = Environment.GetCommandLineArgs();
            var currentDirectory = args.Length > 1 ? args[1] : ApplicationSettings.GetSetting("CurrentDirectory", "C:/");
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

            ApplicationSettings.SetSetting("RootDirectory", Model.RootDirectory);

            ApplicationSettings.SetSetting("WindowWidth", Width);
            ApplicationSettings.SetSetting("WindowHeight", Height);
            ApplicationSettings.SetSetting("WindowLeft", Left);
            ApplicationSettings.SetSetting("WindowTop", Top);
            ApplicationSettings.SetSetting("WindowState", WindowState);

            ApplicationSettings.SetSettingWithXmlSerializer("CommandButtons", new List<CommandButton>(Model.CommandButtons));
        }

        private void OnInputPreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                e.Handled = true;
                if (Input.Text.StartsWith("cd "))
                {
                    if (!Input.Text.EndsWith("/") && !Input.Text.EndsWith("\\"))
                    {
                        if (Directory.Exists(Path.Combine(Model.CurrentWorkingDirectory, Input.Text.Substring(3))))
                            Input.AppendText("/");
                        else
                            SelectFirstSuggestion();
                    }
                    else
                        SelectFirstSuggestion();

                    UpdateSuggestions();
                }
                else
                    Input.SelectSuggestion();
            }
        }

        private void SelectFirstSuggestion()
        {
            var suggestions = (IEnumerable<string>)Input.ItemsSource;
            if (suggestions.Any())
                Input.SetText(((IEnumerable<string>)Input.ItemsSource).First());
            else
                Input.SelectSuggestion();
        }

        private async void OnInputKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var commandButton = Model.CommandButtons.FirstOrDefault(b => b.Alias == Input.Text.ToLowerInvariant());
                if (commandButton != null)
                    Input.Text = commandButton.Text;

                WriteCommand(Input.Text);
                Input.Text = "";
            }
            else if (e.Key == Key.C && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                e.Handled = true;
                OnStopScript(null, null);
            }

            await UpdateSuggestions();
        }

        private bool _suggestionsRunning = false;
        private bool _suggestionsDirty = false;

        private async Task UpdateSuggestions()
        {
            if (!_suggestionsRunning)
            {
                _suggestionsRunning = true;
                _suggestionsDirty = false;

                await Input.SetSuggestionsAsync(async (command) =>
                {
                    var suggestionProvider = _suggestionProviders.FirstOrDefault(p => p.SupportsCommand(command));
                    if (suggestionProvider != null)
                        return await suggestionProvider.GetSuggestionsAsync(command);

                    return new string[] { };
                });

                _suggestionsRunning = false;

                if (_suggestionsDirty)
                    await UpdateSuggestions();
            }
            else
                _suggestionsDirty = true;
        }

        private void WriteCommand(string command)
        {
            if (!Model.IsRunning)
                Model.RunCommand(command);

            _cmdProcessController.WriteLine(command);
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
        private bool _wasError = false;

        public async void AppendOutput(string output, bool isError)
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                if ((DateTime.Now - _lastUpdate).TotalMilliseconds > 200 || _wasError != isError)
                {
                    if (_wasError != isError)
                    {
                        AppendOutputDirectly(_outputCache.ToString(), _wasError);
                        AppendOutputDirectly(output, isError);
                    }
                    else
                        AppendOutputDirectly(_outputCache + output, isError);

                    _outputCache.Clear();
                    _lastUpdate = DateTime.Now;
                    _updateRequested = false;
                }
                else if (!_updateRequested)
                {
                    _outputCache.Append(output);
                    _updateRequested = true;
                    _wasError = isError;

                    await Task.Delay(300);
                    AppendOutput(string.Empty, isError);
                }
                else
                    _outputCache.Append(output);
            });
        }

        private async void AppendOutputDirectly(string output, bool isError)
        {
            if (string.IsNullOrEmpty(output))
                return;

            Model.LastCommand?.AppendOutput(output);

            if (!isError)
            {
                var currentWorkingDirectory = TryFindCurrentWorkingDirectory(output);
                if (currentWorkingDirectory != null)
                {
                    Model.CurrentWorkingDirectory = currentWorkingDirectory;
                    Model.IsRunning = false;
                }
                else
                    Model.IsRunning = true;
            }

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

        private void OnDirectoryDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var listBox = (ListBox)sender;
            var directory = listBox.SelectedItem.ToString();
            listBox.SelectedItem = null;

            var command = "cd \"" + Path.Combine(Model.RootDirectory, directory) + "\"";
            WriteCommand(command);

            Input.Focus();
        }

        private void OnDirectoryKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                OnDirectoryDoubleClick(sender, null);
        }
    }
}
