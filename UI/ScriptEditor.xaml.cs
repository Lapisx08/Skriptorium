using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using Skriptorium.UI.Views;

namespace Skriptorium.UI
{
    public partial class ScriptEditor : UserControl
    {
        private string _originalText = "";
        private bool _suppressChangeTracking = false;
        private string _filePath = "";

        // Highlighting-Marker
        private TextSegmentCollection<TextMarker>? _markers;
        private TextMarkerRenderer? _markerRenderer;

        public ScriptEditor()
        {
            InitializeComponent();
            avalonEditor.TextChanged += AvalonEditor_TextChanged;

            // Marker-Dienste initialisieren
            _markers = new TextSegmentCollection<TextMarker>(avalonEditor.Document);
            _markerRenderer = new TextMarkerRenderer(avalonEditor.TextArea.TextView, _markers);
            avalonEditor.TextArea.TextView.BackgroundRenderers.Add(_markerRenderer);
        }

        private void AvalonEditor_TextChanged(object? sender, EventArgs e)
        {
            if (_suppressChangeTracking)
                return;

            IsModified = avalonEditor.Text != _originalText;
            TextChanged?.Invoke(this, null);
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

        /// <summary>
        /// Markiert alle Vorkommen eines Suchbegriffs im Text.
        /// </summary>
        /// <param name="searchText">Suchbegriff</param>
        /// <param name="matchCase">Groß-/Kleinschreibung beachten</param>
        /// <param name="wholeWord">Nur ganzes Wort markieren</param>
        /// <param name="restrictToSelection">Nur markierter Text einschränken</param>
        /// <param name="selectionStart">Start-Offset der Auswahl</param>
        /// <param name="selectionLength">Länge der Auswahl</param>
        public void HighlightAllOccurrences(string searchText, bool matchCase = false, bool wholeWord = false, bool restrictToSelection = false, int selectionStart = 0, int selectionLength = 0)
        {
            if (string.IsNullOrWhiteSpace(searchText) || _markers == null || _markerRenderer == null)
                return;

            // Alte Marker entfernen
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

        /// <summary>
        /// Entfernt alle Hervorhebungen im Editor.
        /// </summary>
        public void ClearHighlighting()
        {
            if (_markers == null || _markerRenderer == null)
                return;

            foreach (var marker in _markers.ToList())
                _markers.Remove(marker);

            avalonEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Selection);
            avalonEditor.TextArea.TextView.InvalidateVisual();
        }
    }

    // Marker-Datenstruktur
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

    // Zeichnet Marker im Editor
    public class TextMarkerRenderer : IBackgroundRenderer
    {
        private readonly TextView _textView;
        private readonly TextSegmentCollection<TextMarker> _markers;

        public TextMarkerRenderer(TextView textView, TextSegmentCollection<TextMarker> markers)
        {
            _textView = textView;
            _markers = markers;
        }

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (!_textView.VisualLinesValid)
                return;

            foreach (var marker in _markers)
            {
                foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, marker))
                {
                    var brush = new SolidColorBrush(marker.BackgroundColor) { Opacity = 0.4 };
                    drawingContext.DrawRectangle(brush, null, rect);
                }
            }
        }

        public KnownLayer Layer => KnownLayer.Selection;
    }
}