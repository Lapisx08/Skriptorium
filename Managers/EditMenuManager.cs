using System.Windows;
using Skriptorium.UI;

namespace Skriptorium.Managers
{
    public class EditMenuManager
    {
        private readonly ScriptTabManager _tabManager;

        public EditMenuManager(ScriptTabManager tabManager)
        {
            _tabManager = tabManager;
        }

        public void Undo()
        {
            var editor = _tabManager.GetActiveScriptEditor();
            if (editor == null)
            {
                MessageBox.Show("Kein Skript geöffnet.");
                return;
            }

            if (editor.TextBox.CanUndo)
            {
                editor.TextBox.Undo();
            }
        }

        public void Redo()
        {
            var editor = _tabManager.GetActiveScriptEditor();
            if (editor == null)
            {
                MessageBox.Show("Kein Skript geöffnet.");
                return;
            }

            if (editor.TextBox.CanRedo)
            {
                editor.TextBox.Redo();
            }
        }

        public void Cut()
        {
            var editor = _tabManager.GetActiveScriptEditor();
            if (editor == null)
            {
                MessageBox.Show("Kein Skript geöffnet.");
                return;
            }

            if (editor.TextBox.SelectionLength > 0)
            {
                editor.TextBox.Cut();
            }
        }

        public void Copy()
        {
            var editor = _tabManager.GetActiveScriptEditor();
            if (editor == null)
            {
                MessageBox.Show("Kein Skript geöffnet.");
                return;
            }

            if (editor.TextBox.SelectionLength > 0)
            {
                editor.TextBox.Copy();
            }
        }

        public void Paste()
        {
            var editor = _tabManager.GetActiveScriptEditor();
            if (editor == null)
            {
                MessageBox.Show("Kein Skript geöffnet.");
                return;
            }

            editor.TextBox.Paste();
        }

        public void Duplicate()
        {
            var editor = _tabManager.GetActiveScriptEditor();
            var textBox = editor?.TextBox;

            if (textBox == null)
            {
                MessageBox.Show("Kein Skript geöffnet.");
                return;
            }

            if (!string.IsNullOrEmpty(textBox.SelectedText))
            {
                int caretIndex = textBox.SelectionStart + textBox.SelectionLength;
                textBox.Text = textBox.Text.Insert(caretIndex, textBox.SelectedText);
                textBox.SelectionStart = caretIndex;
            }
        }

        public void Delete()
        {
            var editor = _tabManager.GetActiveScriptEditor();
            var textBox = editor?.TextBox;

            if (textBox == null)
            {
                MessageBox.Show("Kein Skript geöffnet.");
                return;
            }

            if (!string.IsNullOrEmpty(textBox.SelectedText))
            {
                int start = textBox.SelectionStart;
                textBox.Text = textBox.Text.Remove(start, textBox.SelectionLength);
                textBox.SelectionStart = start;
            }
        }

        public void SelectAll()
        {
            var editor = _tabManager.GetActiveScriptEditor();
            if (editor == null)
            {
                MessageBox.Show("Kein Skript geöffnet.");
                return;
            }

            editor.TextBox.SelectAll();
        }

    }
}
