using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Rendering;
using Skriptorium.Formatting;
using Skriptorium.Interpreter;
using Skriptorium.Managers;
using Skriptorium.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Skriptorium.UI
{
    public class SyntaxColorizingTransformer : DocumentColorizingTransformer
    {
        private List<DaedalusToken> _tokens = new();

        public void UpdateTokens(List<DaedalusToken> tokens)
        {
            _tokens = tokens ?? new List<DaedalusToken>();
            Console.WriteLine($"Debug: Anzahl der Tokens (gesamt): {_tokens.Count}");
            foreach (var token in _tokens)
            {
                Console.WriteLine($"Debug: Token: {token.Type}, Value: {token.Value}, Line: {token.Line}, Column: {token.Column}");
            }
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            var document = CurrentContext.Document;
            if (document == null)
            {
                Console.WriteLine("Debug: Document ist null in ColorizeLine");
                return;
            }

            var text = document.GetText(line);
            Console.WriteLine($"Debug: Verarbeite Zeile {line.LineNumber}: {text}");

            var tokens = _tokens.Where(t => t.Line == line.LineNumber);
            Console.WriteLine($"Debug: Anzahl der Tokens in Zeile {line.LineNumber}: {tokens.Count()}");

            foreach (var token in tokens)
            {
                Console.WriteLine($"Debug: Token: {token.Type}, Value: {token.Value}, Line: {token.Line}, Column: {token.Column}");
                if (token.Type == TokenType.Whitespace || token.Type == TokenType.EOF) continue;

                var (_, wpfColor) = SyntaxHighlighting.GetColorForToken(token);

                try
                {
                    int startOffset = document.GetOffset(token.Line, token.Column);
                    int endOffset = startOffset + token.Value.Length;

                    Console.WriteLine($"Debug: Coloring Token '{token.Value}' at offset {startOffset} to {endOffset} with color {wpfColor}");

                    if (startOffset >= line.Offset && endOffset <= line.EndOffset)
                    {
                        ChangeLinePart(startOffset, endOffset, element =>
                        {
                            element.TextRunProperties.SetForegroundBrush(new SolidColorBrush(wpfColor));
                        });
                    }
                    else
                    {
                        Console.WriteLine($"Debug: Token außerhalb der Zeilengrenzen: startOffset={startOffset}, endOffset={endOffset}, line.Offset={line.Offset}, line.EndOffset={line.EndOffset}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Debug: Fehler beim Anwenden der Farbe für Token '{token.Value}': {ex.Message}");
                }
            }
        }
    }

    public partial class ScriptEditor : UserControl
    {
        private string _originalText = "";
        private string _lastText = "";
        private bool _suppressChangeTracking = false;
        private string _filePath = "";
        private List<DaedalusToken> _cachedTokens = new();

        private TextSegmentCollection<TextMarker>? _markers;
        private TextMarkerRenderer? _markerRenderer;
        private SyntaxColorizingTransformer? _colorizer;
        private BookmarkManager? _bookmarkManager;
        private DaedalusInterpreter? _interpreter;
        private bool _syntaxHighlightingEnabled = true;

        private FoldingManager? _foldingManager;
        private BraceFoldingStrategy _foldingStrategy;

        public ScriptEditor()
        {
            InitializeComponent();
            ApplyCaretBrushFromTheme();
            avalonEditor.TextChanged += AvalonEditor_TextChanged;
            avalonEditor.TextArea.Caret.PositionChanged += AvalonEditor_CaretPositionChanged;
            avalonEditor.TextArea.TextEntering += TextArea_TextEntering;


            if (avalonEditor.Document == null)
            {
                Console.WriteLine("Fehler: avalonEditor.Document ist null");
                return;
            }

            _markers = new TextSegmentCollection<TextMarker>(avalonEditor.Document);
            _markerRenderer = new TextMarkerRenderer(avalonEditor.TextArea.TextView, _markers);
            avalonEditor.TextArea.TextView.BackgroundRenderers.Add(_markerRenderer);

            _colorizer = new SyntaxColorizingTransformer();
            avalonEditor.TextArea.TextView.LineTransformers.Add(_colorizer);

            _bookmarkManager = new BookmarkManager(avalonEditor);

            _foldingManager = FoldingManager.Install(avalonEditor.TextArea);
            _foldingStrategy = new BraceFoldingStrategy();
            UpdateFoldings();

        }

        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            base.OnVisualParentChanged(oldParent);
            if (oldParent != null && _foldingManager != null)
            {
                FoldingManager.Uninstall(_foldingManager);
                _foldingManager = null;
            }
        }

        private void ApplyCaretBrushFromTheme()
        {
            // Versuche EditorCaretColor-Ressource zu holen
            if (Application.Current.Resources["EditorCaretColor"] is Color caretColor)
            {
                avalonEditor.TextArea.Caret.CaretBrush = new SolidColorBrush(caretColor);
            }
            else
            {
                // Fallback z. B. Schwarz
                avalonEditor.TextArea.Caret.CaretBrush = Brushes.Black;
            }
        }

        private void AvalonEditor_TextChanged(object? sender, EventArgs e)
        {
            if (_suppressChangeTracking)
                return;

            IsModified = avalonEditor.Text != _originalText;
            TextChanged?.Invoke(this, null);
            ApplySyntaxHighlighting();
            UpdateFoldings();
        }

        // Methode zum Aktualisieren der Faltungen
        private void UpdateFoldings()
        {
            if (_foldingManager == null || avalonEditor.Document == null)
                return;

            var foldings = _foldingStrategy.CreateNewFoldings(avalonEditor.Document);
            _foldingManager.UpdateFoldings(foldings, -1); // -1 behält die aktuell geöffneten Faltungen bei
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
                UpdateFoldings();
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
            ApplySyntaxHighlighting();
            UpdateFoldings();
        }

        public void SetTextAndMarkAsModified(string text)
        {
            _suppressChangeTracking = true;
            avalonEditor.Text = text;
            _suppressChangeTracking = false;
            IsModified = true;
            TextChanged?.Invoke(this, null);
            ClearHighlighting();
            ApplySyntaxHighlighting();
            UpdateFoldings();
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
                if (m.BackgroundColor == Colors.Yellow)
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

        private void ApplySyntaxHighlighting()
        {
            if (!_syntaxHighlightingEnabled) return;

            if (_markers == null || _markerRenderer == null || _colorizer == null) return;

            foreach (var m in _markers.ToList())
                if (m.BackgroundColor != Colors.Red && m.BackgroundColor != Colors.Yellow)
                    _markers.Remove(m);

            if (avalonEditor.Text != _lastText)
            {
                var lines = avalonEditor.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                _cachedTokens = new DaedalusLexer().Tokenize(lines);
                _lastText = avalonEditor.Text;
            }

            _colorizer.UpdateTokens(_cachedTokens);

            avalonEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Selection);
            avalonEditor.TextArea.TextView.InvalidateVisual();
        }

        [GeneratedRegex(@"Zeile\s+(\d+),\s*Spalte\s+(\d+)")]
        private static partial Regex ErrorPositionRegex();

        public List<string> CheckAll()
        {
            ClearHighlighting();
            ApplySyntaxHighlighting();
            var errors = new List<string>();

            try
            {
                var tokens = _cachedTokens;
                new DaedalusParser(tokens).ParseScript();
            }
            catch (ParseException ex)
            {
                HighlightError(ex.Line, ex.Column, ex.Found?.Length ?? 1);
                errors.Add(ex.Message);
                return errors;
            }

            try
            {
                var parsingDecls = new DaedalusParser(_cachedTokens).ParseScript();
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
                Console.WriteLine($"Ausnahme abgefangen: {ex.Message}");
                errors.Add(ex.Message);
                var lines = ex.Message.Split(new[] { "\n" }, StringSplitOptions.None);
                foreach (var msg in lines)
                {
                    Console.WriteLine($"Verarbeite Zeile: {msg}");
                    var m = ErrorPositionRegex().Match(msg);
                    if (m.Success)
                    {
                        int line = int.Parse(m.Groups[1].Value);
                        int column = int.Parse(m.Groups[2].Value);
                        Console.WriteLine($"Markiere Fehler bei Zeile {line}, Spalte {column}");
                        HighlightError(line, column, 1);
                    }
                    else
                    {
                        Console.WriteLine("Kein Regex-Match gefunden");
                    }
                }
            }

            return errors;
        }

        private void AvalonEditor_CaretPositionChanged(object sender, EventArgs e)
        {
            CaretPositionChanged?.Invoke(this, e);
        }

        public event EventHandler? CaretPositionChanged;

        public void ToggleSyntaxHighlighting()
        {
            _syntaxHighlightingEnabled = !_syntaxHighlightingEnabled;

            if (_syntaxHighlightingEnabled)
            {
                if (!avalonEditor.TextArea.TextView.LineTransformers.Contains(_colorizer))
                {
                    avalonEditor.TextArea.TextView.LineTransformers.Add(_colorizer);
                }

                ApplySyntaxHighlighting();
            }
            else
            {
                avalonEditor.TextArea.TextView.LineTransformers.Remove(_colorizer);
                avalonEditor.TextArea.TextView.InvalidateVisual();
            }
        }

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
                        var assignments = instance.Body
                            .OfType<Skriptorium.Parsing.Assignment>()
                            .Select(a => ConvertAssignment(a))
                            .ToList();
                        interpreterDecls.Add(new Skriptorium.Interpreter.InstanceDeclaration(
                            name: instance.Name,
                            baseType: instance.BaseClass,
                            assignments: assignments,
                            line: instance.Line,
                            column: instance.Column
                        ));
                        break;
                    case Skriptorium.Parsing.ClassDeclaration:
                    case Skriptorium.Parsing.PrototypeDeclaration:
                        Console.WriteLine($"Warnung: Ignoriere Deklarationstyp {decl.GetType().Name} bei Zeile {decl.Line}, Spalte {decl.Column}");
                        break;
                    default:
                        throw new Exception($"Unbekannter Deklarationstyp: {decl.GetType().Name} bei Zeile {decl.Line}, Spalte {decl.Column}");
                }
            }

            return interpreterDecls;
        }

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
                        typeName: "unknown"
                    );
                case Skriptorium.Parsing.IndexExpression:
                    return new Skriptorium.Interpreter.IndexExpression();
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

        private Skriptorium.Interpreter.Assignment ConvertAssignment(Skriptorium.Parsing.Assignment assign)
        {
            return new Skriptorium.Interpreter.Assignment(
                left: ConvertExpression(assign.Left),
                right: ConvertExpression(assign.Right),
                line: assign.Line,
                column: assign.Column
            );
        }

        public void FormatCode()
        {
            try
            {
                var tokens = _cachedTokens;
                var parser = new DaedalusParser(tokens);
                var declarations = parser.ParseScript(); // AST erzeugen

                var formatter = new Skriptorium.Formatting.DaedalusFormatter();
                string formattedCode = formatter.Format(declarations);

                SetTextAndMarkAsModified(formattedCode);
                ApplySyntaxHighlighting(); // Optional: neu einfärben
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Formatieren: {ex.Message}", "Formatierfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TextArea_TextEntering(object sender, TextCompositionEventArgs e)
        {
            var caretOffset = avalonEditor.CaretOffset;

            if (e.Text == "{")
            {
                avalonEditor.Document.Insert(caretOffset, "{}");
                avalonEditor.CaretOffset = caretOffset + 1;
                e.Handled = true;
            }
            else if (e.Text == "(")
            {
                avalonEditor.Document.Insert(caretOffset, "()");
                avalonEditor.CaretOffset = caretOffset + 1;
                e.Handled = true;
            }
            else if (e.Text == "[")
            {
                avalonEditor.Document.Insert(caretOffset, "[]");
                avalonEditor.CaretOffset = caretOffset + 1;
                e.Handled = true;
            }
            else if (e.Text == "}")
            {
                if (caretOffset < avalonEditor.Document.TextLength &&
                    avalonEditor.Document.GetCharAt(caretOffset) == '}')
                {
                    avalonEditor.CaretOffset = caretOffset + 1;
                    e.Handled = true;
                }
            }
            else if (e.Text == ")")
            {
                if (caretOffset < avalonEditor.Document.TextLength &&
                    avalonEditor.Document.GetCharAt(caretOffset) == ')')
                {
                    avalonEditor.CaretOffset = caretOffset + 1;
                    e.Handled = true;
                }
            }
            else if (e.Text == "]")
            {
                if (caretOffset < avalonEditor.Document.TextLength &&
                    avalonEditor.Document.GetCharAt(caretOffset) == ']')
                {
                    avalonEditor.CaretOffset = caretOffset + 1;
                    e.Handled = true;
                }
            }
        }
    }

    public class TextMarker : TextSegment
    {
        public TextMarker(IDocument document, int startOffset, int length)
        {
            StartOffset = startOffset;
            Length = length;
        }

        public Color BackgroundColor { get; set; } = Colors.Transparent;
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
                    if (marker.BackgroundColor != Colors.Transparent)
                    {
                        var brush = new SolidColorBrush(marker.BackgroundColor) { Opacity = 0.4 };
                        drawingContext.DrawRectangle(brush, null, rect);
                    }
                    if (marker.ForegroundColor != Colors.Black)
                    {
                        var visualLine = textView.GetVisualLineFromVisualTop(rect.Top);
                        if (visualLine != null)
                        {
                            foreach (var element in visualLine.Elements)
                            {
                                if (element.TextRunProperties != null)
                                {
                                    int elementStartOffset = visualLine.StartOffset + element.RelativeTextOffset;
                                    if (elementStartOffset >= marker.StartOffset && elementStartOffset < marker.EndOffset)
                                    {
                                        element.TextRunProperties.SetForegroundBrush(new SolidColorBrush(marker.ForegroundColor));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}