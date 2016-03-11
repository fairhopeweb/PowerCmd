using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using MyToolkit.Command;
using MyToolkit.Mvvm;

namespace PowerCmd.ViewModels
{
    public class CommandButton
    {
        public string Title { get; set; }

        public string Text { get; set; }
    }

    public class MainWindowModel : ViewModelBase
    {
        private string _currentWorkingDirectory;
        private bool _isRunning;

        public MainWindowModel()
        {
            OpenCurrentWorkingDirectoryInExplorerCommand = new RelayCommand(OpenCurrentWorkingDirectoryInExplorer);
        }

        public ICommand OpenCurrentWorkingDirectoryInExplorerCommand { get; private set; }

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


        public ObservableCollection<CommandExecutionInfo> CommandHistory { get; } = new ObservableCollection<CommandExecutionInfo>();

        public ObservableCollection<CommandButton> CommandButtons { get; } = new ObservableCollection<CommandButton>
        {
            new CommandButton { Title = "VS2015", Text = @"""C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools\VsDevCmd.bat""" },
            new CommandButton { Title = "VS2013", Text = @"""C:\Program Files (x86)\Microsoft Visual Studio 12.0\Common7\Tools\VsDevCmd.bat""" },
            new CommandButton { Title = "VS2012", Text = @"""C:\Program Files (x86)\Microsoft Visual Studio 11.0\Common7\Tools\VsDevCmd.bat""" }
        };

        /// <summary>Gets or sets the currentWorkingDirectory. </summary>
        public string CurrentWorkingDirectory
        {
            get { return _currentWorkingDirectory; }
            set { Set(ref _currentWorkingDirectory, value); }
        }

        /// <summary>Gets or sets the last command. </summary>
        public CommandExecutionInfo LastCommand => CommandHistory.Any() ? CommandHistory.First() : null;

        public void RunCommand(string command)
        {
            IsRunning = true; 
            CommandHistory.Insert(0, new CommandExecutionInfo(command, CurrentWorkingDirectory));
            RaisePropertyChanged(() => LastCommand);
        }

        private void OpenCurrentWorkingDirectoryInExplorer()
        {
            Process.Start(CurrentWorkingDirectory);
        }
    }
}
