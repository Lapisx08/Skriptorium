using ICSharpCode.AvalonEdit;
using Skriptorium.Common;
using Skriptorium.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

namespace Skriptorium.UI.Views
{
    public partial class SearchInlinePanel : UserControl
    {
        private int _currentMatchIndex = -1;
        private List<int> _searchOffsets = new List<int>();
        private ScriptEditor? _editor;

        private int _currentMatch = 0;
        private int _totalMatches = 0;

        private readonly System.Timers.Timer _searchDebounceTimer;

        public event EventHandler? SearchTextChanged;
        public event EventHandler? VisibilityChanged;

        private string NoResultsText =>
            Application.Current.TryFindResource("NoResults") as string
            ?? string.Empty;

        public SearchInlinePanel()
        {
            InitializeComponent();

            Keyboard.AddPreviewKeyDownHandler(this, SearchInlinePanel_PreviewKeyDown);

            _searchDebounceTimer = new System.Timers.Timer(300) { AutoReset = false };
            _searchDebounceTimer.Elapsed += (s, e) =>
                Dispatcher.InvokeAsync(() => StartSearch(TxtSearch.Text));

            TxtSearch.TextChanged += TxtSearch_TextChanged;
            TxtSearch.KeyDown += TxtSearch_KeyDown;

            // ToggleButtons und Textfelder aus GlobalState initialisieren
            BtnMatchCase.IsChecked = GlobalSearchReplaceState.MatchCase;
            BtnWholeWord.IsChecked = GlobalSearchReplaceState.WholeWord;
            TxtSearch.Text = GlobalSearchReplaceState.LastSearchText;
            TxtReplace.Text = GlobalSearchReplaceState.LastReplaceText;
            ReplacePanel.Visibility = GlobalSearchReplaceState.IsReplacePanelOpen
                ? Visibility.Visible : Visibility.Collapsed;
        }

        #region Public API

        public void BindEditor(ScriptEditor? editor)
        {
            if (_editor != null)
            {
                _editor.Avalon.TextChanged -= Editor_TextChanged;
                _editor.Avalon.TextArea.SelectionChanged -= Editor_SelectionChanged;
                ClearHighlighting();
            }

            _editor = editor;

            if (_editor != null)
            {
                _editor.Avalon.TextChanged += Editor_TextChanged;
                _editor.Avalon.TextArea.SelectionChanged += Editor_SelectionChanged;

                // ToggleButtons auf globalen Zustand setzen
                BtnMatchCase.IsChecked = GlobalSearchReplaceState.MatchCase;
                BtnWholeWord.IsChecked = GlobalSearchReplaceState.WholeWord;

                if (Visibility == Visibility.Visible && !string.IsNullOrEmpty(TxtSearch.Text))
                    StartSearch(TxtSearch.Text);
            }
        }

        public string SearchText => TxtSearch.Text;

        public void SetSearchText(string text)
        {
            if (TxtSearch.Text != text)
                TxtSearch.Text = text;
        }

        public new Visibility Visibility
        {
            get => base.Visibility;
            set
            {
                if (base.Visibility != value)
                {
                    base.Visibility = value;
                    VisibilityChanged?.Invoke(this, EventArgs.Empty);
                    GlobalSearchReplaceState.IsSearchPanelOpen = value == Visibility.Visible;
                }
            }
        }

        #endregion

        #region Editor Events

        private void Editor_TextChanged(object? sender, EventArgs e)
        {
            if (Visibility != Visibility.Visible) return;

            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        }

        private void Editor_SelectionChanged(object? sender, EventArgs e)
        {
            BtnFindInSelection.IsEnabled =
                _editor != null && _editor.Avalon.SelectionLength > 0;
        }

        #endregion

        public void OpenSearchPanel(ScriptEditor editor)
        {
            BindEditor(editor);

            if (Visibility != Visibility.Visible)
            {
                Visibility = Visibility.Visible;
                TxtSearch.Focus();
            }

            if (!string.IsNullOrEmpty(TxtSearch.Text))
            {
                StartSearch(TxtSearch.Text);
                if (_currentMatchIndex >= 0 && _searchOffsets.Count > 0)
                    SelectCurrentMatch();
            }
        }

