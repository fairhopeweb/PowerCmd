using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;
using MyToolkit.Command;
using MyToolkit.Mvvm;
using MyToolkit.Utilities;

namespace PowerCmd.ViewModels
{
    public class MainWindowModel : ViewModelBase
    {
        private ObservableCollection<CommandButton> _commandButtons;
        private string _currentWorkingDirectory;
        private bool _isRunning;
        private string _rootDirectory = string.Empty;
        private IEnumerable<string> _directories;

        public MainWindowModel()
        {
            OpenCurrentWorkingDirectoryInExplorerCommand = new RelayCommand(OpenCurrentWorkingDirectoryInExplorer);
        }

        public ICommand OpenCurrentWorkingDirectoryInExplorerCommand { get; private set; }

        public ObservableCollection<CommandExecutionInfo> CommandHistory { get; } = new ObservableCollection<CommandExecutionInfo>();

        /// <summary>Gets or sets the root directory. </summary>
        public string RootDirectory
        {
            get { return _rootDirectory; }
            set
            {
                if (Set(ref _rootDirectory, value))
                    LoadDirectoriesAsync();
            }
        }

        /// <summary>Gets or sets the directories. </summary>
        public IEnumerable<string> Directories
        {
            get { return _directories; }
            set { Set(ref _directories, value); }
        }

        /// <summary>Gets or sets the command buttons. </summary>
        public ObservableCollection<CommandButton> CommandButtons
        {
            get { return _commandButtons; }
            set { Set(ref _commandButtons, value); }
        }

        /// <summary>Gets or sets the currentWorkingDirectory. </summary>
        public string CurrentWorkingDirectory
        {
            get { return _currentWorkingDirectory; }
            set { Set(ref _currentWorkingDirectory, value); }
        }

        /// <summary>Gets or sets a value indicating whether a command is running. </summary>
        public bool IsRunning
        {
            get { return _isRunning; }
            set
            {
                if (Set(ref _isRunning, value))
                    LastCommand?.Complete();
            }
        }

        /// <summary>Gets or sets the last command. </summary>
        public CommandExecutionInfo LastCommand => CommandHistory.Any() ? CommandHistory.First() : null;

        /// <summary>Gets the application version with build time. </summary>
        public string ApplicationVersion => "v" + GetType().Assembly.GetVersionWithBuildTime();

        public void RunCommand(string command)
        {
            if (!IsRunning)
            {
                IsRunning = true;
                CommandHistory.Insert(0, new CommandExecutionInfo(command, CurrentWorkingDirectory));
                RaisePropertyChanged(() => LastCommand);
            }
        }

        private void OpenCurrentWorkingDirectoryInExplorer()
        {
            Process.Start(CurrentWorkingDirectory);
        }

        private void LoadDirectoriesAsync()
        {
            if ((RootDirectory.Contains("/") || RootDirectory.Contains("\\")) && Directory.Exists(RootDirectory))
            {
                try
                {
                    Directories = Directory.GetDirectories(RootDirectory).Select(Path.GetFileName);
                }
                catch
                {
                    Directories = new string[] { };
                }
            }
            else
                Directories = new string[] { };
        }
    }
}