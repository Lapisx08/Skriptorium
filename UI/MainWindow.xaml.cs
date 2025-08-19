using AvalonDock;
using AvalonDock.Layout;
using AvalonDock.Controls;
using MahApps.Metro.Controls;
using Skriptorium.Managers;
using Skriptorium.Parsing;
using Skriptorium.UI.Views;
using Skriptorium.UI.Views.Tools;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

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
            _tabManager = new ScriptTabManager(dockingManager, documentPane);
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
            _shortcutManager.Register(Key.H, ModifierKeys.Control,
                                      () => SyntaxHighlightingUmschalten_Click(null, null));
            _shortcutManager.Register(Key.M, ModifierKeys.Control,
                                      () => ToggleFoldings_Click(null, null));
            _shortcutManager.Register(Key.E, ModifierKeys.Control,
                                      () => GetActiveScriptEditor()?.FormatCode());
            _shortcutManager.Register(Key.F5, ModifierKeys.None,
                                      () => SyntaxCheckButton_Click(null, null));
            _shortcutManager.Register(Key.G, ModifierKeys.Control,
                                      () => MenuNPCGenerator_Click(null, null));
            _shortcutManager.Register(Key.G, ModifierKeys.Control | ModifierKeys.Shift,
                                      () => MenuDialogGenerator_Click(null, null));

            // 3. AvalonDock-Ereignisse
            dockingManager.ActiveContentChanged += DockingManager_ActiveContentChanged;

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

            // Strg+D → Duplizieren
            if (e.Key == Key.D && Keyboard.Modifiers == ModifierKeys.Control)
            {
                _editMenuManager.Duplicate();
                e.Handled = true; // verhindert Löschen!
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

        private void DockingManager_DocumentClosing(object sender, DocumentClosingEventArgs e)
        {
            if (e.Document.Content is ScriptEditor editor)
            {
                if (!_tabManager.ConfirmClose(editor))
                {
                    e.Cancel = true;
                }
            }
        }

        private void DockingManager_DocumentClosed(object sender, DocumentClosedEventArgs e)
        {
            // Entferne Ereignisbindungen für geschlossene Dokumente
            if (e.Document.Content is ScriptEditor editor)
            {
                editor.TextChanged -= ScriptEditor_TextChanged;
                editor.CaretPositionChanged -= ScriptEditor_CaretPositionChanged;
            }
        }

        private void DockingManager_ActiveContentChanged(object sender, EventArgs e)
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

        #region Menü "Skriptorium"
        private void MenuSkriptoriumUeber_Click(object? sender, RoutedEventArgs? e)
        {
            var aboutSkriptorium = new AboutSkriptoriumView
            {
                Owner = this
            };
            aboutSkriptorium.ShowDialog();
        }

        private void MenuSkriptoriumEinstellungen_Click(object? sender, RoutedEventArgs? e)
        {
            var settings = new SettingsView
            {
                Owner = this
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
                {
                    ed.TitleTextBlock.Text = System.IO.Path.GetFileName(ed.FilePath);
                    var document = documentPane.Children.FirstOrDefault(d => d.Content == ed);
                    if (document != null)
                        document.Title = System.IO.Path.GetFileName(ed.FilePath);
                }
            }
        }

        private void MenuDateiSpeichernUnter_Click(object? sender, RoutedEventArgs? e)
        {
            var ed = _tabManager.GetActiveScriptEditor();
            if (ed != null && DataManager.SaveFileAs(ed))
            {
                _dataMenuManager.UpdateRecentFilesMenu();
                if (!string.IsNullOrEmpty(ed.FilePath) && ed.TitleTextBlock != null)
                {
                    ed.TitleTextBlock.Text = System.IO.Path.GetFileName(ed.FilePath);
                    var document = documentPane.Children.FirstOrDefault(d => d.Content == ed);
                    if (document != null)
                        document.Title = System.IO.Path.GetFileName(ed.FilePath);
                }
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
                    {
                        editor.TitleTextBlock.Text = System.IO.Path.GetFileName(editor.FilePath);
                        var document = documentPane.Children.FirstOrDefault(d => d.Content == editor);
                        if (document != null)
                            document.Title = System.IO.Path.GetFileName(editor.FilePath);
                    }
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
                var dialog = new SearchReplaceScriptView(ed);
                dialog.Owner = this;
                dialog.Show();
            }
        }

        private void ReplaceInEditor_Click(object sender, RoutedEventArgs e)
        {
            var ed = _tabManager.GetActiveScriptEditor();
            if (ed != null)
            {
                var dialog = new SearchReplaceScriptView(ed);
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

            string assembly = "Skriptorium";
            var uri = new Uri($"pack://application:,,,/{assembly};component/UI/Themes/{themeName}.xaml",
                              UriKind.Absolute);

            dicts.Add(new ResourceDictionary { Source = uri });
        }

        private void SyntaxHighlightingUmschalten_Click(object sender, RoutedEventArgs e)
        {
            var layoutDocument = dockingManager.ActiveContent as LayoutDocument;
            if (layoutDocument == null)
            {
                return;
            }

            var editor = layoutDocument.Content as ScriptEditor;
            if (editor == null)
            {
                return;
            }

            editor.ToggleSyntaxHighlighting();
        }

        private void ToggleFoldings_Click(object sender, RoutedEventArgs e)
        {
            GetActiveScriptEditor()?.ToggleAllFoldings();
        }

        private void MenuCodeFormatieren_Click(object sender, RoutedEventArgs e)
        {
            var editor = _tabManager.GetActiveScriptEditor();
            if (editor != null)
            {
                editor.FormatCode();
            }
            else
            {
                MessageBox.Show("Kein aktiver Editor gefunden.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SyntaxCheckButton_Click(object sender, RoutedEventArgs e)
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

        private void MenuNPCGenerator_Click(object sender, RoutedEventArgs e)
        {
            var npcGenerator = new NPCGenerator();
            npcGenerator.Owner = this;
            npcGenerator.Show();
        }

        private void MenuDialogGenerator_Click(object sender, RoutedEventArgs e)
        {
            var dialogGenerator = new DialogGenerator();
            dialogGenerator.Owner = this;
            dialogGenerator.Show();
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
