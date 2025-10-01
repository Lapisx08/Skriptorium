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
        private ScriptEditor? _lastProcessedEditor; // Für aktive, nicht-leere Editor-Updates

        private readonly DaedalusLexer _lexer = new DaedalusLexer();
        private readonly ScriptTabManager _tabManager;
        private readonly DockingManager _dockingManager;

        // Alle abonnierte LayoutDocuments
        private readonly List<LayoutDocument> _subscribedDocs = new List<LayoutDocument>();

        public CodeStructureView(ScriptTabManager tabManager, DockingManager dockingManager)
        {
            InitializeComponent();
            _tabManager = tabManager;
            _dockingManager = dockingManager;

            // LayoutUpdated nutzen, um neue Dokumente zu abonnieren
            _dockingManager.LayoutUpdated += DockingManager_LayoutUpdated;

            // Optional: ActiveContentChanged minimal belassen
            _dockingManager.ActiveContentChanged += DockingManager_ActiveContentChanged;

            // Initial vorhandene Dokumente abonnieren
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

            // Entferne Dokumente, die nicht mehr existieren
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

            // Leeres Skript → Grids entladen
            if (string.IsNullOrWhiteSpace(editor.Text))
            {
                InstancesGrid.ItemsSource = null;
                FunctionsGrid.ItemsSource = null;
                VariablesGrid.ItemsSource = null;

                // _lastProcessedEditor nicht setzen, damit wieder entladen werden kann
                return;
            }

            // Nicht-leeres Skript → Struktur aufbauen
            _lastProcessedEditor = editor;
            _currentEditor = editor;
            UpdateStructure();
        }

        // Minimal, optional; wird jetzt primär über IsActiveChanged gesteuert
        private void DockingManager_ActiveContentChanged(object sender, System.EventArgs e)
        {
            // Leer lassen oder nur Fallback-Logging
        }

        public void UpdateStructure()
        {
            var editorToUse = _lastActiveEditor ?? _currentEditor;
            if (editorToUse == null || string.IsNullOrEmpty(editorToUse.Text))
                return;

            // Parse Script
            var lines = editorToUse.Text.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None);
            var tokens = _lexer.Tokenize(lines);
            var parser = new DaedalusParser(tokens);
            var declarations = parser.ParseScript();

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
                var document = _tabManager.GetDocumentForEditor(_lastActiveEditor);
                if (document != null)
                    document.IsActive = true;

                _lastActiveEditor.Avalon.ScrollToLine(line);
                _lastActiveEditor.Avalon.TextArea.Caret.Line = line;
                _lastActiveEditor.Avalon.TextArea.Caret.Column = 1;
                _lastActiveEditor.Avalon.Focus();
            }
        }
    }
}
