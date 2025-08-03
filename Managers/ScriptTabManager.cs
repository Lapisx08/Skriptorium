using AvalonDock;
using AvalonDock.Layout;
using Skriptorium.UI;
using System;
using System.Collections.Generic;
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
        private readonly LayoutDocumentPane _documentPane;
        private int _newScriptCounter = 1;

        public ScriptTabManager(DockingManager dockingManager, LayoutDocumentPane documentPane)
        {
            _dockingManager = dockingManager;
            _documentPane = documentPane;
        }

        public void AddNewTab(string content = "", string? tabTitle = null, string? filePath = null)
        {
            if (!string.IsNullOrWhiteSpace(filePath) && TryActivateTabByFilePath(filePath))
                return;

            var scriptEditor = new ScriptEditor
            {
                FilePath = filePath ?? ""
            };
            scriptEditor.SetTextAndResetModified(content);

            string baseTitle = tabTitle ?? $"Neu{_newScriptCounter++}";

            var document = new LayoutDocument
            {
                Title = baseTitle,
                Content = scriptEditor,
                IsActive = true
            };

            scriptEditor.TextChanged += (s, e) =>
            {
                UpdateTabTitle(scriptEditor);
            };

            _documentPane.Children.Add(document);
            _dockingManager.ActiveContent = document;

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                document.IsActive = true;
                _dockingManager.ActiveContent = document;
                if (document.Content is ScriptEditor editor)
                {
                    editor.Focus();
                }
                _dockingManager.Focus();
            }), System.Windows.Threading.DispatcherPriority.Background);
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

            var activeDocument = _documentPane.Children.OfType<LayoutDocument>().FirstOrDefault(d => d.IsActive);
            if (activeDocument != null && activeDocument.Content is ScriptEditor activeEditor)
            {
                return activeEditor;
            }

            var firstDocument = _documentPane.Children.OfType<LayoutDocument>().FirstOrDefault();
            if (firstDocument != null && firstDocument.Content is ScriptEditor firstEditor)
            {
                return firstEditor;
            }

            return null;
        }

        public void CloseActiveTab()
        {
            var editor = GetActiveScriptEditor();
            if (editor != null && _documentPane.Children.FirstOrDefault(d => d.Content == editor) is LayoutDocument document)
            {
                if (!ConfirmClose(editor))
                    return;
                _documentPane.Children.Remove(document);
            }
        }

        public bool TryActivateTabByFilePath(string filePath)
        {
            foreach (var document in _documentPane.Children.OfType<LayoutDocument>())
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
            return _documentPane.Children
                .OfType<LayoutDocument>()
                .FirstOrDefault(d => d.Content == editor);
        }

        private void UpdateTabTitle(ScriptEditor editor)
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
            foreach (var document in _documentPane.Children.OfType<LayoutDocument>())
            {
                if (document.Content is ScriptEditor editor)
                    yield return editor;
            }
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