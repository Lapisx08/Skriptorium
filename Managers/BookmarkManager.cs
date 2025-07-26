using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Skriptorium.Managers
{
    public class BookmarkManager
    {
        private readonly TextEditor _editor;
        private readonly List<Bookmark> _bookmarks = new();
        private readonly BookmarkMargin _bookmarkMargin;

        public BookmarkManager(TextEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));

            // BookmarkMargin hinzufügen
            _bookmarkMargin = new BookmarkMargin(_bookmarks);
            _editor.TextArea.LeftMargins.Add(_bookmarkMargin);

            // Event für Klick auf Zeilennummer registrieren
            var lineNumberMargin = _editor.TextArea.LeftMargins.OfType<LineNumberMargin>().FirstOrDefault();
            if (lineNumberMargin != null)
            {
                lineNumberMargin.MouseLeftButtonDown += LineNumberMargin_MouseLeftButtonDown;
            }

            // Beim Scrollen und bei VisualLines-Änderungen neu zeichnen
            _editor.TextArea.TextView.VisualLinesChanged += (s, e) => RefreshRendering();
            _editor.TextArea.TextView.ScrollOffsetChanged += (s, e) => RefreshRendering();
        }

        private void LineNumberMargin_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var view = _editor.TextArea.TextView;
            view.EnsureVisualLines();
            var pos = e.GetPosition(view);
            var visualLine = view.GetVisualLineFromVisualTop(pos.Y);
            if (visualLine == null) return;

            var line = visualLine.FirstDocumentLine;
            ToggleBookmark(line.LineNumber);
        }

        public void ToggleBookmark(int lineNumber)
        {
            var existing = _bookmarks.FirstOrDefault(b => b.LineNumber == lineNumber);
            if (existing != null)
            {
                _bookmarks.Remove(existing);
            }
            else
            {
                _bookmarks.Add(new Bookmark(_editor.Document, lineNumber));
            }

            RefreshRendering();
        }

        public void ClearAll()
        {
            _bookmarks.Clear();
            RefreshRendering();
        }

        public void GotoNext()
        {
            if (_bookmarks.Count == 0) return;

            int currentLine = _editor.Document.GetLineByOffset(_editor.CaretOffset).LineNumber;

            var next = _bookmarks
                .Where(b => b.LineNumber > currentLine)
                .OrderBy(b => b.LineNumber)
                .FirstOrDefault()
                ?? _bookmarks.OrderBy(b => b.LineNumber).First();

            JumpToLine(next.LineNumber);
        }

        public void GotoPrevious()
        {
            if (_bookmarks.Count == 0) return;

            int currentLine = _editor.Document.GetLineByOffset(_editor.CaretOffset).LineNumber;

            var prev = _bookmarks
                .Where(b => b.LineNumber < currentLine)
                .OrderByDescending(b => b.LineNumber)
                .FirstOrDefault()
                ?? _bookmarks.OrderByDescending(b => b.LineNumber).First();

            JumpToLine(prev.LineNumber);
        }

        private void JumpToLine(int lineNumber)
        {
            _editor.ScrollTo(lineNumber, 0);
            _editor.Select(_editor.Document.GetOffset(lineNumber, 0), 0);
        }

        private void RefreshRendering()
        {
            _bookmarkMargin.InvalidateVisual();
        }
    }

    public class Bookmark : TextSegment
    {
        public int LineNumber { get; }

        public Bookmark(IDocument document, int lineNumber)
        {
            Document = document;
            LineNumber = lineNumber;
            StartOffset = document.GetOffset(lineNumber, 0);
            Length = 1;
        }

        public IDocument Document { get; }
    }

    public class BookmarkMargin : AbstractMargin
    {
        private readonly List<Bookmark> _bookmarks;

        public BookmarkMargin(List<Bookmark> bookmarks)
        {
            _bookmarks = bookmarks ?? throw new ArgumentNullException(nameof(bookmarks));
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (TextView != null)
            {
                // Höhe an sichtbaren Textbereich anpassen
                return new Size(5, TextView.ActualHeight);
            }
            return new Size(5, 0);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (TextView != null)
            {
                // Höhe an sichtbaren Textbereich anpassen
                return new Size(5, TextView.ActualHeight);
            }
            return base.ArrangeOverride(finalSize);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (_bookmarks.Count == 0 || TextView == null || !TextView.VisualLinesValid)
                return;

            var foreground = Brushes.OrangeRed;

            foreach (var bookmark in _bookmarks)
            {
                var visualLine = TextView.GetVisualLine(bookmark.LineNumber);
                if (visualLine == null)
                    continue; // Zeile nicht sichtbar

                var rect = BackgroundGeometryBuilder.GetRectsForSegment(TextView, visualLine.FirstDocumentLine).FirstOrDefault();
                if (rect == null)
                    continue;

                var bookmarkRect = new Rect(0, rect.Top, 5, rect.Height);
                drawingContext.DrawRectangle(foreground, null, bookmarkRect);
            }
        }
    }
}
