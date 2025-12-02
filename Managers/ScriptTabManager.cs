using AvalonDock;
using AvalonDock.Controls;
using AvalonDock.Layout;
using Skriptorium.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Skriptorium.Managers
{
    public class ScriptTabManager
    {
        private readonly DockingManager _dockingManager;
        private readonly LayoutDocumentPane _defaultDocumentPane;
        private int _newScriptCounter = 1;
        private bool _isInitializing = true;
        private readonly Dictionary<string, FileSystemWatcher> _watchers = new Dictionary<string, FileSystemWatcher>(StringComparer.OrdinalIgnoreCase);
        public ScriptTabManager(DockingManager dockingManager, LayoutDocumentPane documentPane)
        {
            _dockingManager = dockingManager;
            _defaultDocumentPane = documentPane;
            InitializeTabScrolling();
        }

        public bool DisableDockingManager()
        {
            bool wasEnabled = _dockingManager.IsEnabled;
            _dockingManager.IsEnabled = false;
            return wasEnabled;
        }

        public void RestoreDockingManager(bool wasEnabled)
        {
            _dockingManager.IsEnabled = wasEnabled;
        }

        public void MoveNewTabToEnd()
        {
            var pane = GetActiveDocumentPane();
            string newScriptPrefix = Application.Current.TryFindResource("NewScriptName") as string ?? "Neu";
            var newTab = pane.Children.OfType<LayoutDocument>()
                           .FirstOrDefault(d => d.Title.StartsWith(newScriptPrefix, StringComparison.OrdinalIgnoreCase));
            if (newTab != null)
            {
                pane.Children.Remove(newTab);
                pane.Children.Add(newTab);
                newTab.IsActive = true;
                _dockingManager.ActiveContent = newTab;
                // Sofortiger Fokus ohne Verzögerung
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (newTab.Content is ScriptEditor editor)
                        editor.Focus();
                    _dockingManager.Focus();
                    ScrollToRightEnd();
                }, DispatcherPriority.Render);
            }
        }

        private LayoutDocumentPane GetActiveDocumentPane()
        {
            if (_defaultDocumentPane.ChildrenCount > 0)
                return _defaultDocumentPane;
            if (_dockingManager.ActiveContent is LayoutDocument activeDoc)
                return activeDoc.Parent as LayoutDocumentPane ?? _defaultDocumentPane;
            var layoutRoot = _dockingManager.Layout;
            var documentPane = layoutRoot.Descendents().OfType<LayoutDocumentPane>()
                .FirstOrDefault(p => p.ChildrenCount > 0)
                ?? layoutRoot.Descendents().OfType<LayoutDocumentPane>().FirstOrDefault();
            if (documentPane == null)
            {
                documentPane = new LayoutDocumentPane();
                var paneGroup = layoutRoot.Descendents().OfType<LayoutDocumentPaneGroup>().FirstOrDefault();
                if (paneGroup == null)
                {
                    paneGroup = new LayoutDocumentPaneGroup();
                    var root = (LayoutRoot)layoutRoot;
                    if (root.RootPanel == null)
                        root.RootPanel = new LayoutPanel();
                    root.RootPanel.Children.Add(paneGroup);
                    paneGroup.InsertChildAt(paneGroup.ChildrenCount, documentPane);
                }
                else
                    paneGroup.InsertChildAt(paneGroup.ChildrenCount, documentPane);
            }
            return documentPane ?? _defaultDocumentPane;
        }

        public void AddNewTab(string content = "", string? tabTitle = null, string? filePath = null, bool activate = false)
        {
            if (!string.IsNullOrWhiteSpace(filePath) && TryActivateTabByFilePath(filePath))
                return;
            var scriptEditor = new ScriptEditor { FilePath = filePath ?? "" };
            scriptEditor.SetTextAndResetModified(content);
            // Dynamisches Präfix für "Neues Skript" aus den Ressourcen
            string newScriptPrefix = Application.Current.TryFindResource("NewScriptName") as string ?? "Neu";
            string baseTitle;
            if (!string.IsNullOrWhiteSpace(tabTitle))
            {
                baseTitle = tabTitle;
            }
            else if (!string.IsNullOrWhiteSpace(filePath))
            {
                baseTitle = Path.GetFileName(filePath);
            }
            else
            {
                baseTitle = $"{newScriptPrefix}{_newScriptCounter++}";
            }
            var document = new LayoutDocument { Title = baseTitle, Content = scriptEditor, IsActive = false };
            if (!string.IsNullOrWhiteSpace(filePath))
                document.ToolTip = filePath;
            scriptEditor.TextChanged += (s, e) => UpdateTabTitle(scriptEditor);
            var targetPane = GetActiveDocumentPane();
            targetPane.Children.Add(document);
            document.IsActive = true;
            _dockingManager.ActiveContent = document;
            // Fokus sofort setzen (verhindert Flackern)
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (document.Content is ScriptEditor editor)
                    editor.Focus();
                ScrollToRightEnd();
            }, DispatcherPriority.Render);
            // Neu: Watcher einrichten, wenn Dateipfad vorhanden
            SetupWatcher(filePath);
        }

        private void SetupWatcher(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath) || _watchers.ContainsKey(filePath))
                return;
            string directory = Path.GetDirectoryName(filePath)!;
            string fileName = Path.GetFileName(filePath);
            var watcher = new FileSystemWatcher
            {
                Path = directory,
                Filter = fileName,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                EnableRaisingEvents = true
            };
            watcher.Changed += OnFileChanged;
            watcher.Renamed += OnFileRenamed;
            watcher.Deleted += OnFileDeleted;
            _watchers[filePath] = watcher;
        }

        private void RemoveWatcher(string filePath)
        {
            if (_watchers.TryGetValue(filePath, out var watcher))
            {
                watcher.Changed -= OnFileChanged;
                watcher.Renamed -= OnFileRenamed;
                watcher.Deleted -= OnFileDeleted;
                watcher.Dispose();
                _watchers.Remove(filePath);
            }
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                string filePath = e.FullPath;
                var editors = GetAllOpenEditors().Where(ed => string.Equals(ed.FilePath, filePath, StringComparison.OrdinalIgnoreCase)).ToList();
                if (!editors.Any() || !File.Exists(filePath)) return;

                try
                {
                    string newContent = DataManager.ReadFileAutoEncoding(filePath);
                    foreach (var editor in editors)
                    {
                        if (newContent == editor.Text) continue;
                        if (editor.IsModified)
                        {
                            // Ohne MessageBox einfach lokale Änderungen beibehalten
                            continue;
                        }
                        editor.SetTextAndResetModified(newContent);
                        UpdateTabTitle(editor);
                    }
                    // Hinweis-MessageBox entfernt
                }
                catch (Exception ex)
                {
                    // Fehler-MessageBox entfernt, optional: Logging
                }
            });
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                string oldPath = e.OldFullPath;
                string newPath = e.FullPath;
                var editors = GetAllOpenEditors().Where(ed => string.Equals(ed.FilePath, oldPath, StringComparison.OrdinalIgnoreCase)).ToList();
                if (editors.Any())
                {
                    foreach (var editor in editors)
                    {
                        editor.FilePath = newPath;
                        var doc = GetDocumentForEditor(editor);
                        if (doc != null)
                        {
                            doc.Title = Path.GetFileName(newPath);
                            doc.ToolTip = newPath;
                        }
                        UpdateTabTitle(editor);
                    }
                    // Hinweis-MessageBox entfernt
                }
                // Watcher aktualisieren: Alten entfernen, neuen einrichten
                RemoveWatcher(oldPath);
                SetupWatcher(newPath);
            });
        }

        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                string filePath = e.FullPath;
                var editors = GetAllOpenEditors().Where(ed => string.Equals(ed.FilePath, filePath, StringComparison.OrdinalIgnoreCase)).ToList();
                if (editors.Any())
                {
                    // Hinweis-MessageBox entfernt
                }
                RemoveWatcher(filePath);
            });
        }

        private void ScrollToRightEnd()
        {
            var activePane = GetActiveDocumentPane();
            var tabControl = FindVisualChildren<TabControl>(_dockingManager)
                .FirstOrDefault(tc => tc.Items.SourceCollection == activePane.Children);
            if (tabControl != null)
            {
                var tabPanel = FindVisualChildren<DocumentPaneTabPanel>(tabControl).FirstOrDefault();
                if (tabPanel != null)
                {
                    var scrollViewer = GetAncestor<ScrollViewer>(tabPanel);
                    scrollViewer?.ScrollToRightEnd();
                }
            }
        }

        public ScriptEditor? GetActiveScriptEditor()
        {
            if (_dockingManager.ActiveContent is ScriptEditor editor) return editor;
            if (_dockingManager.ActiveContent is LayoutDocument doc && doc.Content is ScriptEditor docEditor)
                return docEditor;
            var activeDocument = GetActiveDocumentPane().Children.OfType<LayoutDocument>().FirstOrDefault(d => d.IsActive);
            if (activeDocument != null && activeDocument.Content is ScriptEditor activeEditor)
                return activeEditor;
            var firstDocument = GetActiveDocumentPane().Children.OfType<LayoutDocument>().FirstOrDefault();
            if (firstDocument != null && firstDocument.Content is ScriptEditor firstEditor)
                return firstEditor;
            return null;
        }

        public void CloseActiveTab()
        {
            var editor = GetActiveScriptEditor();
            if (editor != null && GetDocumentByEditor(editor) is LayoutDocument document)
            {
                if (ConfirmClose(editor))
                {
                    string filePath = editor.FilePath;
                    document.Close();
                    // Neu: Prüfen, ob noch Editoren für diesen Pfad offen sind
                    var remainingEditors = GetAllOpenEditors().Count(ed => string.Equals(ed.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
                    if (remainingEditors == 0)
                    {
                        RemoveWatcher(filePath);
                    }
                }
            }
        }

        public bool TryActivateTabByFilePath(string filePath)
        {
            foreach (var document in _dockingManager.Layout.Descendents()
                     .OfType<LayoutDocumentPane>()
                     .SelectMany(p => p.Children.OfType<LayoutDocument>()))
            {
                if (document.Content is ScriptEditor editor &&
                    string.Equals(editor.FilePath, filePath, StringComparison.OrdinalIgnoreCase))
                {
                    document.IsActive = true;
                    _dockingManager.ActiveContent = document;
                    return true;
                }
            }
            return false;
        }

        public bool ConfirmCloseAllTabs()
        {
            foreach (var editor in GetAllOpenEditors().ToList()) // ToList() um Modifikation während Iteration zu vermeiden
            {
                if (!ConfirmClose(editor))
                    return false;
                // Watcher entfernen, wenn letzter Editor für Path
                var remaining = GetAllOpenEditors().Count(ed => string.Equals(ed.FilePath, editor.FilePath, StringComparison.OrdinalIgnoreCase));
                if (remaining == 0)
                {
                    RemoveWatcher(editor.FilePath);
                }
            }
            return true;
        }

        public bool ConfirmClose(ScriptEditor editor)
        {
            if (!editor.IsModified)
                return true;
            string msg = Application.Current.FindResource("MsgScriptModified") as string
                             ?? "Dieses Skript wurde geändert. Möchtest du es speichern?";
            string title = Application.Current.FindResource("MsgSaveChangesTitle") as string
                             ?? "Änderungen speichern?";
            var result = MessageBox.Show(
                msg,
                title,
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning
            );
            if (result == MessageBoxResult.Cancel)
                return false;
            if (result == MessageBoxResult.Yes)
            {
                bool saved = DataManager.SaveFile(editor);
                if (!saved) return false;
                editor.ResetModifiedFlag();
                UpdateTabTitle(editor);
                var doc = GetDocumentForEditor(editor);
                if (doc != null)
                {
                    string tooltip = editor.FilePath;
                    if (string.IsNullOrWhiteSpace(tooltip))
                    {
                        tooltip = Application.Current.FindResource("MsgUnsaved") as string
                                  ?? "Nicht gespeichert";
                    }
                    doc.ToolTip = tooltip;
                }
            }
            return true;
        }

        private LayoutDocument? GetDocumentByEditor(ScriptEditor editor)
        {
            return GetActiveDocumentPane().Children.OfType<LayoutDocument>().FirstOrDefault(d => d.Content == editor);
        }

        public void UpdateTabTitle(ScriptEditor editor)
        {
            var document = GetDocumentByEditor(editor);
            if (document == null) return;
            string baseTitle = document.Title.TrimEnd('*');
            document.Title = editor.IsModified ? baseTitle + "*" : baseTitle;
        }

        public IEnumerable<ScriptEditor> GetAllOpenEditors()
        {
            foreach (var pane in _dockingManager.Layout.Descendents().OfType<LayoutDocumentPane>())
                foreach (var document in pane.Children.OfType<LayoutDocument>())
                    if (document.Content is ScriptEditor editor)
                        yield return editor;
        }

        public LayoutDocument? GetDocumentForEditor(ScriptEditor editor)
        {
            return _dockingManager.Layout.Descendents().OfType<LayoutDocument>().FirstOrDefault(d => d.Content == editor);
        }

        private void InitializeTabScrolling()
        {
            _dockingManager.Loaded += (s, e) =>
            {
                WrapTabPanelsInScrollViewer();
                RegisterMouseWheelHandlers();
                _isInitializing = false;
            };
            _dockingManager.LayoutUpdated += (s, e) => WrapTabPanelsInScrollViewer();
            _dockingManager.ActiveContentChanged += (s, e) => WrapTabPanelsInScrollViewer();
        }

        private void WrapTabPanelsInScrollViewer()
        {
            var tabPanels = FindVisualChildren<DocumentPaneTabPanel>(_dockingManager);
            foreach (var tabPanel in tabPanels)
            {
                if (tabPanel.Parent is ScrollViewer) continue;
                if (tabPanel.Parent is Panel parentPanel)
                {
                    int index = parentPanel.Children.IndexOf(tabPanel);
                    if (index == -1) continue;
                    parentPanel.Children.RemoveAt(index);
                    var scrollViewer = CreateCustomScrollViewer();
                    scrollViewer.Content = tabPanel;
                    parentPanel.Children.Insert(index, scrollViewer);
                }
                else if (tabPanel.Parent is Decorator parentDecorator)
                {
                    var oldContent = parentDecorator.Child;
                    if (oldContent != tabPanel) continue;
                    var scrollViewer = CreateCustomScrollViewer();
                    scrollViewer.Content = tabPanel;
                    parentDecorator.Child = scrollViewer;
                }
            }
        }

        private ScrollViewer CreateCustomScrollViewer()
        {
            var scrollViewer = new ScrollViewer
            {
                CanContentScroll = false,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
            var horizontalStyle = new Style(typeof(System.Windows.Controls.Primitives.ScrollBar));
            horizontalStyle.Setters.Add(new Setter(ScrollBar.OrientationProperty, Orientation.Horizontal));
            horizontalStyle.Setters.Add(new Setter(Control.TemplateProperty, CreateHorizontalScrollBarTemplate()));
            scrollViewer.Resources.Add(typeof(System.Windows.Controls.Primitives.ScrollBar), horizontalStyle);
            return scrollViewer;
        }

        private ControlTemplate CreateHorizontalScrollBarTemplate()
        {
            string xamlTemplate = @"
        <ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='ScrollBar'>
            <Grid Background='Transparent' Height='4'>
                <Track Name='PART_Track'>
                    <Track.DecreaseRepeatButton>
                        <RepeatButton Command='ScrollBar.LineLeftCommand' Opacity='0' IsTabStop='False'/>
                    </Track.DecreaseRepeatButton>
                    <Track.Thumb>
                        <Thumb Height='4'>
                            <Thumb.Template>
                                <ControlTemplate TargetType='Thumb'>
                                    <Border Background='Gray' CornerRadius='2'/>
                                </ControlTemplate>
                            </Thumb.Template>
                        </Thumb>
                    </Track.Thumb>
                    <Track.IncreaseRepeatButton>
                        <RepeatButton Command='ScrollBar.LineRightCommand' Opacity='0' IsTabStop='False'/>
                    </Track.IncreaseRepeatButton>
                </Track>
            </Grid>
        </ControlTemplate>";
            return (ControlTemplate)System.Windows.Markup.XamlReader.Parse(xamlTemplate);
        }

        private void RegisterMouseWheelHandlers()
        {
            var tabControls = FindVisualChildren<TabControl>(_dockingManager);
            foreach (var tabControl in tabControls)
            {
                tabControl.PreviewMouseWheel -= TabControl_PreviewMouseWheel;
                tabControl.PreviewMouseWheel += TabControl_PreviewMouseWheel;
            }
        }

        private IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T tChild)
                        yield return tChild;
                    foreach (T childOfChild in FindVisualChildren<T>(child))
                        yield return childOfChild;
                }
            }
        }

        private T? GetAncestor<T>(DependencyObject? start) where T : DependencyObject
        {
            var current = start;
            while (current != null)
            {
                if (current is T t) return t;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private bool IsOverTabHeader(DependencyObject hit)
        {
            while (hit != null)
            {
                if (hit is TabItem || hit is DocumentPaneTabPanel) return true;
                hit = VisualTreeHelper.GetParent(hit);
            }
            return false;
        }

        private void TabControl_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is TabControl tabControl)
            {
                var original = e.OriginalSource as DependencyObject;
                if (original == null) return;
                if (!IsOverTabHeader(original)) return;
                var tabPanel = GetAncestor<DocumentPaneTabPanel>(original) ?? FindVisualChildren<DocumentPaneTabPanel>(tabControl).FirstOrDefault();
                ScrollViewer? scrollViewer = null;
                if (tabPanel != null)
                {
                    scrollViewer = GetAncestor<ScrollViewer>(tabPanel) ?? FindVisualChildren<ScrollViewer>(tabControl).FirstOrDefault();
                }
                else
                    scrollViewer = FindVisualChildren<ScrollViewer>(tabControl).FirstOrDefault();
                if (scrollViewer != null && scrollViewer.ScrollableWidth > 0)
                {
                    double step = e.Delta / 120.0 * 100;
                    double offset = scrollViewer.HorizontalOffset - step;
                    scrollViewer.ScrollToHorizontalOffset(offset);
                    e.Handled = true;
                }
            }
        }

        // Methode zum Disposen aller Watcher (z. B. in MainWindow.OnClosing aufrufen)
        public void DisposeAllWatchers()
        {
            foreach (var kvp in _watchers.ToList())
            {
                RemoveWatcher(kvp.Key);
            }
            _watchers.Clear();
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);

        public void Execute(object parameter) => _execute(parameter);
    }
}