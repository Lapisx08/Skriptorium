using ControlzEx.Theming;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
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
using Skriptorium.Analysis;
using System.IO;

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

    public class CompletionData : ICompletionData
    {
        public CompletionData(string text)
        {
            Text = text;
        }

        public System.Windows.Media.ImageSource Image => null;

        public string Text { get; }

        public object Content => Text;

        public object Description => $"Vorschlag: {Text}";

        public double Priority => 0;

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionArgs)
        {
            textArea.Document.Replace(completionSegment, Text);
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
        private AutocompletionEngine? _autocompletionEngine;
        private CompletionWindow? _completionWindow;
        private bool _autocompletionEnabled = true;

        public const double OriginalFontSize = 14;

        public double Zoom { get; set; } = 1.0;

        public double EffectiveFontSize => OriginalFontSize * Zoom;

        private readonly double[] _zoomSteps = { 0.2, 0.5, 0.75, 1.0, 1.25, 1.5, 2.0, 3.0, 4.0 };

        public ScriptEditor()
        {
            InitializeComponent();
            DataContext = this;
            ApplyCaretBrushFromTheme();
            avalonEditor.TextChanged += AvalonEditor_TextChanged;
            avalonEditor.TextArea.Caret.PositionChanged += AvalonEditor_CaretPositionChanged;
            avalonEditor.TextArea.TextEntering += TextArea_TextEntering;
            avalonEditor.TextArea.TextEntered += TextArea_TextEntered;
            avalonEditor.PreviewMouseWheel += AvalonEditor_PreviewMouseWheel;

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

            avalonEditor.TextArea.PreviewMouseLeftButtonDown += AvalonEditor_PreviewMouseLeftButtonDown;
            avalonEditor.MouseHover += AvalonEditor_MouseHover;
            avalonEditor.MouseHoverStopped += AvalonEditor_MouseHoverStopped;

            InitializeAutocompletion();

            // Setze initialen globalen Zoom (falls MainWindow verfügbar)
            Loaded += (s, e) =>
            {
                if (Window.GetWindow(this) is MainWindow mainWindow)
                {
                    SetZoom(mainWindow.GlobalZoom);
                }
            };
        }

        private void InitializeAutocompletion()
        {
            _autocompletionEngine = new AutocompletionEngine();
            // Quellcode aus avalonEditor.Text in Zeilen aufteilen
            string[] lines = avalonEditor.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            _autocompletionEngine.UpdateSymbolsFromCode(lines);
        }

        public void ToggleAutocompletion()
        {
            _autocompletionEnabled = !_autocompletionEnabled;
            if (!_autocompletionEnabled && _completionWindow != null)
            {
                _completionWindow.Close();
                _completionWindow = null;
            }
            Console.WriteLine($"Autovervollständigung: {(_autocompletionEnabled ? "Aktiviert" : "Deaktiviert")}");
        }

        private void TextArea_TextEntered(object sender, TextCompositionEventArgs e)
        {
            if (!_autocompletionEnabled)
            {
                if (_completionWindow != null)
                {
                    _completionWindow.Close();
                    _completionWindow = null;
                }
                return;
            }

            if (!char.IsLetterOrDigit(e.Text[0]) && e.Text[0] != '_')
            {
                if (_completionWindow != null)
                {
                    _completionWindow.Close();
                    _completionWindow = null;
                }
                return;
            }

            if (IsInCommentOrString(avalonEditor.CaretOffset))
            {
                if (_completionWindow != null)
                {
                    _completionWindow.Close();
                    _completionWindow = null;
                }
                return;
            }

            string prefix = GetCurrentWordPrefix();
            if (string.IsNullOrEmpty(prefix) || prefix.Length < 2)
            {
                if (_completionWindow != null)
                {
                    _completionWindow.Close();
                    _completionWindow = null;
                }
                return;
            }

            var suggestions = _autocompletionEngine.GetSuggestions(prefix);
            if (!suggestions.Any())
            {
                if (_completionWindow != null)
                {
                    _completionWindow.Close();
                    _completionWindow = null;
                }
                return;
            }

            if (_completionWindow == null)
            {
                _completionWindow = new CompletionWindow(avalonEditor.TextArea);
                _completionWindow.Closed += (o, args) => _completionWindow = null;

                int wordStart = avalonEditor.CaretOffset - prefix.Length;
                _completionWindow.StartOffset = wordStart;
                _completionWindow.EndOffset = avalonEditor.CaretOffset;

                double maxWidth = suggestions.Max(s =>
                {
                    var textBlock = new TextBlock { Text = s, FontFamily = avalonEditor.FontFamily, FontSize = avalonEditor.FontSize };
                    textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    return textBlock.DesiredSize.Width;
                }) + 35;
                _completionWindow.Width = Math.Max(150, maxWidth);

                // Themenabhängige Anpassung der CompletionWindow
                bool isDark = ThemeManager.Current.DetectTheme()?.BaseColorScheme == "Dark";
                if (isDark)
                {
                    _completionWindow.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)); // Dunkler Hintergrund
                    _completionWindow.Foreground = Brushes.White; // Helle Schrift für nicht ausgewählte Einträge
                    _completionWindow.CompletionList.ListBox.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                    _completionWindow.CompletionList.ListBox.Foreground = Brushes.White;

                    // Stil für ListBoxItems anpassen
                    Style listBoxItemStyle = new Style(typeof(ListBoxItem));
                    listBoxItemStyle.Setters.Add(new Setter(ListBoxItem.ForegroundProperty, Brushes.White));
                    listBoxItemStyle.Setters.Add(new Setter(ListBoxItem.BackgroundProperty, new SolidColorBrush(Color.FromRgb(30, 30, 30))));
                    // Stil für ausgewählte Einträge
                    Trigger selectedTrigger = new Trigger
                    {
                        Property = ListBoxItem.IsSelectedProperty,
                        Value = true
                    };
                    selectedTrigger.Setters.Add(new Setter(ListBoxItem.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0, 120, 215)))); // Blaue Hervorhebung
                    selectedTrigger.Setters.Add(new Setter(ListBoxItem.ForegroundProperty, Brushes.White));
                    listBoxItemStyle.Triggers.Add(selectedTrigger);
                    _completionWindow.CompletionList.ListBox.ItemContainerStyle = listBoxItemStyle;
                }
                else
                {
                    // Standard-Stil für hellen Modus
                    _completionWindow.Background = Brushes.White;
                    _completionWindow.Foreground = Brushes.Black;
                    _completionWindow.CompletionList.ListBox.Background = Brushes.White;
                    _completionWindow.CompletionList.ListBox.Foreground = Brushes.Black;
                }

                _completionWindow.Show();
            }

            _completionWindow.CompletionList.CompletionData.Clear();
            foreach (var suggestion in suggestions)
            {
                _completionWindow.CompletionList.CompletionData.Add(new CompletionData(suggestion));
            }

            _completionWindow.CompletionList.SelectItem(prefix);
        }

        private bool IsInCommentOrString(int offset)
        {
            var currentLine = avalonEditor.Document.GetLineByOffset(offset);
            var tokens = _cachedTokens.Where(t => t.Line == currentLine.LineNumber).ToList();
            foreach (var token in tokens)
            {
                int startOffset = avalonEditor.Document.GetOffset(token.Line, token.Column);
                int endOffset = startOffset + token.Value.Length;
                if (offset >= startOffset && offset <= endOffset)
                {
                    if (token.Type == TokenType.Comment || token.Type == TokenType.CommentBlock || token.Type == TokenType.StringLiteral)
                        return true;
                }
            }
            return false;
        }

        private string GetCurrentWordPrefix()
        {
            var document = avalonEditor.Document;
            var offset = avalonEditor.CaretOffset;
            if (offset <= 0)
                return "";

            int start = offset - 1;
            while (start >= 0 && (char.IsLetterOrDigit(document.GetCharAt(start)) || document.GetCharAt(start) == '_'))
                start--;

            start++;
            if (start >= offset || (offset - start) < 2)
                return "";

            return document.GetText(start, offset - start);
        }

        private void UpdateAutocompletion()
        {
            try
            {
                var parser = new DaedalusParser(_cachedTokens);
                _ast = parser.ParseScript();
                // Quellcode aus avalonEditor.Text in Zeilen aufteilen
                string[] lines = avalonEditor.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                _autocompletionEngine.UpdateSymbolsFromCode(lines);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Aktualisieren der Autovervollständigung: {ex.Message}");
            }
        }

        public void SetZoom(double zoomFactor)
        {
            Zoom = zoomFactor;
            avalonEditor.FontSize = OriginalFontSize * Zoom;
            avalonEditor.TextArea.TextView.Redraw();
        }

        private double GetNextZoomStep(bool zoomIn)
        {
            int currentIndex = Array.IndexOf(_zoomSteps, Zoom);
            if (currentIndex == -1)
            {
                currentIndex = _zoomSteps.Length - 1;
                for (int i = 0; i < _zoomSteps.Length; i++)
                {
                    if (Zoom <= _zoomSteps[i])
                    {
                        currentIndex = i;
                        break;
                    }
                }
            }

            int newIndex = zoomIn ? currentIndex + 1 : currentIndex - 1;
            newIndex = Math.Clamp(newIndex, 0, _zoomSteps.Length - 1);
            return _zoomSteps[newIndex];
        }

        private void AvalonEditor_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                bool zoomIn = e.Delta > 0;
                double newZoom = GetNextZoomStep(zoomIn);

                if (Math.Abs(newZoom - Zoom) >= double.Epsilon)
                {
                    if (Window.GetWindow(this) is MainWindow mainWindow)
                    {
                        mainWindow.SetGlobalZoom(newZoom);

                        int zoomPercent = (int)(newZoom * 100);
                        var comboBox = mainWindow.FindName("ZoomComboBox") as ComboBox;
                        if (comboBox != null)
                        {
                            switch (zoomPercent)
                            {
                                case 20: comboBox.SelectedIndex = 0; break;
                                case 50: comboBox.SelectedIndex = 1; break;
                                case 75: comboBox.SelectedIndex = 2; break;
                                case 100: comboBox.SelectedIndex = 3; break;
                                case 125: comboBox.SelectedIndex = 4; break;
                                case 150: comboBox.SelectedIndex = 5; break;
                                case 200: comboBox.SelectedIndex = 6; break;
                                case 300: comboBox.SelectedIndex = 7; break;
                                case 400: comboBox.SelectedIndex = 8; break;
                                default: comboBox.SelectedIndex = 3; break;
                            }
                        }
                    }
                }
                e.Handled = true;
            }
        }

        private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
        {
            var isDark = e.NewTheme?.BaseColorScheme == "Dark";
            var tokenColorMap = isDark
                ? DaedalusSyntaxHighlightingDarkmode.TokenColors
                : DaedalusSyntaxHighlightingLightmode.TokenColors;
            var colorMap = tokenColorMap.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.WpfColor);
            _colorizer = new SyntaxColorizingTransformer(colorMap);
            ApplySyntaxHighlightingState();
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
            UpdateAutocompletion();
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
                ? new SolidColorBrush(Colors.WhiteSmoke)
                : new SolidColorBrush(Colors.Black);
        }

        private void AvalonEditor_TextChanged(object? sender, EventArgs e)
        {
            if (_suppressChangeTracking)
                return;

            IsModified = avalonEditor.Text != _originalText;
            TextChanged?.Invoke(this, null);
            ApplySyntaxHighlighting();
            UpdateFoldings();
            UpdateAutocompletion();
        }

        private void UpdateFoldings()
        {
            if (_foldingManager == null || avalonEditor.Document == null)
                return;

            var foldings = _foldingStrategy.CreateNewFoldings(avalonEditor.Document);
            _foldingManager.UpdateFoldings(foldings, -1);
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
                UpdateAutocompletion();
            }
        }

        public void LoadFile(string path)
        {
            if (System.IO.File.Exists(path))
            {
                this.FilePath = path; // Setzt den Pfad und triggert die Logik oben

                string content = System.IO.File.ReadAllText(path, System.Text.Encoding.GetEncoding(1252));

                _suppressChangeTracking = true;
                avalonEditor.Text = content;
                _originalText = content;
                _suppressChangeTracking = false;

                DoInitialHighlighting();
            }
        }

        public void SetTextAndResetModified(string text)
        {
            _suppressChangeTracking = true;
            avalonEditor.Text = text;
            _originalText = text;
            _lastText = "";
            _suppressChangeTracking = false;
            IsModified = false;
            ClearHighlighting();
            ApplySyntaxHighlighting();
            UpdateFoldings();
            UpdateAutocompletion();
        }

        public void SetTextAndMarkAsModified(string text)
        {
            _suppressChangeTracking = true;
            avalonEditor.Text = text;
            _lastText = "";
            _suppressChangeTracking = false;
            IsModified = true;
            TextChanged?.Invoke(this, null);
            ClearHighlighting();
            ApplySyntaxHighlighting();
            UpdateFoldings();
            UpdateAutocompletion();
        }

        public void ResetModifiedFlag()
        {
            _originalText = avalonEditor.Text;
            IsModified = false;
            TextChanged?.Invoke(this, null);
        }

        public event TextChangedEventHandler? TextChanged;

        public TextEditor Avalon => avalonEditor;

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
                    UpdateAutocompletion();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Fehler beim Parsen: {ex.Message}");
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

        public void ApplySyntaxHighlightingState()
        {
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
            avalonEditor.TextArea.TextView.Redraw();
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
            avalonEditor.TextArea.TextView.Redraw();
        }

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
            else if (e.Text == "\"")
            {
                if (caretOffset < avalonEditor.Document.TextLength &&
                    avalonEditor.Document.GetCharAt(caretOffset) == '"')
                {
                    avalonEditor.CaretOffset = caretOffset + 1;
                    e.Handled = true;
                }
                else
                {
                    avalonEditor.Document.Insert(caretOffset, "\"\"");
                    avalonEditor.CaretOffset = caretOffset + 1;
                    e.Handled = true;
                }
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

        private ToolTip _toolTip = new ToolTip();

        private void AvalonEditor_MouseHover(object sender, MouseEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Control) return;

            TextView textView = avalonEditor.TextArea.TextView;
            Point pos = e.GetPosition(textView);
            var position = textView.GetPosition(pos + textView.ScrollOffset);

            if (position.HasValue)
            {
                try
                {
                    int offset = avalonEditor.Document.GetOffset(position.Value.Line, position.Value.Column);
                    string hoveredWord = GetWholeWordAtOffset(avalonEditor.Document, offset);

                    if (!string.IsNullOrWhiteSpace(hoveredWord))
                    {
                        var definition = ProjectManager.Instance.Compiler.GlobalSymbols.Resolve(hoveredWord);

                        if (definition != null)
                        {
                            bool isSelf = string.Equals(definition.FilePath, this.FilePath, StringComparison.OrdinalIgnoreCase)
                                          && definition.Line == position.Value.Line;

                            if (isSelf)
                            {
                                return; // Wenn es die eigene Definition ist, Tooltip gar nicht erst zeigen
                            }

                            // ToolTip anzeigen (wenn es nicht die eigene Stelle ist)
                            _toolTip.Content = $"Definition gefunden:\n{Path.GetFileName(definition.FilePath)} (Zeile {definition.Line})";
                            _toolTip.PlacementTarget = avalonEditor;
                            _toolTip.IsOpen = true;
                            e.Handled = true;
                        }
                    }
                }
                catch { /* Ignorieren */ }
            }
        }

        private void AvalonEditor_MouseHoverStopped(object sender, MouseEventArgs e)
        {
            _toolTip.IsOpen = false;
        }

        private bool _initialIndexingAttempted = false;
        private readonly object _indexingLock = new object();

        private void AvalonEditor_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Control)
                return;

            System.Diagnostics.Debug.WriteLine("Strg + Klick erkannt!");

            var pos = avalonEditor.GetPositionFromPoint(e.GetPosition(avalonEditor));
            if (!pos.HasValue)
                return;

            int offset = avalonEditor.Document.GetOffset(pos.Value.Line, pos.Value.Column);
            string clickedWord = GetWholeWordAtOffset(avalonEditor.Document, offset);

            if (string.IsNullOrWhiteSpace(clickedWord))
                return;

            System.Diagnostics.Debug.WriteLine($"Wort unter Cursor: '{clickedWord}'");

            var compiler = ProjectManager.Instance.Compiler;
            var definition = compiler.GlobalSymbols.Resolve(clickedWord);

            // Fall 1: Definition gefunden → sofort springen
            if (definition != null)
            {
                System.Diagnostics.Debug.WriteLine($"Definition gefunden in: {definition.FilePath}");
                e.Handled = true;
                JumpToDefinition(definition, clickedWord);
                return;
            }

            // Fall 2: Nicht gefunden → einmalig Indizierung anstoßen
            System.Diagnostics.Debug.WriteLine("Definition im Compiler NICHT gefunden → starte Indizierung");

            if (_initialIndexingAttempted)
            {
                // Wir haben schon versucht → mehrfaches Indizieren verhindern
                System.Diagnostics.Debug.WriteLine("Indizierung wurde bereits versucht – Symbol bleibt nicht gefunden.");
                return;
            }

            lock (_indexingLock)
            {
                if (_initialIndexingAttempted)
                    return;

                _initialIndexingAttempted = true;
            }

            // UI-Feedback geben (optional, aber sehr hilfreich)
            Dispatcher.Invoke(() =>
            {

            });

            // Asynchrone Indizierung starten
            Task.Run(() =>
            {
                try
                {
                    string? filePath = this.FilePath; // aktuelle Datei des Editors
                    string? rootPath = null;

                    if (!string.IsNullOrEmpty(filePath))
                    {
                        rootPath = FindScriptsRoot(filePath);
                    }

                    if (!string.IsNullOrEmpty(rootPath) && Directory.Exists(rootPath))
                    {
                        // Variante A: ganzes Projekt indizieren (empfohlen, wenn es nicht zu groß ist)
                        System.Diagnostics.Debug.WriteLine($"Indiziere komplettes Projekt: {rootPath}");
                        ProjectManager.Instance.LoadProject(rootPath);
                    }
                    else if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                    {
                        // Variante B: nur die aktuelle Datei (schneller, aber unvollständig)
                        System.Diagnostics.Debug.WriteLine($"Indiziere nur aktuelle Datei: {filePath}");
                        ProjectManager.Instance.RefreshSingleFile(filePath);
                    }
                    else
                    {
                        // Fallback: Pfad aus den Einstellungen nehmen
                        string? savedPath = Properties.Settings.Default.ScriptSearchPath;
                        if (!string.IsNullOrEmpty(savedPath) && Directory.Exists(savedPath))
                        {
                            System.Diagnostics.Debug.WriteLine($"Indiziere aus gespeichertem Pfad: {savedPath}");
                            ProjectManager.Instance.LoadProject(savedPath);
                        }
                    }

                    // Nach dem Indizieren nochmal versuchen
                    definition = compiler.GlobalSymbols.Resolve(clickedWord);

                    if (definition != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Nach Indizierung gefunden: {definition.FilePath}");
                        Dispatcher.Invoke(() =>
                        {
                            JumpToDefinition(definition, clickedWord);
                            // Optional: Status zurücksetzen
                            // StatusPositionText.Text = "";
                        });
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Symbol auch nach Indizierung nicht gefunden.");
                        Dispatcher.Invoke(() =>
                        {
                            // Optional: Hinweis anzeigen
                            // MessageBox.Show($"Symbol '{clickedWord}' wurde nicht gefunden.", "Nicht gefunden", MessageBoxButton.OK, MessageBoxImage.Information);
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Fehler beim Indizieren: {ex.Message}");
                    Dispatcher.Invoke(() =>
                    {
                        // Status oder Meldung
                        // StatusPositionText.Text = "Indizierung fehlgeschlagen";
                    });
                }
            });
        }

        private string? FindScriptsRoot(string path)
        {
            DirectoryInfo? current = new DirectoryInfo(path);
            while (current != null)
            {
                if (current.Name.Equals("Scripts", StringComparison.OrdinalIgnoreCase))
                {
                    return current.FullName;
                }
                current = current.Parent;
            }
            return Path.GetDirectoryName(path);
        }

        private string GetWholeWordAtOffset(TextDocument document, int offset)
        {
            if (offset < 0 || offset >= document.TextLength) return string.Empty;

            int start = offset;
            while (start > 0 && IsDaedalusIdentifierChar(document.GetCharAt(start - 1)))
                start--;

            int end = offset;
            while (end < document.TextLength && IsDaedalusIdentifierChar(document.GetCharAt(end)))
                end++;

            if (start == end) return string.Empty;
            return document.GetText(start, end - start);
        }

        private bool IsDaedalusIdentifierChar(char c) => char.IsLetterOrDigit(c) || c == '_';

        private void JumpToDefinition(Declaration definition, string symbolName)
        {
            if (Application.Current.MainWindow is MainWindow mainWin)
            {
                mainWin.OpenAndNavigateTo(
                    definition.FilePath,
                    definition.Line,
                    definition.Column,
                    symbolName
                );
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