using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Skriptorium.Properties;
using Skriptorium.UI;

namespace Skriptorium.UI.Views
{
    public partial class SearchReplaceScriptDialog : Window
    {
        private const int MaxHistory = 10;
        private readonly ScriptEditor _scriptEditor;
        private List<string> _searchHistory = new();

        // Suchergebnisse und Navigation
        private List<int> _searchOffsets = new();
        private int _currentIndex = -1;

        public string SearchText => ComboSearchText.Text;
        public string ReplaceText => ComboReplaceText.Text;

        public event Action<string>? FindNextRequested;

        public SearchReplaceScriptDialog(ScriptEditor scriptEditor)
        {
            InitializeComponent();
            _scriptEditor = scriptEditor;
            _scriptEditor.Avalon.TextArea.SelectionChanged += Avalon_SelectionChanged; // Event-Handler für Textauswahl
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Such-History aus Benutzereinstellungen laden
            var savedHistory = Settings.Default.SearchHistory ?? "";
            _searchHistory = savedHistory
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Take(MaxHistory)
                .ToList();

            ComboSearchText.ItemsSource = _searchHistory;
            ComboReplaceText.ItemsSource = _searchHistory;

            var searchTextBox = GetComboBoxTextBox(ComboSearchText);
            if (searchTextBox != null)
                searchTextBox.TextChanged += ComboSearchText_TextChanged;

            var replaceTextBox = GetComboBoxTextBox(ComboReplaceText);
            if (replaceTextBox != null)
                replaceTextBox.TextChanged += ComboReplaceText_TextChanged;

            // Initiale Aktivierung/Deaktivierung von ChkSelectionOnly
            UpdateSelectionOnlyCheckbox();
        }

        private void Avalon_SelectionChanged(object sender, EventArgs e)
        {
            UpdateSelectionOnlyCheckbox();
        }

        private void UpdateSelectionOnlyCheckbox()
        {
            // Checkbox nur aktivieren, wenn Text markiert ist
            ChkSelectionOnly.IsEnabled = _scriptEditor.Avalon.SelectionLength > 0;
            if (_scriptEditor.Avalon.SelectionLength == 0)
            {
                ChkSelectionOnly.IsChecked = false; // Deaktivieren, wenn keine Auswahl
            }
        }

        private void ChkSelectionOnly_Checked(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(SearchText))
            {
                FindAllOccurrences(); // Suche aktualisieren
            }
        }

