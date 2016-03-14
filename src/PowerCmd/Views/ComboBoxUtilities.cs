using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace PowerCmd.Views
{
    public static class ComboBoxUtilities
    {
        public static void SelectSuggestion(this ComboBox comboBox)
        {
            comboBox.Focus();

            var editableTextBox = (TextBox)comboBox.Template.FindName("PART_EditableTextBox", comboBox);
            editableTextBox.SelectionStart = comboBox.Text.Length;
            editableTextBox.SelectionLength = 0;
        }

        public static void AppendText(this ComboBox comboBox, string text)
        {
            comboBox.Focus();

            var editableTextBox = (TextBox)comboBox.Template.FindName("PART_EditableTextBox", comboBox);
            editableTextBox.AppendText(text);
            editableTextBox.SelectionStart = comboBox.Text.Length;
            editableTextBox.SelectionLength = 0;
        }

        public static void SetText(this ComboBox comboBox, string text)
        {
            comboBox.Focus();
            comboBox.Text = text;

            var editableTextBox = (TextBox)comboBox.Template.FindName("PART_EditableTextBox", comboBox);
            editableTextBox.SelectionStart = comboBox.Text.Length;
            editableTextBox.SelectionLength = 0;
        }

        public static async Task SetSuggestionsAsync(this ComboBox comboBox, Func<string, Task<IEnumerable<string>>> suggestionProvider)
        {
            var editableTextBox = (TextBox)comboBox.Template.FindName("PART_EditableTextBox", comboBox);
            var previousText = comboBox.Text;
            var previousStart = editableTextBox.SelectionStart;
            var command = editableTextBox.SelectionStart >= 0 ? editableTextBox.Text.Substring(0, editableTextBox.SelectionStart) : editableTextBox.Text;

            comboBox.ItemsSource = await suggestionProvider(command);

            if (comboBox.Text != previousText && previousStart >= 0)
            {
                comboBox.Text = previousText;
                editableTextBox.SelectionStart = previousStart;
                editableTextBox.SelectionLength = editableTextBox.Text.Length - previousStart;
            }
        }
    }
}