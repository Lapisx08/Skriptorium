using AvalonDock;
using AvalonDock.Layout;
using Skriptorium.Managers;
using Skriptorium.Parsing;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;

namespace Skriptorium.UI.Views.Tools
{
    public partial class CodeStructureView : UserControl
    {
        private ScriptEditor? _currentEditor;
        private ScriptEditor? _lastActiveEditor; // Speichert den letzten nicht-leeren Editor
        private readonly DaedalusLexer _lexer = new DaedalusLexer();
        private readonly ScriptTabManager _tabManager;
        private readonly DockingManager _dockingManager;

        public CodeStructureView(ScriptTabManager tabManager, DockingManager dockingManager)
        {
            InitializeComponent();
            _tabManager = tabManager;
            _dockingManager = dockingManager;

            // Registriere Event für Änderungen des aktiven Editors
            _dockingManager.ActiveContentChanged += DockingManager_ActiveContentChanged;
            UpdateStructure();
        }

        private void DockingManager_ActiveContentChanged(object sender, System.EventArgs e)
        {
            UpdateStructure();
        }

        public void UpdateStructure()
        {
            _currentEditor = _tabManager.GetActiveScriptEditor();

            // Wenn das aktive Skript leer ist, leere die Grids
            if (_currentEditor != null && string.IsNullOrEmpty(_currentEditor.Text))
            {
                InstancesGrid.ItemsSource = null;
                FunctionsGrid.ItemsSource = null;
                VariablesGrid.ItemsSource = null;
                return;
            }

            // Wenn ein Editor aktiv ist und nicht leer, aktualisiere den letzten aktiven Editor
            if (_currentEditor != null)
            {
                _lastActiveEditor = _currentEditor;
            }

            // Verwende den letzten aktiven Editor, wenn kein aktueller Editor aktiv ist
            var editorToUse = _lastActiveEditor ?? _currentEditor;

            if (editorToUse == null || string.IsNullOrEmpty(editorToUse.Text))
            {
                // Leere die Grids, wenn kein Editor jemals aktiv war
                InstancesGrid.ItemsSource = null;
                FunctionsGrid.ItemsSource = null;
                VariablesGrid.ItemsSource = null;
                return;
            }

            // Parse das aktuelle oder letzte Skript
            var lines = editorToUse.Text.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None);
            var tokens = _lexer.Tokenize(lines);
            var parser = new DaedalusParser(tokens);
            var declarations = parser.ParseScript();

            // Fülle die Grids
            InstancesGrid.ItemsSource = declarations.OfType<InstanceDeclaration>()
                .Select(d => new { d.Name, d.BaseClass, d.Line });
            FunctionsGrid.ItemsSource = declarations.OfType<FunctionDeclaration>()
                .Select(d => new { d.ReturnType, d.Name, ParametersString = string.Join(", ", d.Parameters), d.Line });
            VariablesGrid.ItemsSource = declarations.OfType<VarDeclaration>()
                .Select(d => new { d.Name, d.TypeName, d.Line });
        }

        private void InstancesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (InstancesGrid.SelectedItem is { } selected && selected.GetType().GetProperty("Line")?.GetValue(selected) is int line)
            {
                NavigateToLine(line);
            }
        }

        private void FunctionsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (FunctionsGrid.SelectedItem is { } selected && selected.GetType().GetProperty("Line")?.GetValue(selected) is int line)
            {
                NavigateToLine(line);
            }
        }

        private void VariablesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (VariablesGrid.SelectedItem is { } selected && selected.GetType().GetProperty("Line")?.GetValue(selected) is int line)
            {
                NavigateToLine(line);
            }
        }

        private void NavigateToLine(int line)
        {
            if (_lastActiveEditor?.Avalon != null)
            {
                // Stelle sicher, dass der Editor aktiviert wird
                var document = _tabManager.GetDocumentForEditor(_lastActiveEditor);
                if (document != null)
                {
                    document.IsActive = true;
                }
                _lastActiveEditor.Avalon.ScrollToLine(line);
                _lastActiveEditor.Avalon.TextArea.Caret.Line = line;
                _lastActiveEditor.Avalon.TextArea.Caret.Column = 1;
                _lastActiveEditor.Avalon.Focus();
            }
        }
    }
}