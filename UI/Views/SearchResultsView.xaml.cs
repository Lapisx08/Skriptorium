using Skriptorium.Managers;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Skriptorium.Common;

namespace Skriptorium.UI.Views
{
    public partial class SearchResultsView : UserControl
    {
        private readonly ScriptTabManager _tabManager;
        private readonly ObservableCollection<SearchResultNode> _searchResults;

        public ObservableCollection<SearchResultNode> SearchResults => _searchResults;

        public SearchResultsView(ScriptTabManager tabManager, ObservableCollection<SearchResultNode> searchResults)
        {
            InitializeComponent();
            _tabManager = tabManager;
            _searchResults = searchResults;
            DataContext = this;
            SearchResultsTree.ItemsSource = _searchResults;
        }

        private void SearchResultsTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SearchResultsTree.SelectedItem is SearchMatch match)
            {
                NavigateToMatch(match);
            }
        }

        private void MenuNavigateToMatch_Click(object sender, RoutedEventArgs e)
        {
            if (SearchResultsTree.SelectedItem is SearchMatch match)
            {
                NavigateToMatch(match);
            }
        }

        private void NavigateToMatch(SearchMatch match)
        {
            var node = match.Parent;
            string content;
            try
            {
                content = ReadFileAutoEncoding(node.FullPath); // Datei mit korrekter Kodierung laden
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Datei {node.FullPath}:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _tabManager.AddNewTab(content, Path.GetFileName(node.FullPath), node.FullPath);
            var activeEditor = _tabManager.GetActiveScriptEditor();
            if (activeEditor != null)
            {
                activeEditor.Avalon.Select(match.Offset, match.Length);
                activeEditor.Avalon.ScrollToLine(activeEditor.Avalon.Document.GetLineByOffset(match.Offset).LineNumber);
                activeEditor.Avalon.Focus();
            }
        }

        private static string ReadFileAutoEncoding(string filePath)
        {
            // Zuerst StreamReader mit BOM-Erkennung
            try
            {
                using (var reader = new StreamReader(filePath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                {
                    return reader.ReadToEnd();
                }
            }
            catch
            {
                // Fallback: Latin1 / ANSI
                try
                {
                    return File.ReadAllText(filePath, Encoding.Latin1);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Fehler beim Lesen der Datei {filePath}: {ex.Message}");
                    throw;
                }
            }
        }
    }

    public class SearchResultNode
    {
        public string FullPath { get; }
        public string Name => string.IsNullOrEmpty(System.IO.Path.GetFileName(FullPath)) ? FullPath : System.IO.Path.GetFileName(FullPath);
        public ObservableCollection<SearchMatch> Matches { get; } = new ObservableCollection<SearchMatch>();
        public string Text { get; }
        public ImageSource Icon
        {
            get
            {
                var icon = IconCache.GetIcon(FullPath, false);
                if (icon == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Kein Icon für Pfad: {FullPath}");
                }
                return icon;
            }
        }

        public SearchResultNode(string fullPath, string text = null)
        {
            FullPath = fullPath ?? string.Empty;
            Text = text;
        }
    }

    public class SearchMatch
    {
        public int Offset { get; }
        public int Length { get; }
        public string DisplayText { get; }
        public SearchResultNode Parent { get; }

        public SearchMatch(int offset, int length, string displayText, SearchResultNode parent = null)
        {
            Offset = offset;
            Length = length;
            DisplayText = displayText;
            Parent = parent;
        }
    }
}