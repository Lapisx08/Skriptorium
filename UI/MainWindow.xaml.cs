using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Skriptorium.Managers;

namespace Skriptorium.UI
{
    public partial class MainWindow : Window
    {
        private readonly ScriptTabManager _tabManager;
        private readonly ShortcutManager _shortcutManager;

        public MainWindow()
        {
            InitializeComponent();

            // Tab‑Manager initialisieren
            _tabManager = new ScriptTabManager(tabControlScripts);

            // Shortcut‑Manager initialisieren
            _shortcutManager = new ShortcutManager(this);

            // Letzte Dateien laden
            DataManager.LoadRecentFiles();
            UpdateRecentFilesMenu();

            // Start mit leerem Tab
            _tabManager.AddNewTab();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_tabManager.ConfirmCloseAllTabs())
            {
                e.Cancel = true;
                return;
            }

            DataManager.SaveRecentFiles();
            base.OnClosing(e);
        }

        #region Menü "Skriptorium"
        private void MenuSkriptoriumUeber_Click(object? sender, RoutedEventArgs? e)
            => SkriptoriumManager.ShowAboutDialog();

        private void MenuSkriptoriumEinstellungen_Click(object? sender, RoutedEventArgs? e)
            => MessageBox.Show("Einstellungen sind noch nicht implementiert.", "Einstellungen");

        private void MenuSkriptoriumBeenden_Click(object? sender, RoutedEventArgs? e)
            => Close();
        #endregion

        #region Menü "Datei"
        private void MenuDateiNeuesSkript_Click(object? sender, RoutedEventArgs? e)
            => _tabManager.AddNewTab();

        private void MenuDateiÖffnen_Click(object? sender, RoutedEventArgs? e)
        {
            DataManager.OpenFile((content, path) =>
            {
                _tabManager.AddNewTab(
                    content,
                    tabTitle: System.IO.Path.GetFileName(path),
                    filePath: path);
                UpdateRecentFilesMenu();
            });
        }

        private void UpdateRecentFilesMenu()
        {
            MenuDateiZuletztGeoeffnet.Items.Clear();
            var recent = DataManager.GetRecentFiles();
            if (recent.Count == 0)
            {
                MenuDateiZuletztGeoeffnet.Items.Add(new MenuItem { Header = "(Keine)", IsEnabled = false });
                return;
            }
            foreach (var path in recent)
            {
                var item = new MenuItem
                {
                    Header = System.IO.Path.GetFileName(path),
                    ToolTip = path,
                    Tag = path
                };
                item.Click += OpenRecentFile_Click;
                MenuDateiZuletztGeoeffnet.Items.Add(item);
            }
        }

        private void OpenRecentFile_Click(object? sender, RoutedEventArgs? e)
        {
            if (sender is MenuItem mi && mi.Tag is string path)
            {
                DataManager.OpenFile(path, (content, file) =>
                {
                    _tabManager.AddNewTab(content, System.IO.Path.GetFileName(file), file);
                    UpdateRecentFilesMenu();
                });
            }
        }

        private void MenuDateiSpeichern_Click(object? sender, RoutedEventArgs? e)
        {
            var ed = _tabManager.GetActiveScriptEditor();
            if (ed != null && DataManager.SaveFile(ed))
                UpdateRecentFilesMenu();
        }

        private void MenuDateiSpeichernUnter_Click(object? sender, RoutedEventArgs? e)
        {
            var ed = _tabManager.GetActiveScriptEditor();
            if (ed != null && DataManager.SaveFileAs(ed))
                UpdateRecentFilesMenu();
        }

        private void MenuDateiAllesSpeichern_Click(object? sender, RoutedEventArgs? e)
        {
            var editors = _tabManager.GetAllOpenEditors()
                                     .Where(ed => ed.FilePath != null)
                                     .ToList();

            bool allSaved = true;
            foreach (var editor in editors)
            {
                if (!DataManager.SaveFile(editor))
                {
                    allSaved = false;
                    break;
                }
            }

            if (allSaved)
                MessageBox.Show("Alle Dateien wurden erfolgreich gespeichert.", "Speichern");
            else
                MessageBox.Show("Einige Dateien konnten nicht gespeichert werden.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void MenuDateiSchließen_Click(object? sender, RoutedEventArgs? e)
            => _tabManager.CloseActiveTab();
        #endregion

        #region Menü "Bearbeiten"
        private void MenuBearbeitenRueckgaengig_Click(object? sender, RoutedEventArgs? e)
            => _tabManager.GetActiveScriptEditor()?.TextBox?.Undo();

        private void MenuBearbeitenWiederholen_Click(object? sender, RoutedEventArgs? e)
            => _tabManager.GetActiveScriptEditor()?.TextBox?.Redo();

        private void MenuBearbeitenAusschneiden_Click(object? sender, RoutedEventArgs? e)
            => _tabManager.GetActiveScriptEditor()?.TextBox?.Cut();

        private void MenuBearbeitenKopieren_Click(object? sender, RoutedEventArgs? e)
            => _tabManager.GetActiveScriptEditor()?.TextBox?.Copy();

        private void MenuBearbeitenEinfuegen_Click(object? sender, RoutedEventArgs? e)
            => _tabManager.GetActiveScriptEditor()?.TextBox?.Paste();
        #endregion

        #region Shortcut-Wrapper
        // Diese parameterlosen Methoden werden per Reflection aufgerufen
        private void NewScript() => MenuDateiNeuesSkript_Click(null, null);
        private void OpenScript() => MenuDateiÖffnen_Click(null, null);
        private void SaveScript() => MenuDateiSpeichern_Click(null, null);
        private void SaveScriptAs() => MenuDateiSpeichernUnter_Click(null, null);
        private void CloseActiveTab() => MenuDateiSchließen_Click(null, null);
        private void OpenSettings() => MenuSkriptoriumEinstellungen_Click(null, null);
        #endregion
    }
}
