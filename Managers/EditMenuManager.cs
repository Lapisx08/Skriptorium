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

            if (editor.Avalon.CanUndo)
            {
                editor.Avalon.Undo();
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

            if (editor.Avalon.CanRedo)
            {
                editor.Avalon.Redo();
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

            if (editor.Avalon.SelectionLength > 0)
            {
                editor.Avalon.Cut();
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

            if (editor.Avalon.SelectionLength > 0)
            {
                editor.Avalon.Copy();
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

            editor.Avalon.Paste();
        }

        public void Duplicate()
        {
            var editor = _tabManager.GetActiveScriptEditor();
            var Avalon = editor?.Avalon;

            if (Avalon == null)
            {
                MessageBox.Show("Kein Skript geöffnet.");
                return;
            }

            if (!string.IsNullOrEmpty(Avalon.SelectedText))
            {
                int caretIndex = Avalon.SelectionStart + Avalon.SelectionLength;
                Avalon.Text = Avalon.Text.Insert(caretIndex, Avalon.SelectedText);
                Avalon.SelectionStart = caretIndex;
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

            editor.Avalon.SelectAll();
        }
    }
}