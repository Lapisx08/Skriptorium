using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
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

        public SearchReplaceScriptDialog(ScriptEditor scriptEditor)
        {
            InitializeComponent();
            _scriptEditor = scriptEditor;
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

            ComboSearchIn.SelectedIndex = 0;

            var savedPath = Settings.Default.ScriptSearchPath;
            if (!string.IsNullOrWhiteSpace(savedPath))
            {
                TxtPath.Text = savedPath;
            }

            ChkSearchIn.IsChecked = false;
            ComboSearchIn.SelectedIndex = 0;
            UpdatePathControls();

            var searchTextBox = GetComboBoxTextBox(ComboSearchText);
            if (searchTextBox != null)
                searchTextBox.TextChanged += ComboSearchText_TextChanged;

            var replaceTextBox = GetComboBoxTextBox(ComboReplaceText);
            if (replaceTextBox != null)
                replaceTextBox.TextChanged += ComboReplaceText_TextChanged;
        }

        private TextBox? GetComboBoxTextBox(ComboBox comboBox)
        {
            return comboBox.Template.FindName("PART_EditableTextBox", comboBox) as TextBox;
        }

        private void ChkSearchIn_Checked(object sender, RoutedEventArgs e)
        {
            ComboSearchIn.IsEnabled = true;
            UpdatePathControls();
        }

        private void ChkSearchIn_Unchecked(object sender, RoutedEventArgs e)
        {
            ComboSearchIn.IsEnabled = false;
            TxtPath.Visibility = BtnBrowse.Visibility = Visibility.Collapsed;
        }

        private void ComboSearchIn_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePathControls();
        }

        private void UpdatePathControls()
        {
            bool pathMode = ChkSearchIn.IsChecked == true
                && (ComboSearchIn.SelectedItem as ComboBoxItem)?.Content.ToString() == "Skripte in Pfad";

            TxtPath.Visibility = BtnBrowse.Visibility = pathMode
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            using var dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Ordner auswählen"
            };
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                TxtPath.Text = dlg.FileName;
            }
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

            string text = _scriptEditor.Text;

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

                _searchOffsets.Add(offset);
                offset += searchText.Length;
            }

            // Übergabe von matchCase und wholeWord an HighlightAllOccurrences
            _scriptEditor.HighlightAllOccurrences(searchText, ChkCase.IsChecked == true, ChkWholeWord.IsChecked == true);
        }

        private void BtnFindNext_Click(object? sender, RoutedEventArgs? e)
        {
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

            var doc = _scriptEditor.Avalon.Document;
            doc.Replace(offset, length, ReplaceText);
            _scriptEditor.SetTextAndMarkAsModified(doc.Text);

            string text = doc.Text;
            int searchStart = offset + ReplaceText.Length;

            _searchOffsets.Clear();

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
                _searchOffsets.Add(pos);
                pos += SearchText.Length;
            }

            if (_searchOffsets.Count == 0)
            {
                MessageBox.Show("Keine weiteren Treffer gefunden.", "Ersetzen", MessageBoxButton.OK, MessageBoxImage.Information);
                _currentIndex = -1;
                _scriptEditor.ClearHighlighting();
                return;
            }

            _currentIndex = 0;

            int nextOffset = _searchOffsets[_currentIndex];
            _scriptEditor.Avalon.Select(nextOffset, SearchText.Length);
            _scriptEditor.Avalon.ScrollToLine(_scriptEditor.Avalon.Document.GetLineByOffset(nextOffset).LineNumber);
            _scriptEditor.Avalon.Focus();

            // Übergabe von matchCase und wholeWord an HighlightAllOccurrences
            _scriptEditor.HighlightAllOccurrences(SearchText, ChkCase.IsChecked == true, ChkWholeWord.IsChecked == true);
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
            string text = doc.Text;

            var comparison = ChkCase.IsChecked == true ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

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

                doc.Replace(index, searchText.Length, ReplaceText);
                text = doc.Text;
                index += ReplaceText.Length;
                replacedCount++;
            }

            if (replacedCount == 0)
            {
                MessageBox.Show("Keine Treffer gefunden zum Ersetzen.", "Ersetzen Alle", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                _scriptEditor.SetTextAndMarkAsModified(doc.Text);
                MessageBox.Show($"{replacedCount} Treffer wurden ersetzt.", "Ersetzen Alle", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            // Übergabe von matchCase und wholeWord an HighlightAllOccurrences
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

            // Suchhistorie aktualisieren und speichern
            if (!string.IsNullOrWhiteSpace(SearchText) && !_searchHistory.Contains(SearchText))
            {
                _searchHistory.Insert(0, SearchText);
            }

            _searchHistory = _searchHistory.Distinct().Take(MaxHistory).ToList();
            Settings.Default.SearchHistory = string.Join(";", _searchHistory);

            // Suchpfad speichern
            if (ChkSearchIn.IsChecked == true && TxtPath.Visibility == Visibility.Visible)
            {
                Settings.Default.ScriptSearchPath = TxtPath.Text;
            }

            Settings.Default.Save();
        }
    }
}