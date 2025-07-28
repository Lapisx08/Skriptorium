using Skriptorium.Interpreter;
using Skriptorium.Managers;
using Skriptorium.Parsing;
using Skriptorium.UI.Views;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MahApps.Metro.Controls;

namespace Skriptorium.UI
{
    public partial class MainWindow : MetroWindow
    {
        private readonly ScriptTabManager _tabManager;
        private readonly ShortcutManager _shortcutManager;
        private readonly EditMenuManager _editMenuManager;
        private readonly SearchManager _searchManager;
        private readonly DataMenuManager _dataMenuManager;
        private ScriptEditor? _currentScriptEditor;

        public MainWindow()
        {
            InitializeComponent();

            this.AddHandler(
                Keyboard.PreviewKeyDownEvent,
                new KeyEventHandler(MainWindow_PreviewKeyDown),
                handledEventsToo: true);

            // 1. Manager initialisieren
            _tabManager = new ScriptTabManager(tabControlScripts);
            _editMenuManager = new EditMenuManager(_tabManager);
            _searchManager = new SearchManager(_tabManager);
            _shortcutManager = new ShortcutManager(this);
            _dataMenuManager = new DataMenuManager(_tabManager, MenuDateiZuletztGeoeffnet);

            // 2. Shortcuts registrieren
            _shortcutManager.Register(Key.I, ModifierKeys.Control,
                                  () => MenuSkriptoriumUeber_Click(null, null));
            _shortcutManager.Register(Key.OemComma, ModifierKeys.Control,
                                      () => MenuSkriptoriumEinstellungen_Click(null, null));
            _shortcutManager.Register(Key.N, ModifierKeys.Control,
                                      () => _tabManager.AddNewTab());
            _shortcutManager.Register(Key.O, ModifierKeys.Control,
                                      () => MenuDateiÖffnen_Click(null, null));
            _shortcutManager.Register(Key.S, ModifierKeys.Control,
                                      () => MenuDateiSpeichern_Click(null, null));
            _shortcutManager.Register(Key.S, ModifierKeys.Control | ModifierKeys.Shift,
                                      () => MenuDateiSpeichernUnter_Click(null, null));
            _shortcutManager.Register(Key.S, ModifierKeys.Control | ModifierKeys.Alt,
                                      () => MenuDateiAllesSpeichern_Click(null, null));
            _shortcutManager.Register(Key.W, ModifierKeys.Control,
                                      () => _tabManager.CloseActiveTab());
            _shortcutManager.Register(Key.D, ModifierKeys.Control,
                                      () => _editMenuManager.Duplicate());
            _shortcutManager.Register(Key.A, ModifierKeys.Control,
                                      () => _editMenuManager.SelectAll());
            _shortcutManager.Register(Key.F, ModifierKeys.Control,
                                      () => FindInEditor_Click(null, null));

            // Lesezeichen-Shortcuts
            _shortcutManager.Register(Key.F2, ModifierKeys.None,
                                      () => GetActiveScriptEditor()?.ToggleBookmarkAtCaret());
            _shortcutManager.Register(Key.F2, ModifierKeys.Control,
                                      () => GetActiveScriptEditor()?.GotoNextBookmark());
            _shortcutManager.Register(Key.F2, ModifierKeys.Control | ModifierKeys.Shift,
                                      () => GetActiveScriptEditor()?.GotoPreviousBookmark());
            _shortcutManager.Register(Key.F2, ModifierKeys.Control | ModifierKeys.Alt,
                                      () => GetActiveScriptEditor()?.ClearAllBookmarks());

            // Tool-Shortcuts
            _shortcutManager.Register(Key.E, ModifierKeys.Control,
                                      () => GetActiveScriptEditor()?.FormatCode());

            // 3. TabControl-Ereignis registrieren
            tabControlScripts.SelectionChanged += TabControlScripts_SelectionChanged;

            // 4. Letzte Dateien laden und erstes Tab öffnen
            DataManager.LoadRecentFiles();
            _dataMenuManager.UpdateRecentFilesMenu();
            _tabManager.AddNewTab();
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Strg+Alt+S → Alle speichern
            if ((e.SystemKey == Key.S || e.Key == Key.S) &&
                (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt))
                    == (ModifierKeys.Control | ModifierKeys.Alt))
            {
                MenuDateiAllesSpeichern_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }

            // Strg+I → Skriptorium-Info
            if (e.Key == Key.I && Keyboard.Modifiers == ModifierKeys.Control)
            {
                MenuSkriptoriumUeber_Click(null, null);
                e.Handled = true;
            }
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
        {
            var aboutDialog = new AboutDialog
            {
                Owner = this
            };
            aboutDialog.ShowDialog();
        }