        private void ChkSelectionOnly_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(SearchText))
            {
                FindAllOccurrences(); // Suche aktualisieren
            }
        }

        private TextBox? GetComboBoxTextBox(ComboBox comboBox)
        {
            return comboBox.Template.FindName("PART_EditableTextBox", comboBox) as TextBox;
        }

        private void ComboSearchText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(SearchText))
            {
                _searchOffsets.Clear();
                _currentIndex = -1;
                _scriptEditor.ClearHighlighting();
            }
            else
            {
                FindAllOccurrences();
            }
        }

        private void ComboReplaceText_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Keine Aktion nötig beim Tippen im Ersetzen-Feld
        }

        private void FindAllOccurrences()
        {
            _searchOffsets.Clear();
            _currentIndex = -1;

            string searchText = SearchText;
            if (string.IsNullOrEmpty(searchText))
            {
                _scriptEditor.ClearHighlighting();
                return;
            }

            string text;
            int offsetBase = 0;

            // Prüfen, ob "Nur markierter Text" aktiviert ist und Text markiert ist
            bool restrictToSelection = ChkSelectionOnly.IsChecked == true && _scriptEditor.Avalon.SelectionLength > 0;
            if (restrictToSelection)
            {
                text = _scriptEditor.Avalon.SelectedText;
                offsetBase = _scriptEditor.Avalon.SelectionStart;
            }
            else
            {
                text = _scriptEditor.Text;
            }

            int offset = 0;
            var comparison = ChkCase.IsChecked == true ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            while ((offset = text.IndexOf(searchText, offset, comparison)) >= 0)
            {
                if (ChkWholeWord.IsChecked == true)
                {
                    bool leftOk = offset == 0 || !Char.IsLetterOrDigit(text[offset - 1]);
                    int afterIndex = offset + searchText.Length;
                    bool rightOk = afterIndex >= text.Length || !Char.IsLetterOrDigit(text[afterIndex]);
                    if (!(leftOk && rightOk))
                    {
                        offset++;
                        continue;
                    }
                }

                _searchOffsets.Add(offsetBase + offset);
                offset += searchText.Length;
            }

            // Übergabe der Auswahlparameter an HighlightAllOccurrences
            _scriptEditor.HighlightAllOccurrences(
                searchText,
                ChkCase.IsChecked == true,
                ChkWholeWord.IsChecked == true,
                restrictToSelection,
                _scriptEditor.Avalon.SelectionStart,
                _scriptEditor.Avalon.SelectionLength
            );
        }

        private void BtnFindNext_Click(object? sender, RoutedEventArgs? e)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                MessageBox.Show("Bitte einen Suchbegriff eingeben.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_searchOffsets.Count == 0)
            {
                FindAllOccurrences();
                if (_searchOffsets.Count == 0)
                {
                    MessageBox.Show("Keine Treffer gefunden.", "Suchen", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }

            _currentIndex++;
            if (_currentIndex >= _searchOffsets.Count)
                _currentIndex = 0;

            int offset = _searchOffsets[_currentIndex];
            int length = SearchText.Length;

            _scriptEditor.Avalon.Select(offset, length);
            _scriptEditor.Avalon.ScrollToLine(_scriptEditor.Avalon.Document.GetLineByOffset(offset).LineNumber);
            _scriptEditor.Avalon.Focus();

            // Event auslösen
            FindNextRequested?.Invoke(SearchText);
        }

        private void BtnReplace_Click(object sender, RoutedEventArgs e)
        {
            if (_searchOffsets.Count == 0)
            {
                FindAllOccurrences();
                if (_searchOffsets.Count == 0)
                {
                    MessageBox.Show("Keine Treffer gefunden, nichts zu ersetzen.", "Ersetzen", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }

            if (_currentIndex < 0)
                _currentIndex = 0;

            int offset = _searchOffsets[_currentIndex];
            int length = SearchText.Length;

            // Ursprüngliche Auswahl speichern, wenn "Nur markierter Text" aktiviert
            int originalSelectionStart = _scriptEditor.Avalon.SelectionStart;
            int originalSelectionLength = _scriptEditor.Avalon.SelectionLength;
            bool restrictToSelection = ChkSelectionOnly.IsChecked == true && originalSelectionLength > 0;

            // Prüfen, ob der Offset im markierten Bereich liegt, wenn "Nur markierter Text" aktiviert
            if (restrictToSelection)
            {
                int selectionStart = _scriptEditor.Avalon.SelectionStart;
                int selectionEnd = selectionStart + _scriptEditor.Avalon.SelectionLength;
                if (offset < selectionStart || offset + length > selectionEnd)
                {
                    MessageBox.Show("Der nächste Treffer liegt außerhalb der Auswahl.", "Ersetzen", MessageBoxButton.OK, MessageBoxImage.Information);
                    _currentIndex = -1;
                    _searchOffsets.Clear();
                    FindAllOccurrences();
                    return;
                }
            }

            var doc = _scriptEditor.Avalon.Document;

            // Undo-freundliche Ersetzung mit Gruppierung
            doc.BeginUpdate();
            doc.UndoStack.StartUndoGroup();

            doc.Replace(offset, length, ReplaceText);

            doc.UndoStack.EndUndoGroup();
            doc.EndUpdate();

            // Optional: Wenn du "geändert"-Status setzen willst
            // _scriptEditor.MarkAsModified();

            // Anpassen der Auswahl basierend auf der Längenänderung
            int lengthDifference = ReplaceText.Length - SearchText.Length;
            int newSelectionLength = restrictToSelection ? originalSelectionLength + lengthDifference : originalSelectionLength;

            if (restrictToSelection && newSelectionLength >= 0)
            {
                _scriptEditor.Avalon.Select(originalSelectionStart, newSelectionLength);
            }

            string text = doc.Text;
            int searchStart = offset + ReplaceText.Length;

            _searchOffsets.Clear();

            int offsetBase = 0;
            if (restrictToSelection)
            {
                text = _scriptEditor.Avalon.SelectedText;
                offsetBase = _scriptEditor.Avalon.SelectionStart;
                searchStart = Math.Max(0, searchStart - offsetBase);
            }

            var comparison = ChkCase.IsChecked == true ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            int pos = searchStart;

            while ((pos = text.IndexOf(SearchText, pos, comparison)) >= 0)
            {
                if (ChkWholeWord.IsChecked == true)
                {
                    bool leftOk = pos == 0 || !Char.IsLetterOrDigit(text[pos - 1]);
                    int afterIndex = pos + SearchText.Length;
                    bool rightOk = afterIndex >= text.Length || !Char.IsLetterOrDigit(text[afterIndex]);
                    if (!(leftOk && rightOk))
                    {
                        pos++;
                        continue;
                    }
                }
                _searchOffsets.Add(offsetBase + pos);
                pos += SearchText.Length;
            }

            if (_searchOffsets.Count == 0)
            {
                MessageBox.Show("Keine weiteren Treffer gefunden.", "Ersetzen", MessageBoxButton.OK, MessageBoxImage.Information);
                _currentIndex = -1;
                _scriptEditor.ClearHighlighting();
                if (restrictToSelection && newSelectionLength >= 0)
                {
                    _scriptEditor.Avalon.Select(originalSelectionStart, newSelectionLength);
                }
                return;
            }

            _currentIndex = 0;

            int nextOffset = _searchOffsets[_currentIndex];

            if (!restrictToSelection)
            {
                _scriptEditor.Avalon.Select(nextOffset, SearchText.Length);
            }
            else if (newSelectionLength >= 0)
            {
                _scriptEditor.Avalon.Select(originalSelectionStart, newSelectionLength);
            }

            _scriptEditor.Avalon.ScrollToLine(_scriptEditor.Avalon.Document.GetLineByOffset(nextOffset).LineNumber);
            _scriptEditor.Avalon.Focus();

            _scriptEditor.HighlightAllOccurrences(
                SearchText,
                ChkCase.IsChecked == true,
                ChkWholeWord.IsChecked == true,
                restrictToSelection,
                _scriptEditor.Avalon.SelectionStart,
                _scriptEditor.Avalon.SelectionLength
            );
        }

        private void BtnReplaceAll_Click(object sender, RoutedEventArgs e)
        {
            string searchText = SearchText;
            if (string.IsNullOrEmpty(searchText))
            {
                MessageBox.Show("Bitte geben Sie einen Suchtext ein.", "Ersetzen Alle", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var doc = _scriptEditor.Avalon.Document;
            string text;
            int offsetBase = 0;

            bool restrictToSelection = ChkSelectionOnly.IsChecked == true && _scriptEditor.Avalon.SelectionLength > 0;
            if (restrictToSelection)
            {
                text = _scriptEditor.Avalon.SelectedText;
                offsetBase = _scriptEditor.Avalon.SelectionStart;
            }
            else
            {
                text = doc.Text;
            }

            var comparison = ChkCase.IsChecked == true ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            doc.BeginUpdate();
            doc.UndoStack.StartUndoGroup();

            int replacedCount = 0;
            int index = 0;

            while ((index = text.IndexOf(searchText, index, comparison)) >= 0)
            {
                if (ChkWholeWord.IsChecked == true)
                {
                    bool leftOk = index == 0 || !Char.IsLetterOrDigit(text[index - 1]);
                    int afterIndex = index + searchText.Length;
                    bool rightOk = afterIndex >= text.Length || !Char.IsLetterOrDigit(text[afterIndex]);
                    if (!(leftOk && rightOk))
                    {
                        index++;
                        continue;
                    }
                }

                doc.Replace(offsetBase + index, searchText.Length, ReplaceText);
                text = restrictToSelection && _scriptEditor.Avalon.SelectionLength > 0
                    ? _scriptEditor.Avalon.SelectedText
                    : doc.Text;
                index += ReplaceText.Length;
                replacedCount++;
            }

            doc.UndoStack.EndUndoGroup();
            doc.EndUpdate();

            if (replacedCount == 0)
            {
                MessageBox.Show("Keine Treffer gefunden zum Ersetzen.", "Ersetzen Alle", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"{replacedCount} Treffer wurden ersetzt.", "Ersetzen Alle", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            FindAllOccurrences();
            _currentIndex = -1;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _scriptEditor.ClearHighlighting();
            _scriptEditor.Avalon.TextArea.SelectionChanged -= Avalon_SelectionChanged;

            // Suchhistorie aktualisieren und speichern
            if (!string.IsNullOrEmpty(SearchText) && !_searchHistory.Contains(SearchText))
            {
                _searchHistory.Insert(0, SearchText);
            }

            _searchHistory = _searchHistory.Distinct().Take(MaxHistory).ToList();
            Settings.Default.SearchHistory = string.Join(";", _searchHistory);

            Settings.Default.Save();
        }
    }
}