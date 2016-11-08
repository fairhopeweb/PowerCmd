using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Rendering;

namespace PowerCmd.Extensions.Highlighting
{
    internal class PathColorizer : DocumentColorizingTransformer
    {
        private SolidColorBrush _greenBrush;

        public PathColorizer()
        {
            _greenBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#40932b"));
        }

        protected override void ColorizeLine(ICSharpCode.AvalonEdit.Document.DocumentLine line)
        {
            string lineText = CurrentContext.Document.GetText(line);

            var match = Regex.Match(lineText, "^(.*)>", RegexOptions.Multiline);
            if (match.Success)
            {
                var path = match.Groups[0].Value;
                path = path.Remove(path.Length - 1);

                if (Directory.Exists(path))
                {
                    ChangeLinePart(line.Offset, line.Offset+path.Length+1, ApplyChanges);
                }
            }
        }

        void ApplyChanges(VisualLineElement element)
        {
            element.TextRunProperties.SetForegroundBrush(_greenBrush);
        }
    }
}