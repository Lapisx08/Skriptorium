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
                }), System.Windows.Threading.DispatcherPriority.Background);
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
                    }), System.Windows.Threading.DispatcherPriority.Background);
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

            // Keine Aktivierung hier, da MoveNewTabToEnd den Fokus setzt
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