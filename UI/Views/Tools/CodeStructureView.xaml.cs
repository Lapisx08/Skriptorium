using AvalonDock;
using AvalonDock.Layout;
using Skriptorium.Managers;
using Skriptorium.Parsing;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;

namespace Skriptorium.UI.Views.Tools
{
    public partial class CodeStructureView : UserControl
    {
        private ScriptEditor? _currentEditor;
        private ScriptEditor? _lastActiveEditor;
        private ScriptEditor? _lastProcessedEditor;
        private readonly DaedalusLexer _lexer = new DaedalusLexer();
        private readonly ScriptTabManager _tabManager;
        private readonly DockingManager _dockingManager;
        private readonly List<LayoutDocument> _subscribedDocs = new List<LayoutDocument>();

        public CodeStructureView(ScriptTabManager tabManager, DockingManager dockingManager)
        {
            InitializeComponent();
            _tabManager = tabManager;
            _dockingManager = dockingManager;
            _dockingManager.LayoutUpdated += DockingManager_LayoutUpdated;
            _dockingManager.ActiveContentChanged += DockingManager_ActiveContentChanged;
            AttachDocumentHandlers();
            UpdateStructure();
        }

        private void DockingManager_LayoutUpdated(object? sender, System.EventArgs e)
        {
            AttachDocumentHandlers();
        }

        private void AttachDocumentHandlers()
        {
            if (_dockingManager.Layout == null) return;

            var docs = _dockingManager.Layout.Descendents().OfType<LayoutDocument>().ToList();
            foreach (var doc in docs)
            {
                if (!_subscribedDocs.Contains(doc))
                {
                    doc.IsActiveChanged += LayoutDocument_IsActiveChanged;
                    _subscribedDocs.Add(doc);
                }
            }

            for (int i = _subscribedDocs.Count - 1; i >= 0; i--)
            {
                if (!docs.Contains(_subscribedDocs[i]))
                {
                    try
                    {
                        _subscribedDocs[i].IsActiveChanged -= LayoutDocument_IsActiveChanged;
                    }
                    catch { }
                    _subscribedDocs.RemoveAt(i);
                }
            }
        }

        private void LayoutDocument_IsActiveChanged(object? sender, System.EventArgs e)
        {
            if (sender is not LayoutDocument doc)
                return;

            if (!doc.IsActive)
                return;

            if (doc.Content is not ScriptEditor editor)
                return;

            if (string.IsNullOrWhiteSpace(editor.Text))
            {
                InstancesGrid.ItemsSource = null;
                FunctionsGrid.ItemsSource = null;
                VariablesGrid.ItemsSource = null;
                ConstantsGrid.ItemsSource = null;
                return;
            }

            _lastProcessedEditor = editor;
            _currentEditor = editor;
            _lastActiveEditor = editor; // Setze _lastActiveEditor, um sicherzustellen, dass es aktuell ist
            UpdateStructure();
        }

        private void DockingManager_ActiveContentChanged(object sender, System.EventArgs e)
        {
        }

        public void UpdateStructure()
        {
            var editorToUse = _lastActiveEditor ?? _currentEditor;
            if (editorToUse == null || string.IsNullOrEmpty(editorToUse.Text))
                return;

            var lines = editorToUse.Text.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None);
            var tokens = _lexer.Tokenize(lines);
            var parser = new DaedalusParser(tokens);
            var declarations = parser.ParseScript();

            InstancesGrid.ItemsSource = declarations.OfType<InstanceDeclaration>()
                .Select(d => new { d.Name, d.BaseClass, d.Line });
            FunctionsGrid.ItemsSource = declarations.OfType<FunctionDeclaration>()
                .Select(d => new { d.ReturnType, d.Name, ParametersString = string.Join(", ", d.Parameters), d.Line });
            VariablesGrid.ItemsSource = declarations
                .OfType<MultiVarDeclaration>()
                .SelectMany(d => d.Declarations)
                .Concat(declarations.OfType<ClassDeclaration>()
                    .SelectMany(c => c.Declarations.OfType<MultiVarDeclaration>()
                        .SelectMany(d => d.Declarations)))
                .Select(d => new { d.Name, d.TypeName, d.Line });
            ConstantsGrid.ItemsSource = declarations
                .OfType<ConstDeclaration>()
                .Concat(declarations.OfType<MultiConstDeclaration>()
                    .SelectMany(d => d.Declarations))
                .Concat(declarations.OfType<ClassDeclaration>()
                    .SelectMany(c => c.Declarations.OfType<MultiConstDeclaration>()
                        .SelectMany(d => d.Declarations)))
                .Select(d => new { d.Name, d.TypeName, d.Value, d.Line });
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

        private void ConstantsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ConstantsGrid.SelectedItem is { } selected && selected.GetType().GetProperty("Line")?.GetValue(selected) is int line)
            {
                NavigateToLine(line);
            }
        }

        private void NavigateToLine(int line)
        {
            var editor = _lastActiveEditor ?? _currentEditor;
            if (editor?.Avalon == null)
            {
                MessageBox.Show("Kein aktiver Editor verfügbar oder Editor nicht korrekt initialisiert.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var document = _tabManager.GetDocumentForEditor(editor);
            if (document == null)
            {
                MessageBox.Show("Das Dokument für den Editor konnte nicht gefunden werden.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validieren, ob die Zeile existiert
            int lineCount = editor.Avalon.Document.LineCount;
            if (line < 1 || line > lineCount)
            {
                MessageBox.Show($"Ungültige Zeilennummer: {line}. Das Dokument hat {lineCount} Zeilen.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            document.IsActive = true;
            editor.Avalon.ScrollToLine(line);
            editor.Avalon.TextArea.Caret.Line = line;
            editor.Avalon.TextArea.Caret.Column = 1;
            editor.Avalon.Focus();
        }
    }
}