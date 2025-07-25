using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using Skriptorium.Interpreter;
using Skriptorium.Managers;
using Skriptorium.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Skriptorium.UI
{
    public partial class ScriptEditor : UserControl
    {
        private string _originalText = "";
        private bool _suppressChangeTracking = false;
        private string _filePath = "";

        private TextSegmentCollection<TextMarker>? _markers;
        private TextMarkerRenderer? _markerRenderer;

        private BookmarkManager? _bookmarkManager;

        private DaedalusInterpreter? _interpreter;

        public ScriptEditor()
        {
            InitializeComponent();
            avalonEditor.TextChanged += AvalonEditor_TextChanged;

            _markers = new TextSegmentCollection<TextMarker>(avalonEditor.Document);
            _markerRenderer = new TextMarkerRenderer(avalonEditor.TextArea.TextView, _markers);
            avalonEditor.TextArea.TextView.BackgroundRenderers.Add(_markerRenderer);

            _bookmarkManager = new BookmarkManager(avalonEditor);
        }

        private void AvalonEditor_TextChanged(object? sender, EventArgs e)
        {
            if (_suppressChangeTracking)
                return;

            IsModified = avalonEditor.Text != _originalText;
            TextChanged?.Invoke(this, null);

            // Optional: Automatisches Parsen nach jeder Änderung
            // ParseAndHighlightErrors(); // Vorsicht: Kann performance-intensiv sein!
        }

        public bool IsModified { get; private set; } = false;

        public string Text => avalonEditor.Text;

        public string FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                _originalText = avalonEditor.Text;
                ResetModifiedFlag();
            }
        }

        public void SetTextAndResetModified(string text)
        {
            _suppressChangeTracking = true;
            avalonEditor.Text = text;
            _originalText = text;
            _suppressChangeTracking = false;
            IsModified = false;
            ClearHighlighting();
        }

        public void SetTextAndMarkAsModified(string text)
        {
            _suppressChangeTracking = true;
            avalonEditor.Text = text;
            _suppressChangeTracking = false;
            IsModified = true;
            TextChanged?.Invoke(this, null);
            ClearHighlighting();
        }

        public void ResetModifiedFlag()
        {
            _originalText = avalonEditor.Text;
            IsModified = false;

            if (TitleTextBlock != null && TitleTextBlock.Text.EndsWith("*"))
            {
                TitleTextBlock.Text = TitleTextBlock.Text[..^1];
            }
        }

        public event TextChangedEventHandler? TextChanged;

        public TextEditor Avalon => avalonEditor;

        public TextBlock? TitleTextBlock { get; set; }

        public void HighlightAllOccurrences(string searchText, bool matchCase = false, bool wholeWord = false, bool restrictToSelection = false, int selectionStart = 0, int selectionLength = 0)
        {
            if (string.IsNullOrWhiteSpace(searchText) || _markers == null || _markerRenderer == null)
                return;

            foreach (var m in _markers.ToList())
                _markers.Remove(m);

            string text = restrictToSelection && selectionLength > 0 ? avalonEditor.SelectedText : avalonEditor.Text;
            int offsetBase = restrictToSelection && selectionLength > 0 ? selectionStart : 0;

            StringComparison cmp = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            int offset = 0;

            while ((offset = text.IndexOf(searchText, offset, cmp)) >= 0)
            {
                if (wholeWord)
                {
                    bool leftOk = offset == 0 || !Char.IsLetterOrDigit(text[offset - 1]);
                    int afterIndex = offset + searchText.Length;
                    bool rightOk = afterIndex >= text.Length || !Char.IsLetterOrDigit(text[afterIndex]);

                    if (!(leftOk && rightOk))
                    {
                        offset++;
                        continue;
                    }
                }

                var marker = new TextMarker(avalonEditor.Document, offsetBase + offset, searchText.Length)
                {
                    BackgroundColor = Colors.Yellow,
                    ForegroundColor = Colors.Black
                };
                _markers.Add(marker);

                offset += searchText.Length;
            }

            avalonEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Selection);
            avalonEditor.TextArea.TextView.InvalidateVisual();
        }

        public void ClearAllBookmarks()
        {
            _bookmarkManager?.ClearAll();
        }

        public void GotoNextBookmark()
        {
            _bookmarkManager?.GotoNext();
        }

        public void GotoPreviousBookmark()
        {
            _bookmarkManager?.GotoPrevious();
        }

        public void ToggleBookmarkAtCaret()
        {
            int lineNumber = avalonEditor.TextArea.Caret.Line;
            _bookmarkManager?.ToggleBookmark(lineNumber);
        }

        public void ClearHighlighting()
        {
            if (_markers == null) return;
            foreach (var m in _markers.ToList()) _markers.Remove(m);
            avalonEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Selection);
            avalonEditor.TextArea.TextView.InvalidateVisual();
        }

        public void HighlightError(int line, int column, int length)
        {
            if (_markers == null) return;
            try
            {
                int offset = avalonEditor.Document.GetOffset(line, column);
                var marker = new TextMarker(avalonEditor.Document, offset, length)
                {
                    BackgroundColor = Colors.Red,
                    ForegroundColor = Colors.White
                };
                _markers.Add(marker);
                avalonEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Selection);
            }
            catch { }
        }

        // GeneratedRegex für Semantik-Fehler
        [GeneratedRegex(@"Zeile\s+(\d+),\s*Spalte\s+(\d+)")]
        private static partial Regex ErrorPositionRegex();

        // Führt Syntax-Check und Semantik-Check durch
        public List<string> CheckAll()
        {
            ClearHighlighting();
            var errors = new List<string>();

            // --- Syntax-Check ---
            try
            {
                var lines = avalonEditor.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                var tokens = new DaedalusLexer().Tokenize(lines);

                new DaedalusParser(tokens).ParseScript();
            }
            catch (ParseException ex)
            {
                HighlightError(ex.Line, ex.Column, ex.Found?.Length ?? 1);
                errors.Add(ex.Message);
                return errors;
            }

            // --- Semantik-Check ---
            try
            {
                var tokens2 = new DaedalusLexer()
                    .Tokenize(avalonEditor.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None));
                var parsingDecls = new DaedalusParser(tokens2).ParseScript();

                // Konvertiere Parsing.Declaration in Interpreter.Declaration
                var interpreterDecls = ConvertDeclarations(parsingDecls);

                _interpreter = new DaedalusInterpreter();
                _interpreter.LoadDeclarations(interpreterDecls);
                var semErr = _interpreter.SemanticCheck();

                errors.AddRange(semErr);

                foreach (var msg in semErr)
                {
                    var m = ErrorPositionRegex().Match(msg);
                    if (m.Success)
                    {
                        int line = int.Parse(m.Groups[1].Value);
                        int column = int.Parse(m.Groups[2].Value);
                        HighlightError(line, column, 1);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ausnahme abgefangen: {ex.Message}"); // Debug-Ausgabe
                errors.Add(ex.Message);
                var lines = ex.Message.Split(new[] { "\n" }, StringSplitOptions.None);
                foreach (var msg in lines)
                {
                    Console.WriteLine($"Verarbeite Zeile: {msg}"); // Debug-Ausgabe
                    var m = ErrorPositionRegex().Match(msg);
                    if (m.Success)
                    {
                        int line = int.Parse(m.Groups[1].Value);
                        int column = int.Parse(m.Groups[2].Value);
                        Console.WriteLine($"Markiere Fehler bei Zeile {line}, Spalte {column}"); // Debug-Ausgabe
                        HighlightError(line, column, 1);
                    }
                    else
                    {
                        Console.WriteLine("Kein Regex-Match gefunden"); // Debug-Ausgabe
                    }
                }
            }

            return errors;
        }

        // Hilfsfunktion zum Konvertieren von Declarations
        private List<Skriptorium.Interpreter.Declaration> ConvertDeclarations(List<Skriptorium.Parsing.Declaration> parsingDecls)
        {
            var interpreterDecls = new List<Skriptorium.Interpreter.Declaration>();

            foreach (var decl in parsingDecls)
            {
                switch (decl)
                {
                    case Skriptorium.Parsing.FunctionDeclaration func:
                        interpreterDecls.Add(new Skriptorium.Interpreter.FunctionDeclaration(
                            name: func.Name,
                            parameters: func.Parameters,
                            body: func.Body?.Select(s => ConvertStatement(s)).ToList() ?? new List<Skriptorium.Interpreter.Statement>(),
                            line: func.Line,
                            column: func.Column
                        ));
                        break;
                    case Skriptorium.Parsing.VarDeclaration varDecl:
                        interpreterDecls.Add(new Skriptorium.Interpreter.VarDeclaration(
                            name: varDecl.Name,
                            typeName: varDecl.TypeName,
                            line: varDecl.Line,
                            column: varDecl.Column
                        ));
                        break;
                    case Skriptorium.Parsing.InstanceDeclaration instance:
                        // Konvertiere Body (Statements) in Assignments
                        var assignments = instance.Body
                            .OfType<Skriptorium.Parsing.Assignment>()
                            .Select(a => ConvertAssignment(a))
                            .ToList();
                        interpreterDecls.Add(new Skriptorium.Interpreter.InstanceDeclaration(
                            name: instance.Name,
                            baseType: instance.BaseClass, // BaseClass -> BaseType
                            assignments: assignments,
                            line: instance.Line,
                            column: instance.Column
                        ));
                        break;
                    case Skriptorium.Parsing.ClassDeclaration:
                    case Skriptorium.Parsing.PrototypeDeclaration:
                        // Ignoriere ClassDeclaration und PrototypeDeclaration, da Interpreter sie nicht unterstützt
                        Console.WriteLine($"Warnung: Ignoriere Deklarationstyp {decl.GetType().Name} bei Zeile {decl.Line}, Spalte {decl.Column}");
                        break;
                    default:
                        throw new Exception($"Unbekannter Deklarationstyp: {decl.GetType().Name} bei Zeile {decl.Line}, Spalte {decl.Column}");
                }
            }

            return interpreterDecls;
        }

        // Hilfsfunktion zum Konvertieren von Statements
        private Skriptorium.Interpreter.Statement ConvertStatement(Skriptorium.Parsing.Statement stmt)
        {
            switch (stmt)
            {
                case Skriptorium.Parsing.Assignment assign:
                    return new Skriptorium.Interpreter.Assignment(
                        left: ConvertExpression(assign.Left),
                        right: ConvertExpression(assign.Right),
                        line: assign.Line,
                        column: assign.Column
                    );
                case Skriptorium.Parsing.ExpressionStatement exprStmt:
                    return new Skriptorium.Interpreter.ExpressionStatement(
                        expr: ConvertExpression(exprStmt.Expr)
                    );
                case Skriptorium.Parsing.IfStatement ifStmt:
                    return new Skriptorium.Interpreter.IfStatement(
                        condition: ConvertExpression(ifStmt.Condition),
                        thenBranch: ifStmt.ThenBranch?.Select(s => ConvertStatement(s)).ToList() ?? new List<Skriptorium.Interpreter.Statement>(),
                        elseBranch: ifStmt.ElseBranch?.Select(s => ConvertStatement(s)).ToList() ?? new List<Skriptorium.Interpreter.Statement>()
                    );
                case Skriptorium.Parsing.ReturnStatement retStmt:
                    return new Skriptorium.Interpreter.ReturnStatement(
                        returnValue: ConvertExpression(retStmt.ReturnValue)
                    );
                default:
                    throw new Exception($"Unbekannter Statement-Typ: {stmt.GetType().Name} bei Zeile {stmt.Line}, Spalte {stmt.Column}");
            }
        }

        // Hilfsfunktion zum Konvertieren von Expressions
        private Skriptorium.Interpreter.Expression ConvertExpression(Skriptorium.Parsing.Expression expr)
        {
            if (expr == null)
                return null;

            switch (expr)
            {
                case Skriptorium.Parsing.LiteralExpression lit:
                    return new Skriptorium.Interpreter.LiteralExpression(
                        value: lit.Value
                    );
                case Skriptorium.Parsing.VariableExpression varExpr:
                    return new Skriptorium.Interpreter.VariableExpression(
                        name: varExpr.Name,
                        typeName: "unknown" // Kein TypeName in Parsing.VariableExpression, Standardwert
                    );
                case Skriptorium.Parsing.IndexExpression idx:
                    return new Skriptorium.Interpreter.IndexExpression
                    {
                        // Anpassung erforderlich, wenn IndexExpression im Interpreter erweitert wird
                    };
                case Skriptorium.Parsing.BinaryExpression bin:
                    return new Skriptorium.Interpreter.BinaryExpression(
                        left: ConvertExpression(bin.Left),
                        op: bin.Operator,
                        right: ConvertExpression(bin.Right)
                    );
                case Skriptorium.Parsing.FunctionCallExpression call:
                    return new Skriptorium.Interpreter.FunctionCallExpression(
                        functionName: call.FunctionName,
                        arguments: call.Arguments?.Select(a => ConvertExpression(a)).ToList() ?? new List<Skriptorium.Interpreter.Expression>()
                    );
                default:
                    throw new Exception($"Unbekannter Expression-Typ: {expr.GetType().Name} bei Zeile {expr.Line}, Spalte {expr.Column}");
            }
        }

        // Hilfsfunktion zum Konvertieren von Assignments
        private Skriptorium.Interpreter.Assignment ConvertAssignment(Skriptorium.Parsing.Assignment assign)
        {
            return new Skriptorium.Interpreter.Assignment(
                left: ConvertExpression(assign.Left),
                right: ConvertExpression(assign.Right),
                line: assign.Line,
                column: assign.Column
            );
        }
    }

    public class TextMarker : TextSegment
    {
        public TextMarker(IDocument document, int startOffset, int length)
        {
            StartOffset = startOffset;
            Length = length;
        }

        public Color BackgroundColor { get; set; } = Colors.Yellow;
        public Color ForegroundColor { get; set; } = Colors.Black;
    }

    public class TextMarkerRenderer : IBackgroundRenderer
    {
        private readonly TextView _textView;
        private readonly TextSegmentCollection<TextMarker> _markers;

        public TextMarkerRenderer(TextView textView, TextSegmentCollection<TextMarker> markers)
        {
            _textView = textView;
            _markers = markers;
        }

        public KnownLayer Layer => KnownLayer.Selection;

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (!_textView.VisualLinesValid) return;
            foreach (var marker in _markers)
            {
                foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, marker))
                {
                    var brush = new SolidColorBrush(marker.BackgroundColor) { Opacity = 0.4 };
                    drawingContext.DrawRectangle(brush, null, rect);
                }
            }
        }
    }
}