using System.Windows.Media;

namespace PowerCmd.Models
{
    public class CommandExecutionResult
    {
        public string Key { get; set; }

        public string Value { get; set; }

        public Color Color { get; set; } = Colors.White;
    }
}