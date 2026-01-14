using AvalonDock;
using AvalonDock.Controls;
using AvalonDock.Layout;
using MahApps.Metro.Controls;
using Skriptorium.Common;
using Skriptorium.Managers;
using Skriptorium.Parsing;
using Skriptorium.UI.Views;
using Skriptorium.UI.Views.Tools;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace Skriptorium.UI
{
    public partial class MainWindow : MetroWindow
    {
        internal readonly ScriptTabManager _tabManager;
        private readonly ShortcutManager _shortcutManager;
        private readonly EditMenuManager _editMenuManager;
        private readonly SearchManager _searchManager;
        private readonly DataMenuManager _dataMenuManager;
        private ScriptEditor? _currentScriptEditor;
        private NPCGenerator? _npcGenerator;
        private OrcGenerator? _orcGenerator;
        private DialogGenerator? _dialogGenerator;
        private QuestGenerator? _questGenerator;

        public double GlobalZoom { get; set; } = 1.0;

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
                                      () =>
                                      {
                                          var editor = _tabManager.GetActiveScriptEditor();
                                          if (editor != null)
                                          {
                                                editor.SearchPanel.OpenSearchPanel(editor);
                                          }
                                      });

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
            _shortcutManager.Register(Key.A, ModifierKeys.Control | ModifierKeys.Shift,
                                      () => ToggleAutocompletion_Click(null, null));
            _shortcutManager.Register(Key.K, ModifierKeys.Control,
                                      () => Blockkommentar_Click(null, null));
            _shortcutManager.Register(Key.K, ModifierKeys.Control | ModifierKeys.Shift,
                                      () => KleinesKommentarfeld_Click(null, null));
            _shortcutManager.Register(Key.K, ModifierKeys.Control | ModifierKeys.Alt,
                                      () => MittleresKommentarfeld_Click(null, null));
            _shortcutManager.Register(Key.K, ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt,
                                      () => GroßesKommentarfeld_Click(null, null));
            _shortcutManager.Register(Key.M, ModifierKeys.Control,
                                      () => ToggleFoldings_Click(null, null));
            _shortcutManager.Register(Key.E, ModifierKeys.Control,
                                      () => GetActiveScriptEditor()?.FormatCode());
            _shortcutManager.Register(Key.F5, ModifierKeys.None,
                                      () => SyntaxCheckButton_Click(null, null));
            _shortcutManager.Register(Key.E, ModifierKeys.Control | ModifierKeys.Shift,
                                      () => MenuToolsFileExplorer_Click(null, null));
            _shortcutManager.Register(Key.F, ModifierKeys.Control | ModifierKeys.Shift,
                                      () => MenuToolsSearchExplorer_Click(null, null));
            _shortcutManager.Register(Key.C, ModifierKeys.Control | ModifierKeys.Shift,
                                      () => MenuToolsCodeStructure_Click(null, null));
            _shortcutManager.Register(Key.G, ModifierKeys.Control,
                                      () => MenuNPCGenerator_Click(null, null));
            _shortcutManager.Register(Key.G, ModifierKeys.Control | ModifierKeys.Shift,
                                      () => MenuOrcGenerator_Click(null, null));
            _shortcutManager.Register(Key.G, ModifierKeys.Control | ModifierKeys.Alt,
                                      () => MenuDialogGenerator_Click(null, null));
            _shortcutManager.Register(Key.G, ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt,
                                      () => MenuQuestGenerator_Click(null, null));

            // 3. AvalonDock-Ereignisse
            dockingManager.ActiveContentChanged += DockingManager_ActiveContentChanged;

            // 4. Letzte Dateien laden und erstes Tab öffnen
            DataManager.LoadRecentFiles();
            _dataMenuManager.UpdateRecentFilesMenu();
            this.Loaded += MainWindow_FirstLoaded;

            LanguageManager.LanguageChanged += OnLanguageChanged;
        }

        private void MainWindow_FirstLoaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= MainWindow_FirstLoaded; // nur einmal ausführen

            // 1. Zuerst alle gespeicherten Tabs wieder öffnen (falls vorhanden)
            var savedTabs = DataManager.LoadOpenTabs();
            foreach (var tab in savedTabs)
            {
                if (!string.IsNullOrWhiteSpace(tab.FilePath) && File.Exists(tab.FilePath))
                {
                    DataManager.OpenFile(tab.FilePath, (content, path) =>
                        _tabManager.AddNewTab(content, Path.GetFileName(path), path));
                }
                else if (!string.IsNullOrWhiteSpace(tab.Content))
                {
                    _tabManager.AddNewTab(tab.Content);
                }
            }
        }

        // Handler-Methode für Sprachänderungen
        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            // Panels aktualisieren (wie vorher)
            var panels = dockingManager.Layout.Descendents()
                .OfType<LayoutAnchorable>()
                .Where(a => a.ContentId == "SearchResults"
                         || a.ContentId == "CodeStructure"
                         || a.ContentId == "FileExplorer"
                         || a.ContentId == "SearchExplorer");

            foreach (var panel in panels)
            {
                string newTitle = Application.Current.FindResource(panel.ContentId) as string
                                  ?? panel.ContentId switch
                                  {
                                      "SearchResults" => "Suchergebnisse",
                                      "CodeStructure" => "Code Struktur",
                                      "FileExplorer" => "Datei Explorer",
                                      "SearchExplorer" => "Explorer Suche",
                                      _ => panel.Title
                                  };

                panel.Title = newTitle;
            }

            // „Neu“-Tabs aktualisieren
            var documentTabs = dockingManager.Layout.Descendents()
                    .OfType<LayoutDocument>()
                    .Where(d => d.Title.StartsWith("Neu") ||
                                d.Title.StartsWith("New", StringComparison.OrdinalIgnoreCase) ||
                                d.Title.StartsWith("Nowe", StringComparison.OrdinalIgnoreCase));

            string newScriptPrefix = Application.Current.TryFindResource("NewScriptName") as string ?? "Neu";
            foreach (var doc in documentTabs)
            {
                int number = 0;
                string oldTitle = doc.Title;
                string digits = new string(oldTitle.SkipWhile(c => !char.IsDigit(c)).ToArray());
                if (int.TryParse(digits, out int parsed))
                    number = parsed;
                doc.Title = $"{newScriptPrefix}{number}";
            }

            // Dialog-Generator aktualisieren (Dropdowns + Labels)
            if (_dialogGenerator != null)
            {
                _dialogGenerator.UpdateDialogComboBoxes(); // ComboBox-Texte
                _dialogGenerator.UpdateDialogLabels();      // XP / Iteminstanz / Anzahl
            }
        }

        public void OpenFileInNewTab(string content, string path)
        {
            string newTabText = Application.Current.TryFindResource("NewScriptName") as string ?? "Neu";
            string tabTitle = string.IsNullOrWhiteSpace(path) ? newTabText : Path.GetFileName(path);
            _tabManager.AddNewTab(content, tabTitle, path);
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

            if (e.Key == Key.Escape)
            {
                var editor = _tabManager.GetActiveScriptEditor();
                if (editor != null && editor.SearchPanel.Visibility == Visibility.Visible)
                {
                    editor.SearchPanel.CloseSearchPanel();  // Panel schließen
                    e.Handled = true;                       // Event stoppt weitere Verarbeitung
                }
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
            var editors = _tabManager.GetAllOpenEditors();
            DataManager.SaveOpenTabs(editors);

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
            if (_currentScriptEditor != null)
            {
                _currentScriptEditor.TextChanged -= ScriptEditor_TextChanged;
                _currentScriptEditor.CaretPositionChanged -= ScriptEditor_CaretPositionChanged;
            }

            _currentScriptEditor = _tabManager.GetActiveScriptEditor();

            if (_currentScriptEditor != null)
            {
                // Bindet das SearchPanel an den aktiven Editor
                _currentScriptEditor.SearchPanel.BindEditor(_currentScriptEditor);

                // Globale Sichtbarkeit setzen
                if (GlobalSearchReplaceState.IsSearchPanelOpen)
                    _currentScriptEditor.SearchPanel.OpenSearchPanel(_currentScriptEditor);
                else
                    _currentScriptEditor.SearchPanel.CloseSearchPanel();

                // Suchtext global setzen
                _currentScriptEditor.SearchPanel.SetSearchText(GlobalSearchReplaceState.LastSearchText);

                // Events abonnieren
                _currentScriptEditor.SearchPanel.VisibilityChanged -= SearchPanel_VisibilityChanged;
                _currentScriptEditor.SearchPanel.VisibilityChanged += SearchPanel_VisibilityChanged;

                _currentScriptEditor.SearchPanel.SearchTextChanged -= SearchPanel_SearchTextChanged;
                _currentScriptEditor.SearchPanel.SearchTextChanged += SearchPanel_SearchTextChanged;

                // Text- und Caret-Events
                _currentScriptEditor.TextChanged += ScriptEditor_TextChanged;
                _currentScriptEditor.CaretPositionChanged += ScriptEditor_CaretPositionChanged;

                _currentScriptEditor.ApplySyntaxHighlightingState();
                UpdateStatusBar();

                // Zoom einstellen
                int zoomPercent = (int)(GlobalZoom * 100);
                switch (zoomPercent)
                {
                    case 20: ZoomComboBox.SelectedIndex = 0; break;
                    case 50: ZoomComboBox.SelectedIndex = 1; break;
                    case 75: ZoomComboBox.SelectedIndex = 2; break;
                    case 100: ZoomComboBox.SelectedIndex = 3; break;
                    case 125: ZoomComboBox.SelectedIndex = 4; break;
                    case 150: ZoomComboBox.SelectedIndex = 5; break;
                    case 200: ZoomComboBox.SelectedIndex = 6; break;
                    case 300: ZoomComboBox.SelectedIndex = 7; break;
                    case 400: ZoomComboBox.SelectedIndex = 8; break;
                    default: ZoomComboBox.SelectedIndex = 3; break;
                }

                // Optional: FileExplorer synchronisieren
                SyncFileExplorerToActiveScript();
            }
            else
            {
                StatusPositionText.Text = "Zeile 1, Spalte 1";
                StatusCharCountText.Text = "0 Zeichen";
            }
        }

        public void SetGlobalZoom(double newZoom)
        {
            if (Math.Abs(GlobalZoom - newZoom) < double.Epsilon) return;

            GlobalZoom = newZoom;
            foreach (var editor in _tabManager.GetAllOpenEditors())
            {
                editor.SetZoom(GlobalZoom);
            }
        }

        private void SyncFileExplorerToActiveScript()
        {
            if (_currentScriptEditor == null || string.IsNullOrWhiteSpace(_currentScriptEditor.FilePath))
                return;

            // FileExplorer im Layout finden
            var fileExplorerAnchorable = dockingManager.Layout.Descendents()
                .OfType<LayoutAnchorable>()
                .FirstOrDefault(a => a.ContentId == "FileExplorer");

            // Nur synchronisieren, wenn FileExplorer existiert und sichtbar ist
            if (fileExplorerAnchorable != null && fileExplorerAnchorable.IsVisible &&
                fileExplorerAnchorable.Content is FileExplorerView fileExplorer)
            {
                // Zum Pfad springen
                fileExplorer.SelectAndExpandToFile(_currentScriptEditor.FilePath);
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

        private void MenuDateiSpeichern_Click(object? sender, RoutedEventArgs? e)
        {
            var ed = _tabManager.GetActiveScriptEditor();
            if (ed != null && DataManager.SaveFile(ed))
            {
                _dataMenuManager.UpdateRecentFilesMenu();
                var document = _tabManager.GetDocumentForEditor(ed);
                if (document != null)
                {
                    document.Title = System.IO.Path.GetFileName(ed.FilePath);
                }
                _tabManager.UpdateTabTitle(ed);
            }
        }

        private void MenuDateiSpeichernUnter_Click(object? sender, RoutedEventArgs? e)
        {
            var ed = _tabManager.GetActiveScriptEditor();
            if (ed != null && DataManager.SaveFileAs(ed))
            {
                _dataMenuManager.UpdateRecentFilesMenu();
                var document = _tabManager.GetDocumentForEditor(ed);
                if (document != null)
                {
                    document.Title = System.IO.Path.GetFileName(ed.FilePath);
                }
                _tabManager.UpdateTabTitle(ed);
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
                    var document = _tabManager.GetDocumentForEditor(editor);
                    if (document != null)
                    {
                        document.Title = System.IO.Path.GetFileName(editor.FilePath);
                    }
                    _tabManager.UpdateTabTitle(editor);
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
            var editor = _tabManager.GetActiveScriptEditor();
            if (editor != null)
            {
                // Inline-Panel öffnen
                editor.SearchPanel.OpenSearchPanel(editor);
            }
        }

        private void ReplaceInEditor_Click(object sender, RoutedEventArgs e)
        {
            var ed = _tabManager.GetActiveScriptEditor();
            if (ed != null)
            {
                var dialog = new SearchReplaceScriptView(ed, _tabManager, dockingManager);
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

        #region Menü "Tools"
        private void ToggleAutocompletion_Click(object sender, RoutedEventArgs e)
        {
            var editor = _tabManager.GetActiveScriptEditor();
            if (editor != null)
            {
                editor.ToggleAutocompletion();
            }
            else
            {
                MessageBox.Show("Kein aktiver Editor gefunden.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SyntaxHighlightingUmschalten_Click(object sender, RoutedEventArgs e)
        {
            var editor = _tabManager.GetActiveScriptEditor();
            if (editor != null)
            {
                editor.ToggleSyntaxHighlighting();
            }
        }

        private void KommentareButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            btn.ContextMenu.PlacementTarget = btn;
            btn.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            btn.ContextMenu.IsOpen = true;
        }

        private void Blockkommentar_Click(object sender, RoutedEventArgs e)
        {
            KommentarblockEinfügen("/* */");
        }

        private void KleinesKommentarfeld_Click(object sender, RoutedEventArgs e)
        {
            KommentarblockEinfügen("// ------  ------");
        }

        private void MittleresKommentarfeld_Click(object sender, RoutedEventArgs e)
        {
            KommentarblockEinfügen(
        @"//**************************************************************
//   
//**************************************************************");
        }

        private void GroßesKommentarfeld_Click(object sender, RoutedEventArgs e)
        {
            KommentarblockEinfügen(
        @"//##############################################################
//###
//###   
//###
//##############################################################");
        }

        private void KommentarblockEinfügen(string text)
        {
            var editor = GetActiveScriptEditor();
            if (editor?.Avalon != null)
            {
                editor.Avalon.Document.Insert(editor.Avalon.CaretOffset, text + Environment.NewLine);
            }
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
            if (_npcGenerator == null || !_npcGenerator.IsVisible)
            {
                _npcGenerator = new NPCGenerator();
                _npcGenerator.Closed += (s, args) => _npcGenerator = null;
                _npcGenerator.Show();
            }
            else
            {
                _npcGenerator.Focus();
            }
        }

        private void MenuOrcGenerator_Click(object sender, RoutedEventArgs e)
        {
            if (_orcGenerator == null || !_orcGenerator.IsVisible)
            {
                _orcGenerator = new OrcGenerator();
                _orcGenerator.Closed += (s, args) => _orcGenerator = null;
                _orcGenerator.Show();
            }
            else
            {
                _orcGenerator.Focus();
            }
        }

        private void MenuDialogGenerator_Click(object sender, RoutedEventArgs e)
        {
            if (_dialogGenerator == null || !_dialogGenerator.IsVisible)
            {
                _dialogGenerator = new DialogGenerator();
                _dialogGenerator.Closed += (s, args) => _dialogGenerator = null; // Referenz zurücksetzen
                _dialogGenerator.Show();
            }
            else
            {
                _dialogGenerator.Focus();
            }
        }

        private void MenuQuestGenerator_Click(object sender, RoutedEventArgs e)
        {
            if (_questGenerator == null || !_questGenerator.IsVisible)
            {
                _questGenerator = new QuestGenerator();
                _questGenerator.Closed += (s, args) => _questGenerator = null; // Referenz zurücksetzen
                _questGenerator.Show();
            }
            else
            {
                _questGenerator.Focus();
            }
        }

        private void MenuToolsFileExplorer_Click(object sender, RoutedEventArgs e)
        {
            ShowFileExplorer();
        }

        private void ShowFileExplorer()
        {
            var existing = dockingManager.Layout.Descendents()
                .OfType<LayoutAnchorable>()
                .FirstOrDefault(a => a.ContentId == "FileExplorer");

            if (existing != null)
            {
                // Titel bei bestehendem Panel aktualisieren
                existing.Title = Application.Current.FindResource("FileExplorer") as string ?? "Datei Explorer";
                existing.IsVisible = true;
                existing.IsActive = true;
                return;
            }

            var fileExplorer = new FileExplorerView();

            var anchorable = new LayoutAnchorable
            {
                Title = Application.Current.FindResource("FileExplorer") as string ?? "Datei Explorer",  // Initial setzen
                Content = fileExplorer,
                CanClose = true,
                CanFloat = true,
                CanHide = false,
                ContentId = "FileExplorer"
            };

            anchorable.AddToLayout(dockingManager, AnchorableShowStrategy.Left);

            var pane = anchorable.FindParent<LayoutAnchorablePane>();
            if (pane != null)
            {
                pane.DockWidth = new GridLength(200, GridUnitType.Pixel);
            }

            anchorable.IsVisible = true;
            anchorable.IsActive = true;
        }

        private void MenuToolsCodeStructure_Click(object sender, RoutedEventArgs e)
        {
            ShowCodeStructureView();
        }

        private void ShowCodeStructureView()
        {
            // Prüfen, ob Panel schon existiert
            var existing = dockingManager.Layout.Descendents()
                .OfType<LayoutAnchorable>()
                .FirstOrDefault(a => a.ContentId == "CodeStructure");

            if (existing != null)
            {
                // Titel bei bestehendem Panel dynamisch aktualisieren
                existing.Title = Application.Current.FindResource("CodeStructure") as string ?? "Code Struktur";
                existing.IsVisible = true;
                existing.IsActive = true;
                return;
            }

            // Neues Panel erstellen
            var codeStructureView = new CodeStructureView(_tabManager, dockingManager);

            var anchorable = new LayoutAnchorable
            {
                Title = Application.Current.FindResource("CodeStructure") as string ?? "Code Struktur",  // Initial
                Content = codeStructureView,
                CanClose = true,
                CanFloat = true,
                CanHide = false,
                ContentId = "CodeStructure"
            };

            // Panel im Layout hinzufügen
            anchorable.AddToLayout(dockingManager, AnchorableShowStrategy.Right);

            var pane = anchorable.FindParent<LayoutAnchorablePane>();
            if (pane != null)
            {
                pane.DockWidth = new GridLength(200, GridUnitType.Pixel);
            }

            anchorable.IsVisible = true;
            anchorable.IsActive = true;
        }
        #endregion

        private void MenuToolsSearchExplorer_Click(object sender, RoutedEventArgs e)
        {
            const string contentId = "SearchExplorer";

            // Prüfen, ob die Suche bereits existiert
            var existing = dockingManager.Layout.Descendents()
                .OfType<LayoutAnchorable>()
                .FirstOrDefault(a => a.ContentId == contentId);

            if (existing != null)
            {
                // Titel bei bestehendem Panel aktualisieren
                existing.Title = Application.Current.FindResource("SearchExplorer") as string ?? "Explorer Suche";
                existing.IsVisible = true;
                existing.IsActive = true;
                return;
            }

            // Neues UserControl erstellen
            var searchExplorer = new SearchReplaceExplorer(_tabManager);

            var anchorable = new LayoutAnchorable
            {
                Title = Application.Current.FindResource("SearchExplorer") as string ?? "Explorer Suche",  // Initial setzen
                Content = searchExplorer,
                CanClose = true,
                CanFloat = true,
                CanHide = false,
                ContentId = contentId
            };

            // Füge es links neben dem Datei Explorer hinzu
            anchorable.AddToLayout(dockingManager, AnchorableShowStrategy.Left);

            // Optional: Breite setzen
            var pane = anchorable.FindParent<LayoutAnchorablePane>();
            if (pane != null)
            {
                pane.DockWidth = new GridLength(200, GridUnitType.Pixel);
            }

            // Sichtbar und aktiv machen
            anchorable.IsVisible = true;
            anchorable.IsActive = true;
        }

        private void ScriptEditor_TextChanged(object sender, EventArgs e)
        {
            UpdateStatusBar();
            UpdateCodeStructureView();
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

        private void UpdateCodeStructureView()
        {
            var codeStructureView = dockingManager.Layout.Descendents()
                .OfType<LayoutAnchorable>()
                .FirstOrDefault(a => a.ContentId == "CodeStructure")
                ?.Content as CodeStructureView;

            codeStructureView?.UpdateStructure();
        }

        private void ZoomComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return; // Ignoriert frühe Events

            if (ZoomComboBox.SelectedItem is ComboBoxItem selectedItem &&
                selectedItem.Content is string percentString &&
                double.TryParse(percentString.TrimEnd('%'), out double percent))
            {
                double newZoom = percent / 100.0;
                SetGlobalZoom(newZoom);
            }
        }

        public void SetTheme(string themeName)
        {
            var dicts = Application.Current.Resources.MergedDictionaries;
            dicts.Clear();

            string assembly = "Skriptorium";
            var uri = new Uri($"pack://application:,,,/{assembly};component/UI/Themes/{themeName}.xaml",
                              UriKind.Absolute);

            dicts.Add(new ResourceDictionary { Source = uri });
        }

        private void SearchPanel_VisibilityChanged(object sender, EventArgs e)
        {
            if (_currentScriptEditor == null) return;

            bool isVisible = _currentScriptEditor.SearchPanel.Visibility == Visibility.Visible;
            GlobalSearchReplaceState.IsSearchPanelOpen = isVisible;

            foreach (var editor in _tabManager.GetAllOpenEditors())
            {
                if (editor == _currentScriptEditor) continue;

                if (isVisible)
                    editor.SearchPanel.OpenSearchPanel(editor);
                else
                    editor.SearchPanel.CloseSearchPanel();

                // ToggleButtons synchronisieren
                editor.SearchPanel.BtnMatchCase.IsChecked = _currentScriptEditor?.SearchPanel.BtnMatchCase.IsChecked;
                editor.SearchPanel.BtnWholeWord.IsChecked = _currentScriptEditor?.SearchPanel.BtnWholeWord.IsChecked;
            }
        }

        private void SearchPanel_SearchTextChanged(object sender, EventArgs e)
        {
            if (_currentScriptEditor == null) return;

            GlobalSearchReplaceState.LastSearchText = _currentScriptEditor.SearchPanel.SearchText;

            foreach (var editor in _tabManager.GetAllOpenEditors())
            {
                if (editor == _currentScriptEditor) continue;
                editor.SearchPanel.SetSearchText(GlobalSearchReplaceState.LastSearchText);
            }
        }
    }
}