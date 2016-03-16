using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using MyToolkit.Model;
using PowerCmd.Extensions.OutputAnalyzers;

namespace PowerCmd.Models
{
    public class CommandExecutionInfo : ObservableObject
    {
        private static readonly List<IOutputAnalyzer> _analyzers = new List<IOutputAnalyzer>
        {
            new DirCommandAnalyzer(), 
            new MsBuildCommandAnalyzer(),
            new MainBrainShowUpdatesCommandAnalyzer()
        }; 

        private string _command;
        private string _workingDirectory;
        private readonly StringBuilder _output = new StringBuilder(128 * 1024);

        private DateTime _startTime;
        private DateTime? _endTime;
        private bool _hasErrors;

        public CommandExecutionInfo(string command, string workingDirectory)
        {
            Command = command;
            WorkingDirectory = workingDirectory;
            StartTime = DateTime.Now;
        }

        /// <summary>Gets or sets the command. </summary>
        public string Command
        {
            get { return _command; }
            set { Set(ref _command, value); }
        }

        /// <summary>Gets or sets the working directory. </summary>
        public string WorkingDirectory
        {
            get { return _workingDirectory; }
            set { Set(ref _workingDirectory, value); }
        }

        /// <summary>Gets or sets the start time. </summary>
        public DateTime StartTime
        {
            get { return _startTime; }
            set { Set(ref _startTime, value); }
        }

        /// <summary>Gets or sets the end time. </summary>
        public DateTime? EndTime
        {
            get { return _endTime; }
            set
            {
                if (Set(ref _endTime, value))
                {
                    RaisePropertyChanged(() => Duration);
                    RaisePropertyChanged(() => IsRunning);
                }
            }
        }

        public bool IsRunning => !Duration.HasValue;

        /// <summary>Gets or sets a value indicating whether the command has errors. </summary>
        public bool HasErrors
        {
            get { return _hasErrors; }
            set { Set(ref _hasErrors, value); }
        }

        /// <summary>Gets or sets the duration. </summary>
        public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : (TimeSpan?)null;

        public ObservableCollection<CommandExecutionResult> Results { get; } = new ObservableCollection<CommandExecutionResult>();

        public string Output => _output.ToString();

        public void AppendOutput(string output)
        {
            _output.Append(output);
        }

        public async Task CompleteAsync()
        {
            if (!EndTime.HasValue)
            {
                EndTime = DateTime.Now;

                foreach (var analyzer in _analyzers)
                {
                    if (await analyzer.SupportsCommandAsync(this))
                        await analyzer.AnalyzeAsync(this);
                }
            }
        }
    }
}