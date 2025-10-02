using AvalonDock;
using AvalonDock.Layout;
using MahApps.Metro.Controls;
using Skriptorium.Managers;
using Skriptorium.Properties;
using Skriptorium.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Skriptorium.UI.Views
{
    public partial class SearchReplaceScriptView : MetroWindow
    {
        private const int MaxHistory = 10;
        private readonly ScriptEditor _scriptEditor;
        private readonly ScriptTabManager _tabManager;
        private readonly DockingManager _dockingManager;
        private List<string> _searchHistory = new();
        private List<int> _searchOffsets = new();
        private int _currentIndex = -1;
        private ObservableCollection<SearchResultNode> _searchResults = new ObservableCollection<SearchResultNode>();

        public string SearchText => ComboSearchText.Text;
        public string ReplaceText => ComboReplaceText.Text;
        public string SearchIn => ComboSearchIn.SelectedItem is ComboBoxItem item ? item.Content.ToString() : null;

        public event Action<string>? FindNextRequested;

        public SearchReplaceScriptView(ScriptEditor scriptEditor, ScriptTabManager tabManager, DockingManager dockingManager)
        {
            InitializeComponent();
            _scriptEditor = scriptEditor;
            _tabManager = tabManager;
            _dockingManager = dockingManager;
            _scriptEditor.Avalon.TextArea.SelectionChanged += Avalon_SelectionChanged;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var savedHistory = Settings.Default.SearchHistory ?? "";
            _searchHistory = savedHistory
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Take(MaxHistory)
                .ToList();

            ComboSearchText.ItemsSource = _searchHistory;
            ComboReplaceText.ItemsSource = _searchHistory;

            var searchTextBox = GetComboBoxTextBox(ComboSearchText);
            if (searchTextBox is not null)
                searchTextBox.TextChanged += ComboSearchText_TextChanged;

            var replaceTextBox = GetComboBoxTextBox(ComboReplaceText);
            if (replaceTextBox is not null)
                replaceTextBox.TextChanged += ComboReplaceText_TextChanged;

            UpdateSelectionOnlyCheckbox();
            LoadSearchInSetting();
            UpdateReplaceControlsState();
        }

        private void LoadSearchInSetting()
        {
            if (ComboSearchIn.Items.Count > 0)
            {
                ComboSearchIn.SelectedIndex = 0;
            }

            ChkSearchIn.IsChecked = false;
            ComboSearchIn.IsEnabled = false;
        }

        private void UpdateReplaceControlsState()
        {
            bool isSearchInChecked = ChkSearchIn.IsChecked == true;
            BtnReplaceNext.IsEnabled = !isSearchInChecked;
            BtnReplaceAll.IsEnabled = true;
        }

        private void UpdateSearchResultsVisibility()
        {
            // Wird in BtnFindNext_Click gesteuert
        }

        private void ChkSearchIn_Checked(object sender, RoutedEventArgs e)
        {
            ComboSearchIn.IsEnabled = true;
            UpdateReplaceControlsState();
            UpdateSearchResultsVisibility();
            if (!string.IsNullOrEmpty(SearchText))
            {
                FindAllOccurrences();
            }
        }

        private void ChkSearchIn_Unchecked(object sender, RoutedEventArgs e)
        {
            ComboSearchIn.IsEnabled = false;
            UpdateReplaceControlsState();
            UpdateSearchResultsVisibility();
            if (!string.IsNullOrEmpty(SearchText))
            {
                FindAllOccurrences();
            }
        }

        private void ComboSearchIn_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSearchResultsVisibility();
            if (!string.IsNullOrEmpty(SearchText))
            {
                FindAllOccurrences();
            }
        }

        private void Avalon_SelectionChanged(object sender, EventArgs e)
        {
            UpdateSelectionOnlyCheckbox();
        }

        private void UpdateSelectionOnlyCheckbox()
        {
            ChkSelectionOnly.IsEnabled = _scriptEditor.Avalon.SelectionLength > 0;
            if (_scriptEditor.Avalon.SelectionLength == 0)
            {
                ChkSelectionOnly.IsChecked = false;
            }
        }

        private void ChkSelectionOnly_Checked(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(SearchText))
            {
                FindAllOccurrences();
            }
        }

        private void ChkSelectionOnly_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(SearchText))
            {
                FindAllOccurrences();
            }
        }

        private System.Windows.Controls.TextBox? GetComboBoxTextBox(System.Windows.Controls.ComboBox comboBox)
        {
            return comboBox.Template.FindName("PART_EditableTextBox", comboBox) as System.Windows.Controls.TextBox;
        }

        private void ComboSearchText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(SearchText))
            {
                _searchOffsets.Clear();
                _currentIndex = -1;
                _searchResults.Clear();
                _scriptEditor.ClearHighlighting();
                HideSearchResultsPanel();
            }
            else
            {
                FindAllOccurrences();
            }
        }

        private void ComboReplaceText_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Keine Aktion nötig
        }

        private void FindAllOccurrences()
        {
            _searchOffsets.Clear();
            _currentIndex = -1;
            _searchResults.Clear();

            string searchText = SearchText;
            if (string.IsNullOrEmpty(searchText))
            {
                _scriptEditor.ClearHighlighting();
                System.Diagnostics.Debug.WriteLine("Kein Suchtext eingegeben.");
                return;
            }

            bool isSearchInAllScripts = ChkSearchIn.IsChecked == true && SearchIn == "In allen offenen Skripten";
            bool isSearchInDirectory = ChkSearchIn.IsChecked == true && SearchIn == "Im gesetzten Verzeichnis";
            bool restrictToSelection = ChkSelectionOnly.IsChecked == true && _scriptEditor.Avalon.SelectionLength > 0;

            if (isSearchInAllScripts)
            {
                var editors = _tabManager.GetAllOpenEditors();
                if (!editors.Any())
                {
                    System.Diagnostics.Debug.WriteLine("Keine offenen Editoren gefunden.");
                    return;
                }

                foreach (var editor in editors)
                {
                    string filePath = editor.FilePath ?? "Unbenanntes Skript";
                    var node = new SearchResultNode(filePath, editor.Text);
                    string text = editor.Avalon.Document.Text;
                    if (string.IsNullOrEmpty(text))
                    {
                        System.Diagnostics.Debug.WriteLine($"Kein Text im Editor für {filePath}");
                        continue;
                    }

                    FindMatchesInScript(text, searchText, node);
                    if (node.Matches.Count > 0)
                    {
                        _searchResults.Add(node);
                        System.Diagnostics.Debug.WriteLine($"Treffer gefunden in {filePath}: {node.Matches.Count} Matches");
                    }
                }
            }
            else if (isSearchInDirectory)
            {
                string scriptPath = Properties.Settings.Default.ScriptSearchPath;
                if (string.IsNullOrEmpty(scriptPath) || !Directory.Exists(scriptPath))
                {
                    MessageBox.Show("Kein gültiger Skript-Ordner festgelegt. Bitte wählen Sie einen Ordner in den Einstellungen.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                    System.Diagnostics.Debug.WriteLine("Ungültiger oder nicht existierender Skript-Ordner.");
                    return;
                }

                try
                {
                    var files = Directory.EnumerateFiles(scriptPath, "*.*", SearchOption.AllDirectories)
                        .Where(file => file.EndsWith(".d", StringComparison.OrdinalIgnoreCase) ||
                                      file.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));

                    foreach (var file in files)
                    {
                        try
                        {
                            string text = File.ReadAllText(file);
                            var node = new SearchResultNode(file, text);
                            FindMatchesInScript(text, searchText, node);
                            if (node.Matches.Count > 0)
                            {
                                _searchResults.Add(node);
                                System.Diagnostics.Debug.WriteLine($"Treffer gefunden in {file}: {node.Matches.Count} Matches");
                            }
                        }
                        catch (IOException ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Fehler beim Lesen der Datei {file}: {ex.Message}");
                        }
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    MessageBox.Show($"Zugriff auf den Ordner verweigert: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    System.Diagnostics.Debug.WriteLine($"Zugriffsfehler: {ex.Message}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Durchsuchen des Ordners: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    System.Diagnostics.Debug.WriteLine($"Allgemeiner Fehler: {ex.Message}");
                }
            }
            else
            {
                string text;
                int offsetBase = 0;

                if (restrictToSelection)
                {
                    text = _scriptEditor.Avalon.SelectedText;
                    offsetBase = _scriptEditor.Avalon.SelectionStart;
                }
                else
                {
                    text = _scriptEditor.Avalon.Document.Text;
                }

                var node = new SearchResultNode(_scriptEditor.FilePath ?? "Aktives Skript");
                FindMatchesInScript(text, searchText, node, offsetBase);
                if (node.Matches.Count > 0)
                {
                    _searchResults.Add(node);
                }

                int offset = 0;
                var comparison = ChkCase.IsChecked == true ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

                while ((offset = text.IndexOf(searchText, offset, comparison)) >= 0)
                {
                    if (ChkWholeWord.IsChecked == true)
                    {
                        bool leftOk = offset == 0 || !char.IsLetterOrDigit(text[offset - 1]);
                        int afterIndex = offset + searchText.Length;
                        bool rightOk = afterIndex >= text.Length || !char.IsLetterOrDigit(text[afterIndex]);
                        if (!(leftOk && rightOk))
                        {
                            offset++;
                            continue;
                        }
                    }

                    _searchOffsets.Add(offsetBase + offset);
                    offset += searchText.Length;
                }

                _scriptEditor.HighlightAllOccurrences(
                    searchText,
                    ChkCase.IsChecked == true,
                    ChkWholeWord.IsChecked == true,
                    restrictToSelection,
                    _scriptEditor.Avalon.SelectionStart,
                    _scriptEditor.Avalon.SelectionLength
                );
            }

            if (_searchResults.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("Keine Suchergebnisse gefunden.");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Insgesamt {_searchResults.Sum(n => n.Matches.Count)} Treffer in {_searchResults.Count} Dateien.");
            }
        }

        private void FindMatchesInScript(string text, string searchText, SearchResultNode node, int offsetBase = 0)
        {
            int offset = 0;
            var comparison = ChkCase.IsChecked == true ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            while ((offset = text.IndexOf(searchText, offset, comparison)) >= 0)
            {
                if (ChkWholeWord.IsChecked == true)
                {
                    bool leftOk = offset == 0 || !char.IsLetterOrDigit(text[offset - 1]);
                    int afterIndex = offset + searchText.Length;
                    bool rightOk = afterIndex >= text.Length || !char.IsLetterOrDigit(text[afterIndex]);
                    if (!(leftOk && rightOk))
                    {
                        offset++;
                        continue;
                    }
                }

                int lineNumber = GetLineNumber(text, offset);
                string lineText = lineNumber < lines.Length ? lines[lineNumber].Trim() : "";
                string displayText = $"Zeile {lineNumber + 1}: {(lineText.Length > 50 ? lineText.Substring(0, 50) + "..." : lineText)}";
                node.Matches.Add(new SearchMatch(offsetBase + offset, searchText.Length, displayText, node));
                offset += searchText.Length;
            }
        }

        private int GetLineNumber(string text, int offset)
        {
            int lineCount = 0;
            for (int i = 0; i < offset && i < text.Length; i++)
            {
                if (text[i] == '\n')
                    lineCount++;
            }
            return lineCount;
        }

        private void BtnFindNext_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                MessageBox.Show("Bitte einen Suchbegriff eingeben.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_searchOffsets.Count == 0 && _searchResults.Count == 0)
            {
                FindAllOccurrences();
                if (_searchOffsets.Count == 0 && _searchResults.Count == 0)
                {
                    MessageBox.Show("Keine Treffer gefunden.", "Suchen", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }

            if (ChkSearchIn.IsChecked == true && (SearchIn == "In allen offenen Skripten" || SearchIn == "Im gesetzten Verzeichnis"))
            {
                ShowSearchResultsPanel();
                if (_searchResults.Count > 0)
                {
                    var firstNode = _searchResults.FirstOrDefault();
                    if (firstNode != null && firstNode.Matches.Count > 0)
                    {
                        var firstMatch = firstNode.Matches[0];
                        var searchResultsView = GetSearchResultsView();
                        if (searchResultsView != null)
                        {
                            var container = searchResultsView.SearchResultsTree.ItemContainerGenerator.ContainerFromItem(firstNode) as TreeViewItem;
                            if (container != null)
                            {
                                container.IsExpanded = true;
                                var matchContainer = container.ItemContainerGenerator.ContainerFromItem(firstMatch) as TreeViewItem;
                                if (matchContainer != null)
                                {
                                    matchContainer.IsSelected = true;
                                    matchContainer.BringIntoView();
                                }
                            }
                            searchResultsView.SearchResultsTree.Focus();
                        }
                    }
                }
            }
            else
            {
                _currentIndex++;
                if (_currentIndex >= _searchOffsets.Count)
                    _currentIndex = 0;

                int offset = _searchOffsets[_currentIndex];
                int length = SearchText.Length;

                _scriptEditor.Avalon.Select(offset, length);
                _scriptEditor.Avalon.ScrollToLine(_scriptEditor.Avalon.Document.GetLineByOffset(offset).LineNumber);
                _scriptEditor.Avalon.Focus();

                FindNextRequested?.Invoke(SearchText);
            }
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

            int originalSelectionStart = _scriptEditor.Avalon.SelectionStart;
            int originalSelectionLength = _scriptEditor.Avalon.SelectionLength;
            bool restrictToSelection = ChkSelectionOnly.IsChecked == true && originalSelectionLength > 0;

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

            doc.BeginUpdate();
            doc.UndoStack.StartUndoGroup();

            doc.Replace(offset, length, ReplaceText);

            doc.UndoStack.EndUndoGroup();
            doc.EndUpdate();

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
                    bool leftOk = pos == 0 || !char.IsLetterOrDigit(text[pos - 1]);
                    int afterIndex = pos + SearchText.Length;
                    bool rightOk = afterIndex >= text.Length || !char.IsLetterOrDigit(text[afterIndex]);
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

            FindAllOccurrences();

            bool isSearchInAllScripts = ChkSearchIn.IsChecked == true && SearchIn == "In allen offenen Skripten";
            bool isSearchInDirectory = ChkSearchIn.IsChecked == true && SearchIn == "Im gesetzten Verzeichnis";
            bool restrictToSelection = ChkSelectionOnly.IsChecked == true && _scriptEditor.Avalon.SelectionLength > 0;

            int totalReplacedCount = 0;

            if (isSearchInAllScripts)
            {
                var editors = _tabManager.GetAllOpenEditors();
                if (!editors.Any())
                {
                    MessageBox.Show("Keine offenen Skripte zum Ersetzen.", "Ersetzen Alle", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                foreach (var editor in editors)
                {
                    var doc = editor.Avalon.Document;
                    string text = doc.Text;
                    if (string.IsNullOrEmpty(text))
                    {
                        System.Diagnostics.Debug.WriteLine($"Kein Text im Editor für {editor.FilePath ?? "Unbenanntes Skript"}");
                        continue;
                    }

                    var comparison = ChkCase.IsChecked == true ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                    int offset = 0;
                    int replacedCount = 0;

                    doc.BeginUpdate();
                    doc.UndoStack.StartUndoGroup();

                    while ((offset = text.IndexOf(searchText, offset, comparison)) >= 0)
                    {
                        if (ChkWholeWord.IsChecked == true)
                        {
                            bool leftOk = offset == 0 || !char.IsLetterOrDigit(text[offset - 1]);
                            int afterIndex = offset + searchText.Length;
                            bool rightOk = afterIndex >= text.Length || !char.IsLetterOrDigit(text[afterIndex]);
                            if (!(leftOk && rightOk))
                            {
                                offset++;
                                continue;
                            }
                        }

                        doc.Replace(offset, searchText.Length, ReplaceText);
                        text = doc.Text; // Text nach jeder Ersetzung aktualisieren
                        offset += ReplaceText.Length;
                        replacedCount++;
                    }

                    doc.UndoStack.EndUndoGroup();
                    doc.EndUpdate();

                    if (replacedCount > 0)
                    {
                        totalReplacedCount += replacedCount;
                        System.Diagnostics.Debug.WriteLine($"Ersetzt {replacedCount} Treffer in {editor.FilePath ?? "Unbenanntes Skript"}");
                    }
                }
            }
            else if (isSearchInDirectory)
            {
                if (_searchResults.Count == 0)
                {
                    MessageBox.Show("Keine Treffer gefunden zum Ersetzen.", "Ersetzen Alle", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string scriptPath = Properties.Settings.Default.ScriptSearchPath;
                if (string.IsNullOrEmpty(scriptPath) || !Directory.Exists(scriptPath))
                {
                    MessageBox.Show("Kein gültiger Skript-Ordner festgelegt.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                foreach (var node in _searchResults.ToList())
                {
                    string filePath = node.FullPath;
                    try
                    {
                        string text = File.ReadAllText(filePath);
                        string newText = text;
                        int offset = 0;
                        int replacedCount = 0;
                        var comparison = ChkCase.IsChecked == true ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

                        while ((offset = newText.IndexOf(searchText, offset, comparison)) >= 0)
                        {
                            if (ChkWholeWord.IsChecked == true)
                            {
                                bool leftOk = offset == 0 || !char.IsLetterOrDigit(newText[offset - 1]);
                                int afterIndex = offset + searchText.Length;
                                bool rightOk = afterIndex >= newText.Length || !char.IsLetterOrDigit(newText[afterIndex]);
                                if (!(leftOk && rightOk))
                                {
                                    offset++;
                                    continue;
                                }
                            }

                            newText = newText.Remove(offset, searchText.Length).Insert(offset, ReplaceText);
                            offset += ReplaceText.Length;
                            replacedCount++;
                        }

                        if (replacedCount > 0)
                        {
                            File.WriteAllText(filePath, newText);
                            totalReplacedCount += replacedCount;
                            System.Diagnostics.Debug.WriteLine($"Ersetzt {replacedCount} Treffer in {filePath}");
                        }
                    }
                    catch (IOException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Fehler beim Bearbeiten der Datei {filePath}: {ex.Message}");
                        MessageBox.Show($"Fehler beim Bearbeiten der Datei {filePath}: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Zugriffsfehler bei {filePath}: {ex.Message}");
                        MessageBox.Show($"Zugriff verweigert für {filePath}: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                var doc = _scriptEditor.Avalon.Document;
                string text;
                int offsetBase = 0;

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
                        bool leftOk = index == 0 || !char.IsLetterOrDigit(text[index - 1]);
                        int afterIndex = index + searchText.Length;
                        bool rightOk = afterIndex >= text.Length || !char.IsLetterOrDigit(text[afterIndex]);
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
                    totalReplacedCount += replacedCount;
                    System.Diagnostics.Debug.WriteLine($"Ersetzt {replacedCount} Treffer im aktiven Skript");
                }
            }

            if (totalReplacedCount == 0)
            {
                MessageBox.Show("Keine Treffer gefunden zum Ersetzen.", "Ersetzen Alle", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"{totalReplacedCount} Treffer wurden ersetzt.", "Ersetzen Alle", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            FindAllOccurrences();
            if (isSearchInAllScripts || isSearchInDirectory)
            {
                ShowSearchResultsPanel();
            }
            else
            {
                _currentIndex = -1;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _scriptEditor.ClearHighlighting();
            _scriptEditor.Avalon.TextArea.SelectionChanged -= Avalon_SelectionChanged;

            if (!string.IsNullOrEmpty(SearchText) && !_searchHistory.Contains(SearchText))
            {
                _searchHistory.Insert(0, SearchText);
            }

            _searchHistory = _searchHistory.Distinct().Take(MaxHistory).ToList();
            Settings.Default.SearchHistory = string.Join(";", _searchHistory);

            Settings.Default.Save();
        }

        private void ShowSearchResultsPanel()
        {
            var existing = _dockingManager.Layout.Descendents()
                .OfType<LayoutAnchorable>()
                .FirstOrDefault(a => a.ContentId == "SearchResults");

            if (existing != null)
            {
                existing.IsVisible = true;
                existing.IsActive = true;
                return;
            }

            var searchResultsView = new SearchResultsView(_tabManager, _searchResults);

            var anchorable = new LayoutAnchorable
            {
                Title = "Suchergebnisse",
                Content = searchResultsView,
                CanClose = true,
                CanFloat = true,
                CanHide = false,
                ContentId = "SearchResults"
            };

            anchorable.AddToLayout(_dockingManager, AnchorableShowStrategy.Left);

            var pane = anchorable.FindParent<LayoutAnchorablePane>();
            if (pane != null)
            {
                pane.DockWidth = new GridLength(250, GridUnitType.Pixel);
            }

            anchorable.IsVisible = true;
            anchorable.IsActive = true;
        }

        private void HideSearchResultsPanel()
        {
            var anchorable = _dockingManager.Layout.Descendents()
                .OfType<LayoutAnchorable>()
                .FirstOrDefault(a => a.ContentId == "SearchResults");

            if (anchorable != null)
            {
                anchorable.IsVisible = false;
            }
        }

        private SearchResultsView? GetSearchResultsView()
        {
            var anchorable = _dockingManager.Layout.Descendents()
                .OfType<LayoutAnchorable>()
                .FirstOrDefault(a => a.ContentId == "SearchResults");

            return anchorable?.Content as SearchResultsView;
        }
    }
}