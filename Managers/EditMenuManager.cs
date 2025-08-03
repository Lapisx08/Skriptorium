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
                int selectionStart = Avalon.SelectionStart;
                int selectionLength = Avalon.SelectionLength;
                int caretIndex = selectionStart + selectionLength;

                // Extrahiere Text direkt aus dem Document
                var document = Avalon.Document;
                if (selectionStart < 0 || selectionLength < 0 || caretIndex > document.TextLength)
                {
                    MessageBox.Show($"Ungültige Auswahl: SelectionStart={selectionStart}, SelectionLength={selectionLength}, caretIndex={caretIndex}, Textlänge={document.TextLength}");
                    return;
                }

                string selectedText = document.GetText(selectionStart, selectionLength);

                try
                {
                    document.Insert(caretIndex, selectedText);
                    Avalon.SelectionStart = caretIndex;
                    Avalon.SelectionLength = 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Duplizieren: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Kein Text ausgewählt.");
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