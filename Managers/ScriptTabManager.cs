using AvalonDock;
using AvalonDock.Layout;
using AvalonDock.Controls;
using Skriptorium.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Controls.Primitives;

namespace Skriptorium.Managers
{
    public class ScriptTabManager
    {
        private readonly DockingManager _dockingManager;
        private readonly LayoutDocumentPane _defaultDocumentPane;
        private int _newScriptCounter = 1;

        public ScriptTabManager(DockingManager dockingManager, LayoutDocumentPane documentPane)
        {
            _dockingManager = dockingManager;
            _defaultDocumentPane = documentPane;
            InitializeTabScrolling(); // Initialisiert Tab-Scrolling
        }

        // Methode zum temporären Deaktivieren des DockingManager
        public bool DisableDockingManager()
        {
            bool wasEnabled = _dockingManager.IsEnabled;
            _dockingManager.IsEnabled = false;
            return wasEnabled;
        }

        // Methode zum Wiederherstellen des DockingManager-Status
        public void RestoreDockingManager(bool wasEnabled)
        {
            _dockingManager.IsEnabled = wasEnabled;
        }

        // Methode, um das "Neu"-Tab ans Ende zu verschieben und zu fokussieren
        public void MoveNewTabToEnd()
        {
            var pane = GetActiveDocumentPane();
            var children = pane.Children.ToList();
            var newTab = children.FirstOrDefault(d => d.Title.StartsWith("Neu"));
            if (newTab != null)
            {
                children.Remove(newTab);
                children.Add(newTab); // Neu-Tab ans Ende verschieben
                pane.Children.Clear();
                foreach (var child in children)
                    pane.Children.Add(child);

                // Neu-Tab aktivieren und fokussieren
                newTab.IsActive = true;
                _dockingManager.ActiveContent = newTab;
                Console.WriteLine($"Moved 'Neu' tab to end at index {pane.ChildrenCount - 1} and focused");

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (newTab.Content is ScriptEditor editor)
                    {
                        editor.Focus();
                    }
                    _dockingManager.Focus();
                }), DispatcherPriority.Background);
            }
            else
            {
                // Fallback: Fokussiere das letzte Tab, falls kein "Neu"-Tab existiert
                var lastTab = children.LastOrDefault();
                if (lastTab != null)
                {
                    lastTab.IsActive = true;
                    _dockingManager.ActiveContent = lastTab;
                    Console.WriteLine($"No 'Neu' tab found, focused last tab at index {pane.ChildrenCount - 1}");
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (lastTab.Content is ScriptEditor editor)
                        {
                            editor.Focus();
                        }
                        _dockingManager.Focus();
                    }), DispatcherPriority.Background);
                }
                else
                {
                    Console.WriteLine("No tabs found to focus");
                }
            }
        }

        private LayoutDocumentPane GetActiveDocumentPane()
        {
            // Bevorzuge _defaultDocumentPane, wenn sie Dokumente enthält
            if (_defaultDocumentPane.ChildrenCount > 0)
            {
                return _defaultDocumentPane;
            }

            // Falls _defaultDocumentPane leer ist, suche nach der aktiven Pane
            if (_dockingManager.ActiveContent is LayoutDocument activeDoc)
            {
                var parentPane = activeDoc.Parent as LayoutDocumentPane;
                if (parentPane != null)
                    return parentPane;
            }

            // Suche nach der ersten verfügbaren LayoutDocumentPane mit Dokumenten
            var layoutRoot = _dockingManager.Layout;
            var documentPane = layoutRoot.Descendents()
                .OfType<LayoutDocumentPane>()
                .FirstOrDefault(p => p.ChildrenCount > 0)
                ?? layoutRoot.Descendents()
                    .OfType<LayoutDocumentPane>()
                    .FirstOrDefault();

            // Falls keine Pane gefunden wurde, erstelle eine neue
            if (documentPane == null)
            {
                documentPane = new LayoutDocumentPane();
                var paneGroup = layoutRoot.Descendents()
                    .OfType<LayoutDocumentPaneGroup>()
                    .FirstOrDefault();

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
                {
                    paneGroup.InsertChildAt(paneGroup.ChildrenCount, documentPane);
                }
            }

            return documentPane ?? _defaultDocumentPane;
        }

        public void AddNewTab(string content = "", string? tabTitle = null, string? filePath = null, bool activate = false)
        {
            if (!string.IsNullOrWhiteSpace(filePath) && TryActivateTabByFilePath(filePath))
                return;

            var scriptEditor = new ScriptEditor
            {
                FilePath = filePath ?? ""
            };
            scriptEditor.SetTextAndResetModified(content);

            string baseTitle = tabTitle ?? $"Neu{_newScriptCounter++}";
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                baseTitle = Path.GetFileName(filePath);
            }

            var document = new LayoutDocument
            {
                Title = baseTitle,
                Content = scriptEditor,
                IsActive = false
            };

            scriptEditor.TextChanged += (s, e) =>
            {
                UpdateTabTitle(scriptEditor);
            };

            var targetPane = GetActiveDocumentPane();
            targetPane.Children.Add(document); // Am Ende hinzufügen (rechts)
            Console.WriteLine($"Tab '{baseTitle}' added at index {targetPane.ChildrenCount - 1} in pane with {targetPane.ChildrenCount} tabs");

            // Neuen TabControl für MouseWheel-Ereignis registrieren (vermeidet Duplikate)
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                var tabControl = FindVisualChildren<TabControl>(_dockingManager)
                    .FirstOrDefault(tc => tc.Items.SourceCollection == targetPane.Children);
                if (tabControl != null)
                {
                    tabControl.PreviewMouseWheel -= TabControl_PreviewMouseWheel; // Entferne alte Handler
                    tabControl.PreviewMouseWheel += TabControl_PreviewMouseWheel;
                }

                // Wrap nach jedem Add, falls neu
                WrapTabPanelsInScrollViewer();
            }), DispatcherPriority.Background);

            // Fokus auf den neu erstellten Tab setzen
            document.IsActive = true;
            _dockingManager.ActiveContent = document;

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (document.Content is ScriptEditor editor)
                {
                    editor.Focus();
                }
                _dockingManager.Focus();
            }), DispatcherPriority.Background);
        }

        public ScriptEditor? GetActiveScriptEditor()
        {
            if (_dockingManager.ActiveContent is ScriptEditor editor)
            {
                return editor;
            }
            if (_dockingManager.ActiveContent is LayoutDocument doc && doc.Content is ScriptEditor docEditor)
            {
                return docEditor;
            }

            var activeDocument = GetActiveDocumentPane().Children.OfType<LayoutDocument>().FirstOrDefault(d => d.IsActive);
            if (activeDocument != null && activeDocument.Content is ScriptEditor activeEditor)
            {
                return activeEditor;
            }

            var firstDocument = GetActiveDocumentPane().Children.OfType<LayoutDocument>().FirstOrDefault();
            if (firstDocument != null && firstDocument.Content is ScriptEditor firstEditor)
            {
                return firstEditor;
            }

            return null;
        }

        public void CloseActiveTab()
        {
            var editor = GetActiveScriptEditor();
            if (editor != null && GetDocumentByEditor(editor) is LayoutDocument document)
            {
                document.Close();
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
            foreach (var editor in GetAllOpenEditors())
            {
                if (!ConfirmClose(editor))
                    return false;
            }
            return true;
        }

        public bool ConfirmClose(ScriptEditor editor)
        {
            if (!editor.IsModified)
                return true;

            var result = MessageBox.Show(
                "Dieses Skript wurde geändert. Möchtest du es speichern?",
                "Änderungen speichern?",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning
            );
            if (result == MessageBoxResult.Cancel)
                return false;
            if (result == MessageBoxResult.Yes)
            {
                bool saved = DataManager.SaveFile(editor);
                if (!saved)
                    return false;

                editor.ResetModifiedFlag();
                UpdateTabTitle(editor);
            }
            return true;
        }

        private LayoutDocument? GetDocumentByEditor(ScriptEditor editor)
        {
            return GetActiveDocumentPane().Children
                .OfType<LayoutDocument>()
                .FirstOrDefault(d => d.Content == editor);
        }

        public void UpdateTabTitle(ScriptEditor editor)
        {
            var document = GetDocumentByEditor(editor);
            if (document == null) return;

            string baseTitle = document.Title.TrimEnd('*');
            if (editor.IsModified)
                document.Title = baseTitle + "*";
            else
                document.Title = baseTitle;
        }

        public IEnumerable<ScriptEditor> GetAllOpenEditors()
        {
            foreach (var pane in _dockingManager.Layout.Descendents().OfType<LayoutDocumentPane>())
            {
                foreach (var document in pane.Children.OfType<LayoutDocument>())
                {
                    if (document.Content is ScriptEditor editor)
                        yield return editor;
                }
            }
        }

        public LayoutDocument? GetDocumentForEditor(ScriptEditor editor)
        {
            return _dockingManager.Layout.Descendents()
                .OfType<LayoutDocument>()
                .FirstOrDefault(d => d.Content == editor);
        }

        #region Tab-Scrolling (Verbessert)
        private void InitializeTabScrolling()
        {
            _dockingManager.Loaded += (s, e) =>
            {
                WrapTabPanelsInScrollViewer();
                RegisterMouseWheelHandlers();
            };
        }

        private void WrapTabPanelsInScrollViewer()
        {
            var tabPanels = FindVisualChildren<DocumentPaneTabPanel>(_dockingManager);
            foreach (var tabPanel in tabPanels)
            {
                if (tabPanel.Parent is ScrollViewer) continue; // Bereits gewrappt

                if (tabPanel.Parent is Panel parentPanel)
                {
                    int index = parentPanel.Children.IndexOf(tabPanel);
                    if (index == -1) continue;

                    parentPanel.Children.RemoveAt(index);

                    var scrollViewer = CreateCustomScrollViewer();
                    scrollViewer.Content = tabPanel;

                    parentPanel.Children.Insert(index, scrollViewer);
                }
                else if (tabPanel.Parent is Decorator parentDecorator) // Für Border oder ähnliche
                {
                    var oldContent = parentDecorator.Child;
                    if (oldContent != tabPanel) continue;

                    var scrollViewer = CreateCustomScrollViewer();
                    scrollViewer.Content = tabPanel;
                    parentDecorator.Child = scrollViewer;
                }
                // Erweitere bei Bedarf für andere Parent-Typen (z.B. ContentControl)
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

            // Style nur für horizontale ScrollBars
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
            <Grid Background='Transparent' Height='0'>
                <Track Name='PART_Track'>
                    <Track.DecreaseRepeatButton>
                        <RepeatButton Command='ScrollBar.LineLeftCommand' Opacity='0' IsTabStop='False'/>
                    </Track.DecreaseRepeatButton>
                        <Track.Thumb>
                            <Thumb Height='0'>
                                <Thumb.Template>
                                    <ControlTemplate TargetType='Thumb'>
                                        <Border Background='Transparent' CornerRadius='2'/>
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
                tabControl.PreviewMouseWheel -= TabControl_PreviewMouseWheel; // Vermeide Duplikate
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
                    {
                        yield return tChild;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
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
            // Akzeptiere sowohl TabItem als auch das spezielle DocumentPaneTabPanel
            while (hit != null)
            {
                if (hit is TabItem || hit is DocumentPaneTabPanel)
                    return true;
                hit = VisualTreeHelper.GetParent(hit);
            }
            return false;
        }

        private void TabControl_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is TabControl tabControl)
            {
                // Verwende OriginalSource und Suche nach Vorfahren um zuverlässiger Header-Region zu erkennen
                var original = e.OriginalSource as DependencyObject;
                if (original == null) return;

                // Wenn die Maus nicht über einem Tab-Header (TabItem / DocumentPaneTabPanel) ist, nichts tun
                if (!IsOverTabHeader(original))
                    return;

                // Versuche das zugehörige DocumentPaneTabPanel zu bestimmen
                var tabPanel = GetAncestor<DocumentPaneTabPanel>(original) ?? FindVisualChildren<DocumentPaneTabPanel>(tabControl).FirstOrDefault();

                // Finde den ScrollViewer, der das TabPanel umschließt (entweder ein Vorfahre oder ein direktes Elternteil)
                ScrollViewer? scrollViewer = null;
                if (tabPanel != null)
                {
                    scrollViewer = GetAncestor<ScrollViewer>(tabPanel);
                    if (scrollViewer == null)
                    {
                        // Falls Wrap noch nicht angewendet wurde, nehme den ersten ScrollViewer im TabControl (Fallback)
                        scrollViewer = FindVisualChildren<ScrollViewer>(tabControl).FirstOrDefault();
                    }
                }
                else
                {
                    // Fallback: erster ScrollViewer innerhalb des TabControls
                    scrollViewer = FindVisualChildren<ScrollViewer>(tabControl).FirstOrDefault();
                }

                if (scrollViewer != null && scrollViewer.ScrollableWidth > 0)
                {
                    // Horizontales Scrolling per Mausrad, Richtung angepasst für natürliche Navigation
                    double step = e.Delta / 120.0 * 100; // Positiv für Wheel up
                    double offset = scrollViewer.HorizontalOffset - step; // Inverted für left-scroll on up
                    scrollViewer.ScrollToHorizontalOffset(offset);

                    // Sehr wichtig: Das Event als handled markieren, damit das Editor-Scrollen nicht ausgeführt wird
                    e.Handled = true;
                }
            }
        }
        #endregion
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