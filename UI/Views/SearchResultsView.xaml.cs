using Skriptorium.Managers;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Skriptorium.UI.Views
{
    public partial class SearchResultsView : UserControl
    {
        private readonly ScriptTabManager _tabManager;
        private ObservableCollection<SearchResultNode> _searchResults;

        public SearchResultsView(ScriptTabManager tabManager, ObservableCollection<SearchResultNode> searchResults)
        {
            InitializeComponent();
            _tabManager = tabManager;
            _searchResults = searchResults;
            SearchResultsTree.ItemsSource = _searchResults;
        }

        private void SearchResultsTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SearchResultsTree.SelectedItem is SearchMatch match)
            {
                var node = match.Parent;
                _tabManager.AddNewTab(node.Text, System.IO.Path.GetFileName(node.FullPath), node.FullPath);
                var activeEditor = _tabManager.GetActiveScriptEditor();
                if (activeEditor != null)
                {
                    activeEditor.Avalon.Select(match.Offset, match.Length);
                    activeEditor.Avalon.ScrollToLine(activeEditor.Avalon.Document.GetLineByOffset(match.Offset).LineNumber);
                    activeEditor.Avalon.Focus();
                }
            }
        }
    }
}