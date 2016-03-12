using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;
using MyToolkit.Model;

namespace PowerCmd.ViewModels
{
    public class CommandExecutionInfo : ObservableObject
    {
        private string _command;
        private string _workingDirectory;
        private StringBuilder _output = new StringBuilder(128 * 1024);

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

        public void Complete()
        {
            if (!EndTime.HasValue)
            {
                EndTime = DateTime.Now;

                // TODO: Refactor and use strategy pattern
                AddDirResults();
                AddMsBuildResults();
                AddMbMsbuildResults();
            }
        }

        private void AddDirResults()
        {
            if (Command.ToLowerInvariant().StartsWith("dir") && !HasErrors)
            {
                Results.Add(new CommandExecutionResult
                {
                    Key = "Number of Directories",
                    Value = ReadNumberOfDirectories().ToString()
                });

                Results.Add(new CommandExecutionResult
                {
                    Key = "Number of Files",
                    Value = ReadNumberOfFiles().ToString()
                });
            }
        }

        private int ReadNumberOfDirectories()
        {
            return Regex.Matches(Output, "<DIR>", RegexOptions.Multiline)
                .Cast<Match>()
                .Count(match => match.Success);
        }

        private int ReadNumberOfFiles()
        {
            return Regex.Matches(Output, "(AM|PM)             ", RegexOptions.Multiline)
                .Cast<Match>()
                .Count(match => match.Success);
        }

        private void AddMbMsbuildResults()
        {
            if (Command.ToLowerInvariant().StartsWith("msbuild /t:su") && !HasErrors)
            {
                Results.Add(new CommandExecutionResult
                {
                    Key = "Number of NuGet Package Updates",
                    Value = ReadNumberOfUpdates().ToString()
                });
            }
        }

        private int ReadNumberOfUpdates()
        {
            return Regex.Matches(Output, "Repository Version:", RegexOptions.Multiline)
                .Cast<Match>()
                .Count(match => match.Success);
        }

        private void AddMsBuildResults()
        {
            if (Command.ToLowerInvariant().StartsWith("msbuild") && !HasErrors)
            {
                Results.Add(new CommandExecutionResult
                {
                    Key = "Warnings",
                    Color = Colors.Orange,
                    Value = ReadMsBuildCounter("Warning").ToString()
                });

                var errors = ReadMsBuildCounter("Error"); 
                Results.Add(new CommandExecutionResult
                {
                    Key = "Errors",
                    Color = Colors.Red,
                    Value = errors.ToString()
                });

                if (errors > 0 || Output.Contains("Build FAILED."))
                    HasErrors = true; 
            }
        }

        private int ReadMsBuildCounter(string type)
        {
            var count = 0;
            foreach (Match match in Regex.Matches(Output, "    ([0-9]*) " + type + "\\(s\\)", RegexOptions.Multiline))
            {
                if (match.Success)
                {
                    var matchCount = 0; 
                    if (int.TryParse(match.Groups[1].Value, out matchCount))
                        count += matchCount;
                }
            }
            return count;
        }
    }
}