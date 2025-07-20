using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Skriptorium.UI;

namespace Skriptorium.Managers
{
    public class SearchManager
    {
        private readonly ScriptTabManager _tabManager;

        public SearchManager(ScriptTabManager tabManager)
        {
            _tabManager = tabManager;
        }

        // Editorsuche (aktuelles geöffnetes Skript)
        public void FindNext(string searchText, bool matchCase = false)
        {
            var editor = _tabManager.GetActiveScriptEditor();
            var textBox = editor?.TextBox;

            if (textBox == null || string.IsNullOrEmpty(searchText))
            {
                MessageBox.Show("Kein Skript geöffnet oder kein Suchtext eingegeben.");
                return;
            }

            var content = textBox.Text;
            var currentIndex = textBox.SelectionStart + textBox.SelectionLength;
            var comparison = matchCase ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;

            int foundIndex = content.IndexOf(searchText, currentIndex, comparison);

            if (foundIndex >= 0)
            {
                textBox.Select(foundIndex, searchText.Length);
                textBox.Focus();
            }
            else
            {
                MessageBox.Show("Text nicht gefunden.", "Suchen", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void Replace(string searchText, string replaceText, bool matchCase = false)
        {
            var editor = _tabManager.GetActiveScriptEditor();
            var textBox = editor?.TextBox;

            if (textBox == null || string.IsNullOrEmpty(searchText))
            {
                MessageBox.Show("Kein Skript geöffnet oder kein Suchtext eingegeben.");
                return;
            }

            var comparison = matchCase ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;

            if (string.Equals(textBox.SelectedText, searchText, comparison))
            {
                textBox.SelectedText = replaceText; // Löst TextChanged aus → Stern erscheint
            }

            FindNext(searchText, matchCase);
        }

        public void ReplaceAll(string searchText, string replaceText, bool matchCase = false)
        {
            var editor = _tabManager.GetActiveScriptEditor();

            if (editor == null || string.IsNullOrEmpty(searchText))
            {
                MessageBox.Show("Kein Skript geöffnet oder kein Suchtext eingegeben.");
                return;
            }

            var comparison = matchCase ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
            var content = editor.Text;
            int count = 0;
            int index = 0;

            while (true)
            {
                index = content.IndexOf(searchText, index, comparison);
                if (index == -1) break;

                content = content.Remove(index, searchText.Length)
                                 .Insert(index, replaceText);
                index += replaceText.Length;
                count++;
            }

            if (count > 0)
            {
                editor.SetTextAndMarkAsModified(content); // Löst Änderungserkennung aus
                MessageBox.Show($"{count} Ersetzungen vorgenommen.", "Ersetzen", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Keine Vorkommen gefunden.", "Ersetzen", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Dateisuche (mehrere Dateien im Ordner)
        public List<(string FilePath, int Count)> FindInFiles(string directoryPath, string searchText, string fileExtensionFilter = "*.txt", bool matchCase = false)
        {
            var result = new List<(string, int)>();

            if (string.IsNullOrEmpty(searchText) || !Directory.Exists(directoryPath))
            {
                MessageBox.Show("Ungültiges Verzeichnis oder Suchtext fehlt.");
                return result;
            }

            var comparison = matchCase ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;

            foreach (var file in Directory.GetFiles(directoryPath, fileExtensionFilter, SearchOption.AllDirectories))
            {
                string content = File.ReadAllText(file);
                int count = 0;
                int index = 0;

                while (true)
                {
                    index = content.IndexOf(searchText, index, comparison);
                    if (index == -1) break;
                    index += searchText.Length;
                    count++;
                }

                if (count > 0)
                    result.Add((file, count));
            }

            return result;
        }

        public int ReplaceInFiles(string directoryPath, string searchText, string replaceText, string fileExtensionFilter = "*.txt", bool matchCase = false)
        {
            if (string.IsNullOrEmpty(searchText) || !Directory.Exists(directoryPath))
            {
                MessageBox.Show("Ungültiges Verzeichnis oder Suchtext fehlt.");
                return 0;
            }

            var comparison = matchCase ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
            int totalReplacements = 0;

            foreach (var file in Directory.GetFiles(directoryPath, fileExtensionFilter, SearchOption.AllDirectories))
            {
                string content = File.ReadAllText(file);
                int count = 0;
                int index = 0;

                while (true)
                {
                    index = content.IndexOf(searchText, index, comparison);
                    if (index == -1) break;

                    content = content.Remove(index, searchText.Length)
                                     .Insert(index, replaceText);
                    index += replaceText.Length;
                    count++;
                }

                if (count > 0)
                {
                    File.WriteAllText(file, content);
                    totalReplacements += count;
                }
            }

            MessageBox.Show($"{totalReplacements} Ersetzungen in Dateien vorgenommen.", "Dateien ersetzen", MessageBoxButton.OK, MessageBoxImage.Information);
            return totalReplacements;
        }
    }
}