        private void SearchInlinePanel_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && Visibility == Visibility.Visible)
            {
                CloseSearchPanel();
                e.Handled = true;
            }
        }

        public void CloseSearchPanel()
        {
            Visibility = Visibility.Collapsed;
            ClearHighlighting();
            _editor?.Avalon.Focus();
        }

        #region Toggle & UI

        private void BtnToggleReplace_Click(object sender, RoutedEventArgs e)
        {
            bool open = BtnToggleReplace.IsChecked == true;
            ReplacePanel.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
            GlobalSearchReplaceState.IsReplacePanelOpen = open;
        }

        private void BtnMatchCase_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton tb)
            {
                GlobalSearchReplaceState.MatchCase = tb.IsChecked ?? false;
                _searchDebounceTimer.Stop();
                _searchDebounceTimer.Start();
            }
        }

        private void BtnWholeWord_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton tb)
            {
                GlobalSearchReplaceState.WholeWord = tb.IsChecked ?? false;
                _searchDebounceTimer.Stop();
                _searchDebounceTimer.Start();
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            GlobalSearchReplaceState.LastSearchText = TxtSearch.Text;
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();

            SearchTextChanged?.Invoke(this, EventArgs.Empty);
        }

        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                {
                    if (_searchOffsets.Count > 0) BtnFindPrevious_Click(sender, e);
                    else StartSearch(TxtSearch.Text);
                }
                else
                {
                    if (_searchOffsets.Count > 0) BtnFindNext_Click(sender, e);
                    else StartSearch(TxtSearch.Text);
                }

                e.Handled = true;
                Dispatcher.BeginInvoke(() => TxtSearch.Focus());
            }
        }

        #endregion

        #region Buttons Suche

        private void BtnFindNext_Click(object sender, RoutedEventArgs e)
        {
            if (_searchOffsets.Count == 0) return;
            _currentMatchIndex++;
            if (_currentMatchIndex >= _searchOffsets.Count) _currentMatchIndex = 0;
            SelectCurrentMatch();
        }

        private void BtnFindPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (_searchOffsets.Count == 0) return;
            _currentMatchIndex--;
            if (_currentMatchIndex < 0) _currentMatchIndex = _searchOffsets.Count - 1;
            SelectCurrentMatch();
        }

        private void BtnFindInSelection_Click(object sender, RoutedEventArgs e)
        {
            if (_editor == null || string.IsNullOrEmpty(SearchText)) return;

            if (BtnFindInSelection.IsChecked == true)
            {
                int selectionStart = _editor.Avalon.SelectionStart;
                int selectionLength = _editor.Avalon.SelectionLength;
                if (selectionLength == 0) return;

                string selectedText = _editor.Avalon.SelectedText;
                FindMatches(selectedText, selectionStart);

                if (_searchOffsets.Count > 0)
                {
                    _currentMatchIndex = 0;
                    SelectCurrentMatch();
                }
            }
            else
            {
                StartSearch(TxtSearch.Text);
            }
        }

        private void BtnReplace_Click(object sender, RoutedEventArgs e)
        {
            if (_editor == null || _searchOffsets.Count == 0 || _currentMatchIndex < 0) return;

            var doc = _editor.Avalon.Document;
            int offset = _searchOffsets[_currentMatchIndex];
            int searchLength = TxtSearch.Text.Length;
            string replaceText = TxtReplace.Text;

            GlobalSearchReplaceState.LastReplaceText = TxtReplace.Text;

            string currentText = doc.GetText(offset, searchLength);
            var comparison = GlobalSearchReplaceState.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            if (!string.Equals(currentText, TxtSearch.Text, comparison)) return;

            doc.BeginUpdate();
            doc.UndoStack.StartUndoGroup();
            doc.Replace(offset, searchLength, replaceText);
            doc.UndoStack.EndUndoGroup();
            doc.EndUpdate();

            int searchStart = offset + replaceText.Length;
            FindMatches(_editor.Avalon.Text, 0);

            if (_searchOffsets.Count == 0)
            {
                ClearHighlighting();
                UpdateMatchInfo(0, 0);
                return;
            }

            _currentMatchIndex = 0;
            for (int i = 0; i < _searchOffsets.Count; i++)
            {
                if (_searchOffsets[i] >= searchStart)
                {
                    _currentMatchIndex = i;
                    break;
                }
            }

            HighlightAllMatches();
            SelectCurrentMatch();
        }

        private void BtnReplaceAll_Click(object sender, RoutedEventArgs e)
        {
            if (_editor == null) return;

            string searchText = TxtSearch.Text;
            string replaceText = TxtReplace.Text;
            if (string.IsNullOrEmpty(searchText)) return;

            GlobalSearchReplaceState.LastSearchText = searchText;
            GlobalSearchReplaceState.LastReplaceText = replaceText;

            var doc = _editor.Avalon.Document;
            bool restrictToSelection = BtnFindInSelection.IsChecked == true && _editor.Avalon.SelectionLength > 0;

            string text;
            int offsetBase = 0;

            if (restrictToSelection)
            {
                text = _editor.Avalon.SelectedText;
                offsetBase = _editor.Avalon.SelectionStart;
            }
            else text = doc.Text;

            var comparison = GlobalSearchReplaceState.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            doc.BeginUpdate();
            doc.UndoStack.StartUndoGroup();

            int index = 0;
            int replacedCount = 0;

            while ((index = text.IndexOf(searchText, index, comparison)) >= 0)
            {
                if (GlobalSearchReplaceState.WholeWord)
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

                doc.Replace(offsetBase + index, searchText.Length, replaceText);
                text = restrictToSelection ? _editor.Avalon.SelectedText : doc.Text;
                index += replaceText.Length;
                replacedCount++;
            }

            doc.UndoStack.EndUndoGroup();
            doc.EndUpdate();

            if (replacedCount == 0)
            {
                UpdateMatchInfo(0, 0);
                return;
            }

            FindMatches(_editor.Avalon.Text, 0);
            HighlightAllMatches();

            _currentMatchIndex = _searchOffsets.Count > 0 ? 0 : -1;
            UpdateMatchInfo(_searchOffsets.Count > 0 ? 1 : 0, _searchOffsets.Count);

            if (_currentMatchIndex >= 0)
                SelectCurrentMatch();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            CloseSearchPanel();
        }

        #endregion

        #region Suche & Highlighting

        private void StartSearch(string searchText)
        {
            if (_editor == null || Visibility != Visibility.Visible)
            {
                TxtMatchInfo.Text = TxtMatchInfo.Text = NoResultsText;
                return;
            }

            if (string.IsNullOrEmpty(searchText))
            {
                _searchOffsets.Clear();
                _currentMatchIndex = -1;
                _currentMatch = 0;
                _totalMatches = 0;
                TxtMatchInfo.Text = TxtMatchInfo.Text = NoResultsText;
                ClearHighlighting();
                return;
            }

            FindMatches(_editor.Avalon.Text, 0);
            HighlightAllMatches();

            if (_searchOffsets.Count > 0)
            {
                _currentMatchIndex = 0;
                UpdateMatchInfo(1, _searchOffsets.Count);
            }
            else
            {
                _currentMatchIndex = -1;
                TxtMatchInfo.Text = TxtMatchInfo.Text = NoResultsText;
            }
        }

        private void FindMatches(string text, int offsetBase)
        {
            _searchOffsets.Clear();
            string searchText = TxtSearch.Text;
            if (string.IsNullOrEmpty(searchText)) return;

            var comparison = GlobalSearchReplaceState.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            int pos = 0;

            while ((pos = text.IndexOf(searchText, pos, comparison)) >= 0)
            {
                if (GlobalSearchReplaceState.WholeWord)
                {
                    bool leftOk = pos == 0 || !char.IsLetterOrDigit(text[pos - 1]);
                    int afterIndex = pos + searchText.Length;
                    bool rightOk = afterIndex >= text.Length || !char.IsLetterOrDigit(text[afterIndex]);
                    if (!(leftOk && rightOk))
                    {
                        pos++;
                        continue;
                    }
                }

                _searchOffsets.Add(offsetBase + pos);
                pos += searchText.Length;
            }

            _totalMatches = _searchOffsets.Count;
            _currentMatch = _searchOffsets.Count > 0 ? 1 : 0;
        }

        private void SelectCurrentMatch()
        {
            if (_editor == null || _searchOffsets.Count == 0 || _currentMatchIndex < 0) return;

            int offset = _searchOffsets[_currentMatchIndex];
            int length = TxtSearch.Text.Length;

            _editor.Avalon.Select(offset, length);
            _editor.Avalon.ScrollToLine(_editor.Avalon.Document.GetLineByOffset(offset).LineNumber);
            _editor.Avalon.Focus();

            _currentMatch = _currentMatchIndex + 1;
            UpdateMatchInfo(_currentMatch, _totalMatches);
        }

        private void HighlightAllMatches()
        {
            if (_editor == null || string.IsNullOrEmpty(TxtSearch.Text)) return;
            _editor.HighlightAllOccurrences(TxtSearch.Text, GlobalSearchReplaceState.MatchCase, GlobalSearchReplaceState.WholeWord);
        }

        private void ClearHighlighting()
        {
            _editor?.ClearHighlighting();
        }

        #endregion

        #region Trefferanzeige

        public void UpdateMatchInfo(int currentMatch = -1, int totalMatches = -1)
        {
            if (totalMatches >= 0) _totalMatches = totalMatches;
            if (currentMatch >= 0) _currentMatch = currentMatch;

            TxtMatchInfo.Text = _totalMatches == 0
                ? TxtMatchInfo.Text = NoResultsText
                : $"{_currentMatch} von {_totalMatches}";
        }

        #endregion

        public class TextLengthToVisibilityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is int length)
                    return length == 0 ? Visibility.Visible : Visibility.Collapsed;
                return Visibility.Visible;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
                => throw new NotImplementedException();
        }
    }
}
