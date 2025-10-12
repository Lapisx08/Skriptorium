using Skriptorium.Common;
using Skriptorium.Managers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Skriptorium.UI.Views
{
    public partial class SearchReplaceExplorer : UserControl
    {
        private readonly ScriptTabManager _tabManager;
        private readonly ConcurrentDictionary<string, (string Content, DateTime LastModified)> _fileCache
            = new ConcurrentDictionary<string, (string Content, DateTime LastModified)>();
        private const int MaxCacheSize = 2000;
        public ObservableCollection<FileSearchNode> SearchResults { get; } = new ObservableCollection<FileSearchNode>();
        private bool _matchCase = false;
        private bool _wholeWord = false;
        private readonly DispatcherTimer _searchTimer;
        private CancellationTokenSource _cts;
        private readonly List<string> _errors = new List<string>();

        public SearchReplaceExplorer(ScriptTabManager tabManager)
        {
            InitializeComponent();
            _tabManager = tabManager;
            SearchResultsTree.ItemsSource = SearchResults;

            SearchResultsTree.SetValue(VirtualizingStackPanel.IsVirtualizingProperty, true);
            SearchResultsTree.SetValue(VirtualizingStackPanel.VirtualizationModeProperty, VirtualizationMode.Recycling);

            _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _searchTimer.Tick += (s, e) =>
            {
                _searchTimer.Stop();
                StartSearch(TxtSearch.Text);
            };
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchTimer.Stop();
            if (string.IsNullOrEmpty(TxtSearch.Text))
                Dispatcher.Invoke(() => SearchResults.Clear(), DispatcherPriority.Background);
            else
                _searchTimer.Start();
        }

        private void BtnMatchCase_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton tb)
            {
                _matchCase = tb.IsChecked ?? false;
                StartSearch(TxtSearch.Text);
            }
        }

        private void BtnWholeWord_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton tb)
            {
                _wholeWord = tb.IsChecked ?? false;
                StartSearch(TxtSearch.Text);
            }
        }

        private void StartSearch(string searchTerm)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            _ = PerformSearchAsync(searchTerm, _cts.Token);
        }

        private async Task PerformSearchAsync(string searchTerm, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                Dispatcher.Invoke(() => SearchResults.Clear(), DispatcherPriority.Background);
                return;
            }

            string rootPath = Properties.Settings.Default.ScriptSearchPath;
            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
            {
                Dispatcher.Invoke(() =>
                    MessageBox.Show("Kein gültiger Skript-Ordner festgelegt.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning));
                return;
            }

            _errors.Clear();
            var tempResults = new ConcurrentBag<(string Path, List<FileMatch> Matches)>();

            await Task.Run(async () =>
            {
                var channel = Channel.CreateBounded<string>(100);

                var producer = Task.Run(async () =>
                {
                    string[] searchPatterns = new[] { "*.d", "*.txt", "*.src" };
                    foreach (var pattern in searchPatterns)
                    {
                        foreach (var file in Directory.EnumerateFiles(rootPath, pattern, SearchOption.AllDirectories))
                        {
                            await channel.Writer.WriteAsync(file, ct);
                        }
                    }
                    channel.Writer.Complete();
                }, ct);

                var consumer = Parallel.ForEachAsync(channel.Reader.ReadAllAsync(ct),
                    new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = ct },
                    async (file, ct) =>
                    {
                        if (ct.IsCancellationRequested) return;

                        try
                        {
                            string content;
                            var lastModified = File.GetLastWriteTime(file);

                            if (_fileCache.TryGetValue(file, out var cached) && cached.LastModified == lastModified)
                                content = cached.Content;
                            else
                            {
                                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                                content = await ReadFileAutoEncodingAsync(file, cts.Token);
                                _fileCache.AddOrUpdate(file, (content, lastModified), (k, v) => (content, lastModified));
                            }

                            var matches = new List<FileMatch>();
                            FindMatchesInScript(content, searchTerm, matches, _matchCase, _wholeWord);
                            if (matches.Count > 0)
                                tempResults.Add((file, matches));
                        }
                        catch (IOException ex)
                        {
                            lock (_errors)
                            {
                                _errors.Add($"Fehler beim Lesen der Datei {file}: {ex.Message}");
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            lock (_errors)
                            {
                                _errors.Add($"Timeout beim Lesen der Datei {file}");
                            }
                        }
                    });

                await Task.WhenAll(producer, consumer);

                if (_fileCache.Count > MaxCacheSize)
                    _fileCache.Clear();
            }, ct);

            await Dispatcher.InvokeAsync(() =>
            {
                SearchResults.Clear();
                int maxResults = 1000;
                foreach (var (path, matches) in tempResults.Take(maxResults))
                {
                    var node = new FileSearchNode(path);
                    foreach (var match in matches)
                        node.Matches.Add(match);
                    SearchResults.Add(node);
                }

                if (_errors.Any())
                {
                    MessageBox.Show($"Es traten Fehler auf:\n{string.Join("\n", _errors.Take(5))}",
                        "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }, DispatcherPriority.Background);
        }

        private void FindMatchesInScript(string text, string searchText, List<FileMatch> matches, bool isCaseSensitive, bool isWholeWord)
        {
            var comparison = isCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var lineOffsets = new List<int> { 0 };
            for (int i = 0; i < text.Length; i++)
                if (text[i] == '\n') lineOffsets.Add(i + 1);

            int offset = 0;
            while ((offset = text.IndexOf(searchText, offset, comparison)) >= 0)
            {
                if (isWholeWord)
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

                int lineNumber = Array.BinarySearch(lineOffsets.ToArray(), offset);
                lineNumber = lineNumber >= 0 ? lineNumber : ~lineNumber - 1;
                string lineText = lineNumber < lines.Length ? lines[lineNumber].Trim() : "";

                matches.Add(new FileMatch
                {
                    LineNumber = lineNumber + 1,
                    LinePreview = lineText,
                    Highlight = searchText,
                    LineOffset = offset,
                    MatchLength = searchText.Length
                });

                offset += searchText.Length;
            }
        }

        private void HighlightTextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock tb && tb.DataContext is FileMatch fm && !string.IsNullOrEmpty(fm.Highlight))
            {
                tb.Inlines.Clear();
                string text = fm.LinePreview;
                string search = fm.Highlight;
                var comp = _matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

                int last = 0;
                while (true)
                {
                    int index = text.IndexOf(search, last, comp);
                    if (index < 0) break;

                    if (index > last)
                        tb.Inlines.Add(new Run(text.Substring(last, index - last)));

                    tb.Inlines.Add(new Run(text.Substring(index, search.Length))
                    {
                        Background = new SolidColorBrush(Color.FromArgb(90, 230, 200, 40))
                    });

                    last = index + search.Length;
                }

                if (last < text.Length)
                    tb.Inlines.Add(new Run(text.Substring(last)));
            }
        }

        private void BtnReplaceAll_Click(object sender, RoutedEventArgs e)
        {
            string searchTerm = TxtSearch.Text;
            string replaceTerm = TxtReplace.Text;
            if (string.IsNullOrEmpty(searchTerm))
            {
                MessageBox.Show("Bitte geben Sie einen Suchtext ein.", "Ersetzen Alle", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _errors.Clear();
            int totalReplacedCount = 0;

            foreach (var node in SearchResults.ToList())
            {
                try
                {
                    string text;
                    var lastModified = File.GetLastWriteTime(node.FullPath);

                    if (_fileCache.TryGetValue(node.FullPath, out var cached) && cached.LastModified == lastModified)
                        text = cached.Content;
                    else
                    {
                        text = ReadFileAutoEncoding(node.FullPath);
                        _fileCache.AddOrUpdate(node.FullPath, (text, lastModified), (k, v) => (text, lastModified));
                    }

                    string newText = text;
                    int offset = 0;
                    int replacedCount = 0;
                    var comparison = _matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

                    while ((offset = newText.IndexOf(searchTerm, offset, comparison)) >= 0)
                    {
                        if (_wholeWord)
                        {
                            bool leftOk = offset == 0 || !char.IsLetterOrDigit(newText[offset - 1]);
                            int afterIndex = offset + searchTerm.Length;
                            bool rightOk = afterIndex >= newText.Length || !char.IsLetterOrDigit(newText[afterIndex]);
                            if (!(leftOk && rightOk))
                            {
                                offset++;
                                continue;
                            }
                        }

                        newText = newText.Remove(offset, searchTerm.Length).Insert(offset, replaceTerm);
                        offset += replaceTerm.Length;
                        replacedCount++;
                    }

                    if (replacedCount > 0)
                    {
                        File.WriteAllText(node.FullPath, newText);
                        _fileCache.AddOrUpdate(node.FullPath, (newText, File.GetLastWriteTime(node.FullPath)), (k, v) => (newText, File.GetLastWriteTime(node.FullPath)));
                        totalReplacedCount += replacedCount;
                    }
                }
                catch (IOException ex)
                {
                    lock (_errors) { _errors.Add($"Fehler beim Bearbeiten der Datei {node.FullPath}: {ex.Message}"); }
                }
                catch (UnauthorizedAccessException ex)
                {
                    lock (_errors) { _errors.Add($"Zugriff verweigert für {node.FullPath}: {ex.Message}"); }
                }
            }

            Dispatcher.Invoke(() =>
            {
                MessageBox.Show(totalReplacedCount == 0
                    ? "Keine Treffer gefunden zum Ersetzen."
                    : $"{totalReplacedCount} Treffer wurden ersetzt.", "Ersetzen Alle", MessageBoxButton.OK, MessageBoxImage.Information);

                if (_errors.Any())
                    MessageBox.Show($"Es traten Fehler auf:\n{string.Join("\n", _errors.Take(5))}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
            }, DispatcherPriority.Background);

            StartSearch(searchTerm);
        }

        private void SearchResultsTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SearchResultsTree.SelectedItem is FileMatch match)
                NavigateToMatch(match);
        }

        private void NavigateToMatch(FileMatch match)
        {
            var node = SearchResults.FirstOrDefault(n => n.Matches.Contains(match));
            if (node == null) return;

            string content;
            try { content = ReadFileAutoEncoding(node.FullPath); }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Datei {node.FullPath}:\n{ex.Message}");
                return;
            }

            _tabManager.AddNewTab(content, Path.GetFileName(node.FullPath), node.FullPath);

            var editor = _tabManager.GetActiveScriptEditor();
            if (editor != null)
            {
                editor.Avalon.Select(match.LineOffset, match.MatchLength);
                editor.Avalon.ScrollToLine(editor.Avalon.Document.GetLineByOffset(match.LineOffset).LineNumber);
                editor.Avalon.Focus();
            }
        }

        private static async Task<string> ReadFileAutoEncodingAsync(string filePath, CancellationToken ct)
        {
            try
            {
                string text = await File.ReadAllTextAsync(filePath, Encoding.Latin1, ct);
                if (!text.Contains('\uFFFD')) return text;
                return await File.ReadAllTextAsync(filePath, Encoding.UTF8, ct);
            }
            catch { return string.Empty; }
        }

        private static string ReadFileAutoEncoding(string filePath)
        {
            try
            {
                string text = File.ReadAllText(filePath, Encoding.Latin1);
                if (!text.Contains('\uFFFD')) return text;
                return File.ReadAllText(filePath, Encoding.UTF8);
            }
            catch { return string.Empty; }
        }

        // *** Collapse-Button ***
        private void BtnCollapseMatches_Click(object sender, RoutedEventArgs e)
        {
            foreach (var node in SearchResults)
                CollapseNodeRecursively(node);
        }

        private void CollapseNodeRecursively(FileSearchNode node)
        {
            node.IsExpanded = false;
            foreach (var child in node.Matches) { /* Leaf nodes haben keine Kinder */ }
        }
    }

    public class FileMatch
    {
        public int LineNumber { get; set; }
        public string LinePreview { get; set; }
        public string Highlight { get; set; }
        public int LineOffset { get; set; }
        public int MatchLength { get; set; }
    }

    public class FileSearchNode : System.ComponentModel.INotifyPropertyChanged
    {
        public string FullPath { get; }
        public string Name => Path.GetFileName(FullPath);
        public ObservableCollection<FileMatch> Matches { get; } = new ObservableCollection<FileMatch>();
        public ImageSource Icon => IconCache.GetIcon(FullPath, false, false);

        private bool _isExpanded = true;
        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(nameof(IsExpanded)); }
        }

        public FileSearchNode(string path) => FullPath = path;

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && b ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