        private void MenuSkriptoriumEinstellungen_Click(object? sender, RoutedEventArgs? e)
        {
            var settings = new SettingsView
            {
                Owner = this    // Owner auf MainWindow setzen
            };
            settings.ShowDialog();
        }

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
                _tabManager.AddNewTab(content, System.IO.Path.GetFileName(path), path);
                _dataMenuManager.UpdateRecentFilesMenu();
            });
        }

        private void OpenRecentFile_Click(object? sender, RoutedEventArgs? e)
        {
            if (sender is MenuItem mi && mi.Tag is string path)
            {
                DataManager.OpenFile(path, (content, file) =>
                {
                    _tabManager.AddNewTab(content, System.IO.Path.GetFileName(file), file);
                    _dataMenuManager.UpdateRecentFilesMenu();
                });
            }
        }

        private void MenuDateiSpeichern_Click(object? sender, RoutedEventArgs? e)
        {
            var ed = _tabManager.GetActiveScriptEditor();
            if (ed != null && DataManager.SaveFile(ed))
            {
                _dataMenuManager.UpdateRecentFilesMenu();
                if (!string.IsNullOrEmpty(ed.FilePath) && ed.TitleTextBlock != null)
                    ed.TitleTextBlock.Text = System.IO.Path.GetFileName(ed.FilePath);
            }
        }

        private void MenuDateiSpeichernUnter_Click(object? sender, RoutedEventArgs? e)
        {
            var ed = _tabManager.GetActiveScriptEditor();
            if (ed != null && DataManager.SaveFileAs(ed))
            {
                _dataMenuManager.UpdateRecentFilesMenu();
                if (!string.IsNullOrEmpty(ed.FilePath) && ed.TitleTextBlock != null)
                    ed.TitleTextBlock.Text = System.IO.Path.GetFileName(ed.FilePath);
            }
        }

        private void MenuDateiAllesSpeichern_Click(object? sender, RoutedEventArgs? e)
        {
            var editors = _tabManager.GetAllOpenEditors()
                                     .Where(ed => ed.FilePath != null)
                                     .ToList();
            bool allSaved = true;
            foreach (var editor in editors)
            {
                if (DataManager.SaveFile(editor))
                {
                    if (editor.TitleTextBlock != null)
                        editor.TitleTextBlock.Text = System.IO.Path.GetFileName(editor.FilePath);
                }
                else
                {
                    allSaved = false;
                    break;
                }
            }
            _dataMenuManager.UpdateRecentFilesMenu();
        }

        private void MenuDateiSchließen_Click(object? sender, RoutedEventArgs? e)
            => _tabManager.CloseActiveTab();
        #endregion

        #region Menü "Bearbeiten"
        private void MenuBearbeitenRueckgaengig_Click(object? sender, RoutedEventArgs? e)
            => _tabManager.GetActiveScriptEditor()?.Avalon?.Undo();

        private void MenuBearbeitenWiederholen_Click(object? sender, RoutedEventArgs? e)
            => _tabManager.GetActiveScriptEditor()?.Avalon?.Redo();

        private void MenuBearbeitenAusschneiden_Click(object? sender, RoutedEventArgs? e)
            => _tabManager.GetActiveScriptEditor()?.Avalon?.Cut();

        private void MenuBearbeitenKopieren_Click(object? sender, RoutedEventArgs? e)
            => _tabManager.GetActiveScriptEditor()?.Avalon?.Copy();

        private void MenuBearbeitenEinfuegen_Click(object? sender, RoutedEventArgs? e)
            => _tabManager.GetActiveScriptEditor()?.Avalon?.Paste();

        private void MenuBearbeitenDuplizieren_Click(object? sender, RoutedEventArgs? e)
            => _editMenuManager.Duplicate();

        private void MenuBearbeitenAllesAuswaehlen_Click(object? sender, RoutedEventArgs? e)
            => _editMenuManager.SelectAll();
        #endregion

        #region Menü "Suchen"
        private void FindInEditor_Click(object sender, RoutedEventArgs e)
        {
            var ed = _tabManager.GetActiveScriptEditor();
            if (ed != null)
            {
                var dialog = new SearchReplaceScriptDialog(ed);
                dialog.Owner = this;
                dialog.Show();
            }
        }

        private void ReplaceInEditor_Click(object sender, RoutedEventArgs e)
        {
            var ed = _tabManager.GetActiveScriptEditor();
            if (ed != null)
            {
                var dialog = new SearchReplaceScriptDialog(ed);
                dialog.Owner = this;
                dialog.Show();
            }
        }
        #endregion

        #region Menü "Lesezeichen"
        private ScriptEditor? GetActiveScriptEditor()
            => _tabManager.GetActiveScriptEditor();

        private void MenuLesezeichenUmschalten_Click(object sender, RoutedEventArgs e)
            => GetActiveScriptEditor()?.ToggleBookmarkAtCaret();

        private void MenuLesezeichenNaechstes_Click(object sender, RoutedEventArgs e)
            => GetActiveScriptEditor()?.GotoNextBookmark();

        private void MenuLesezeichenVorheriges_Click(object sender, RoutedEventArgs e)
            => GetActiveScriptEditor()?.GotoPreviousBookmark();

        private void MenuLesezeichenAlleLoeschen_Click(object sender, RoutedEventArgs e)
            => GetActiveScriptEditor()?.ClearAllBookmarks();
        #endregion

        public void SetTheme(string themeName)
        {
            var dicts = Application.Current.Resources.MergedDictionaries;
            dicts.Clear();

            // ACHTUNG: Assembly-Name korrekt angeben!
            string assembly = "Skriptorium";
            var uri = new Uri($"pack://application:,,,/{assembly};component/UI/Themes/{themeName}.xaml",
                              UriKind.Absolute);

            dicts.Add(new ResourceDictionary { Source = uri });
        }

        private void SyntaxHighlightingUmschalten_Click(object sender, RoutedEventArgs e)
        {
            if ((tabControlScripts.SelectedItem as TabItem)?.Content is ScriptEditor editor)
            {
                editor.ToggleSyntaxHighlighting();
            }
        }

        private void SemanticCheckButton_Click(object sender, RoutedEventArgs e)
        {
            var editor = _tabManager.GetActiveScriptEditor();
            if (editor == null)
            {
                MessageBox.Show("Kein aktives Skript geöffnet.", "Hinweis",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var errors = editor.CheckAll();

            if (errors.Count > 0)
            {
                MessageBox.Show(string.Join(Environment.NewLine, errors),
                                "Fehler gefunden",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show("Keine Fehler gefunden.", "Prüfung erfolgreich",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private ScriptEditor? GetCurrentEditor()
        {
            if (tabControlScripts.SelectedItem is TabItem selectedTab &&
                selectedTab.Content is ScriptEditor editor)
            {
                return editor;
            }

            return null;
        }

        private void MenuCodeFormatieren_Click(object sender, RoutedEventArgs e)
        {
            var editor = GetCurrentEditor();
            if (editor != null)
            {
                editor.FormatCode(); // Diese Methode musst du im ScriptEditor bereitstellen
            }
            else
            {
                MessageBox.Show("Kein aktiver Editor gefunden.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void TabControlScripts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Alte Ereignisbindungen entfernen
            if (_currentScriptEditor != null)
            {
                _currentScriptEditor.TextChanged -= ScriptEditor_TextChanged;
                _currentScriptEditor.CaretPositionChanged -= ScriptEditor_CaretPositionChanged;
            }

            // Neuen ScriptEditor abrufen
            _currentScriptEditor = _tabManager.GetActiveScriptEditor();

            // Neue Ereignisbindungen hinzufügen
            if (_currentScriptEditor != null)
            {
                _currentScriptEditor.TextChanged += ScriptEditor_TextChanged;
                _currentScriptEditor.CaretPositionChanged += ScriptEditor_CaretPositionChanged;
                UpdateStatusBar();
            }
            else
            {
                StatusPositionText.Text = "Zeile 1, Spalte 1";
                StatusCharCountText.Text = "0 Zeichen";
            }
        }

        private void ScriptEditor_TextChanged(object sender, EventArgs e)
        {
            UpdateStatusBar();
        }

        private void ScriptEditor_CaretPositionChanged(object sender, EventArgs e)
        {
            UpdateStatusBar();
        }

        private void UpdateStatusBar()
        {
            if (_currentScriptEditor?.Avalon.Document != null)
            {
                var caret = _currentScriptEditor.Avalon.TextArea.Caret;
                StatusPositionText.Text = $"Zeile {caret.Line}, Spalte {caret.Column}";
                StatusCharCountText.Text = $"{_currentScriptEditor.Avalon.Document.TextLength} Zeichen";
            }
            else
            {
                StatusPositionText.Text = "Zeile 1, Spalte 1";
                StatusCharCountText.Text = "0 Zeichen";
            }
        }
    }
}
