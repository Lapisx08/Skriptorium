using ControlzEx.Theming;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Rendering;
using MahApps.Metro;
using Skriptorium.Formatting;
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
        private readonly Dictionary<TokenType, Color> _tokenColors;
        private List<DaedalusToken> _tokens = new();

        public SyntaxColorizingTransformer(Dictionary<TokenType, Color> tokenColors)
        {
            _tokenColors = tokenColors ?? throw new ArgumentNullException(nameof(tokenColors));
        }

        public void UpdateTokens(List<DaedalusToken> tokens)
        {
            _tokens = tokens ?? new List<DaedalusToken>();
            Console.WriteLine($"Debug: Anzahl der Tokens (gesamt): {_tokens.Count}");
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            var document = CurrentContext.Document;
            if (document == null)
                return;

            var tokens = _tokens.Where(t => t.Line == line.LineNumber).ToList();

            foreach (var token in tokens)
            {
                if (token.Type == TokenType.Whitespace || token.Type == TokenType.EOF) continue;

                if (_tokenColors.TryGetValue(token.Type, out var wpfColor))
                {
                    int startOffset = document.GetOffset(token.Line, token.Column);
                    int endOffset = startOffset + token.Value.Length;

                    if (startOffset >= line.Offset && endOffset <= line.EndOffset)
                    {
                        ChangeLinePart(startOffset, endOffset, element =>
                        {
                            element.TextRunProperties.SetForegroundBrush(new SolidColorBrush(wpfColor));
                        });
                    }
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
        private SyntaxColorizingTransformer? _colorizer = null!;
        private BookmarkManager? _bookmarkManager;
        private bool _syntaxHighlightingEnabled = true;

        private FoldingManager? _foldingManager;
        private BraceFoldingStrategy _foldingStrategy;
        private bool _allFolded = false;

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

            var isDark = ThemeManager.Current.DetectTheme()?.BaseColorScheme == "Dark";
            var tokenColorMap = isDark
                 ? DaedalusSyntaxHighlightingDarkmode.TokenColors
                 : DaedalusSyntaxHighlightingLightmode.TokenColors;
            var colorMap = tokenColorMap.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.WpfColor);
            _colorizer = new SyntaxColorizingTransformer(colorMap);
            avalonEditor.TextArea.TextView.LineTransformers.Add(_colorizer);

            ThemeManager.Current.ThemeChanged += OnThemeChanged;
            DoInitialHighlighting();

            _bookmarkManager = new BookmarkManager(avalonEditor);

            _foldingManager = FoldingManager.Install(avalonEditor.TextArea);
            _foldingStrategy = new BraceFoldingStrategy();
            UpdateFoldings();
        }

        private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
        {
            var isDark = e.NewTheme?.BaseColorScheme == "Dark";
            var tokenColorMap = isDark
                ? DaedalusSyntaxHighlightingDarkmode.TokenColors
                : DaedalusSyntaxHighlightingLightmode.TokenColors;
            var colorMap = tokenColorMap.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.WpfColor);
            _colorizer = new SyntaxColorizingTransformer(colorMap);
            avalonEditor.TextArea.TextView.LineTransformers.Clear();
            avalonEditor.TextArea.TextView.LineTransformers.Add(_colorizer);
            _colorizer.UpdateTokens(_cachedTokens);
            ApplyCaretBrushFromTheme();
            avalonEditor.TextArea.TextView.InvalidateVisual();
        }

        private void DoInitialHighlighting()
        {
            if (string.IsNullOrEmpty(avalonEditor.Text))
            {
                _cachedTokens = new List<DaedalusToken>();
                _colorizer.UpdateTokens(_cachedTokens);
                return;
            }

            var lines = avalonEditor.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            _cachedTokens = new DaedalusLexer().Tokenize(lines);
            _colorizer.UpdateTokens(_cachedTokens);
            avalonEditor.TextArea.TextView.InvalidateVisual();
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
            var isDark = ThemeManager.Current.DetectTheme()?.BaseColorScheme == "Dark";
            avalonEditor.TextArea.Caret.CaretBrush = isDark
                ? new SolidColorBrush(Colors.WhiteSmoke) // Helle Farbe für Darkmode
                : new SolidColorBrush(Colors.Black);     // Dunkle Farbe für Lightmode
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

        public void ToggleAllFoldings()
        {
            if (_foldingManager == null || !_foldingManager.AllFoldings.Any()) return;

            bool shouldFold = !_allFolded;

            foreach (var section in _foldingManager.AllFoldings)
            {
                section.IsFolded = shouldFold;
            }

            _allFolded = shouldFold;
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
            _lastText = ""; // Erzwinge Neuverarbeitung in ApplySyntaxHighlighting
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
            _lastText = ""; // Erzwinge Neuverarbeitung in ApplySyntaxHighlighting
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

            TextChanged?.Invoke(this, null);
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

        private List<string> Errors = new List<string>();
        private List<Skriptorium.Parsing.Declaration> _ast = new List<Skriptorium.Parsing.Declaration>();

        private void ApplySyntaxHighlighting()
        {
            if (!_syntaxHighlightingEnabled)
                return;

            if (_markers == null || _markerRenderer == null || _colorizer == null)
                return;

            foreach (var m in _markers.ToList())
                if (m.BackgroundColor != Colors.Red && m.BackgroundColor != Colors.Yellow)
                    _markers.Remove(m);

            if (avalonEditor.Text != _lastText)
            {
                var lines = avalonEditor.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                var lexer = new DaedalusLexer();
                var tokens = lexer.Tokenize(lines);
                _cachedTokens = tokens;
                _colorizer.UpdateTokens(_cachedTokens);

                try
                {
                    var parser = new DaedalusParser(tokens);
                    var parsedDeclarations = parser.ParseScript();
                    Errors.Clear();
                    _ast = parsedDeclarations;
                }
                catch (Exception ex)
                {
                    // Optional: Fehler für spätere Nutzung speichern oder ignorieren
                }

                _lastText = avalonEditor.Text;
            }
            else
            {
                _colorizer.UpdateTokens(_cachedTokens);
            }

            avalonEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Background);
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

            avalonEditor.TextArea.TextView.LineTransformers.Clear();

            if (_syntaxHighlightingEnabled)
            {
                avalonEditor.TextArea.TextView.LineTransformers.Add(_colorizer);
                ApplySyntaxHighlighting();
            }
            else
            {
                var isDark = ThemeManager.Current.DetectTheme()?.BaseColorScheme == "Dark";
                var defaultColor = isDark ? Colors.WhiteSmoke : Colors.Black;

                avalonEditor.TextArea.TextView.LineTransformers.Add(new DefaultColorLineTransformer(defaultColor));
            }

            avalonEditor.TextArea.TextView.InvalidateVisual();
            avalonEditor.TextArea.TextView.Redraw();  // explizites Neuzeichnen erzwingen
        }

        // Neue Klasse für den Default-Color-LineTransformer
        public class DefaultColorLineTransformer : DocumentColorizingTransformer
        {
            private readonly Color _defaultColor;

            public DefaultColorLineTransformer(Color defaultColor)
            {
                _defaultColor = defaultColor;
            }

            protected override void ColorizeLine(DocumentLine line)
            {
                ChangeLinePart(line.Offset, line.EndOffset, element =>
                {
                    element.TextRunProperties.SetForegroundBrush(new SolidColorBrush(_defaultColor));
                });
            }
        }

        public void FormatCode()
        {
            var document = Avalon.Document;
            var originalText = document.Text;

            var formatter = new DaedalusFormatter();
            var formattedText = formatter.Format(originalText);

            if (formattedText != originalText)
            {
                using (document.RunUpdate())
                {
                    document.Replace(0, document.TextLength, formattedText);
                }
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