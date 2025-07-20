using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using Skriptorium.Properties; // <-- hier importieren

namespace Skriptorium.UI.Views
{
    public partial class SearchReplaceScriptDialog : Window
    {
        private const int MaxHistory = 10;
        private readonly ScriptEditor _scriptEditor;
        private List<string> _searchHistory;

        public string SearchText => ComboSearchText.Text;
        public string ReplaceText => ComboReplaceText.Text;

        public SearchReplaceScriptDialog(ScriptEditor scriptEditor)
        {
            InitializeComponent();
            _scriptEditor = scriptEditor;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 1. Such-History aus AppSettings laden
            var savedHistory = ConfigurationManager.AppSettings["SearchHistory"] ?? "";
            _searchHistory = savedHistory
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Take(MaxHistory)
                .ToList();
            ComboSearchText.ItemsSource = _searchHistory;
            ComboReplaceText.ItemsSource = _searchHistory;

            // 2. Default für „Suchen in“
            ComboSearchIn.SelectedIndex = 0;

            // 3. Gespeicherten Pfad (Settings) auslesen
            var savedPath = Settings.Default.ScriptSearchPath;
            if (!string.IsNullOrWhiteSpace(savedPath))
            {
                ChkSearchIn.IsChecked = true;
                ComboSearchIn.SelectedIndex = 1; // „Skripte in Pfad“
                TxtPath.Text = savedPath;
                UpdatePathControls();
            }
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
            using (var dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Ordner auswählen"
            })
            {
                if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                    TxtPath.Text = dlg.FileName;
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            // Eingabe validieren
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                MessageBox.Show("Bitte einen Suchbegriff eingeben.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // History aktualisieren
            _searchHistory.Remove(SearchText);
            _searchHistory.Insert(0, SearchText);
            if (_searchHistory.Count > MaxHistory)
                _searchHistory.RemoveAt(_searchHistory.Count - 1);

            var joined = string.Join(";", _searchHistory);

            // Konfiguration öffnen
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var settings = config.AppSettings.Settings;

            // Key existiert vielleicht noch nicht – daher zuerst prüfen
            if (settings["SearchHistory"] == null)
            {
                settings.Add("SearchHistory", joined);
            }
            else
            {
                settings["SearchHistory"].Value = joined;
            }

            // Änderungen speichern
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");

            // Such‑Scope bestimmen
            SearchScope scope;
            if (ChkSearchIn.IsChecked == true)
            {
                var sel = (ComboSearchIn.SelectedItem as ComboBoxItem)?.Content.ToString();
                scope = sel == "Alle offenen Skripte"
                    ? SearchScope.OpenDocuments
                    : SearchScope.Folder;
            }
            else
            {
                scope = SearchScope.ActiveDocument;
            }

            string path = scope == SearchScope.Folder ? TxtPath.Text : null;

            // Hier Deine eigentliche Suchen-/Ersetz‑Logik aufrufen
            // DoSearchAndReplace(_scriptEditor, SearchText, ReplaceText, scope, path, ...);

            // Falls Pfadmodus, Pfad in den User‑Settings speichern
            if (scope == SearchScope.Folder && !string.IsNullOrWhiteSpace(path))
            {
                Settings.Default.ScriptSearchPath = path;
                Settings.Default.Save();
            }

            DialogResult = true;
        }

        private enum SearchScope
        {
            ActiveDocument,
            OpenDocuments,
            Folder
        }
    }
}
