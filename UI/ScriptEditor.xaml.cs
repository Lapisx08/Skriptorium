using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using Skriptorium.Interpreter;
using Skriptorium.Managers;
using Skriptorium.Parsing;
using Skriptorium.UI.Views;
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
                errors.Add(ex.Message); // Direkt ex.Message verwenden
                return errors;
            }

            // --- Semantik-Check ---
            var tokens2 = new DaedalusLexer()
                .Tokenize(avalonEditor.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None));
            var decls = new DaedalusParser(tokens2).ParseScript();

            _interpreter = new DaedalusInterpreter();
            _interpreter.LoadDeclarations(decls);
            var semErr = _interpreter.SemanticCheck();

            if (semErr != null && semErr.Any())
            {
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

            return errors;
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
