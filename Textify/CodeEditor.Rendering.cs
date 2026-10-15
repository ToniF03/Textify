using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace Textify
{
    public sealed partial class CodeEditor
    {
        private sealed class EditorSurface : FrameworkElement
        {
            private static readonly Brush GutterSeparator = FrozenBrush(Color.FromRgb(226, 230, 235));
            private static readonly Brush SelectionBackground = FrozenBrush(Color.FromRgb(194, 220, 247));
            private static readonly Brush BracketBackground = FrozenBrush(Color.FromRgb(255, 217, 128));
            private static readonly Brush CaretForeground = FrozenBrush(Color.FromRgb(45, 111, 176));
            private static readonly Brush FoldPlaceholderForeground = FrozenBrush(Color.FromRgb(125, 135, 146));
            private static readonly Brush FoldMarkerBackground = FrozenBrush(Color.FromRgb(249, 250, 252));
            private static readonly Pen ChevronPen = FrozenPen(Color.FromRgb(112, 122, 133));
            private static readonly Pen FoldMarkerPen = FrozenPen(Color.FromRgb(190, 198, 207));
            private static readonly Pen SelectedFoldMarkerPen = FrozenPen(Color.FromRgb(112, 143, 173));
            private readonly CodeEditor _editor;
            public EditorSurface(CodeEditor editor)
            {
                _editor = editor;
                Focusable = true;
                Cursor = System.Windows.Input.Cursors.IBeam;
                SnapsToDevicePixels = true;
            }

            private static Brush FrozenBrush(Color color)
            {
                var brush = new SolidColorBrush(color);
                brush.Freeze();
                return brush;
            }

            private static Pen FrozenPen(Color color)
            {
                var pen = new Pen(new SolidColorBrush(color), 1.4);
                pen.StartLineCap = PenLineCap.Round;
                pen.EndLineCap = PenLineCap.Round;
                pen.Freeze();
                return pen;
            }

            protected override Size MeasureOverride(Size availableSize)
            {
                var lines = _editor.Lines;
                double maxWidth = 0;
                if (!_editor.IsWordWrapEnabled)
                {
                    for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
                    {
                        double lineWidth = _editor.MeasureTextWidth(lines[lineIndex].TrimEnd('\r'));
                        if (_editor.IsFoldCollapsedAtLine(lineIndex))
                            lineWidth += _editor.MeasureTextWidth("  …") + CodeEditor.FoldMarkerPadding;
                        maxWidth = Math.Max(maxWidth, lineWidth);
                    }
                }
                double width = _editor.IsWordWrapEnabled
                    ? (!double.IsInfinity(availableSize.Width) ? availableSize.Width : Math.Max(_editor.TextOrigin + 20, _editor.ActualWidth))
                    : _editor.TextOrigin + maxWidth + _editor.MeasureTextWidth("  ") + 10;
                double height = TopInset * 2 + Math.Max(1, _editor.VisibleLineCount) * _editor.LineHeight;
                // ScrollViewer measures its content with Infinity along scrollable axes.
                // DesiredSize must stay finite; use the text's natural extent in that case.
                if (!double.IsInfinity(availableSize.Width) && !double.IsNaN(availableSize.Width))
                    width = Math.Max(width, availableSize.Width);
                if (!double.IsInfinity(availableSize.Height) && !double.IsNaN(availableSize.Height))
                    height = Math.Max(height, availableSize.Height);
                return new Size(width, height);
            }

            protected override void OnRender(DrawingContext dc)
            {
                base.OnRender(dc);
                dc.DrawRectangle(_editor.Background, null, new Rect(0, 0, ActualWidth, ActualHeight));
                if (_editor.GutterVisible)
                {
                    dc.DrawRectangle(_editor.LineNumberBackground, null,
                        new Rect(0, 0, _editor.TextOrigin, ActualHeight));
                    dc.DrawRectangle(GutterSeparator, null,
                        new Rect(_editor.TextOrigin - 1, 0, 1, ActualHeight));
                }

                string[] lines = _editor.Lines;
                double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
                var typeface = new Typeface(_editor.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
                int matchingBracket = _editor.EnableBracketHighlighting ? _editor.FindMatchingBracketPosition() : -1;
                int bracketCandidate = _editor.EnableBracketHighlighting && _editor.Caret < _editor.Text.Length && CodeEditor.IsBracket(_editor.Text[_editor.Caret])
                    ? _editor.Caret : (_editor.EnableBracketHighlighting ? _editor.Caret - 1 : -1);
                int firstRow = _editor.FirstVisibleLine;
                int lastRow = Math.Max(firstRow + 1, _editor.VisibleLineEnd);

                for (int row = firstRow; row < lastRow; row++)
                {
                    int documentLine = _editor.DocumentLineAtVisibleRow(row);
                    int offset = _editor.GetLineStart(documentLine);
                    string fullLine = lines[documentLine].TrimEnd('\r');
                    int segmentStart = _editor.VisibleRowStartColumn(row);
                    int segmentLength = Math.Min(_editor.VisibleRowLength(row), Math.Max(0, fullLine.Length - segmentStart));
                    string line = fullLine.Substring(segmentStart, segmentLength);
                    double y = TopInset + row * _editor.LineHeight;
                    bool firstSegment = segmentStart == 0;
                    bool lastSegment = row + 1 >= _editor.VisibleLineCount ||
                        _editor.DocumentLineAtVisibleRow(row + 1) != documentLine;
                    if (_editor.HighlightCurrentLine && _editor.LineIndexAt(_editor.Caret) == documentLine)
                        dc.DrawRectangle(_editor.CurrentLineBackground, null, new Rect(0, y, ActualWidth, _editor.LineHeight));
                    if (_editor.IsWholeLineSelected(documentLine))
                        dc.DrawRectangle(SelectionBackground, null, new Rect(0, y, ActualWidth, _editor.LineHeight));
                    Brush lineBrush = _editor.Foreground;

                    if (firstSegment)
                    {
                        if (_editor.ShowLineNumbers)
                        {
                            var label = MakeText((documentLine + 1).ToString(CultureInfo.InvariantCulture), typeface,
                                _editor.LineNumberForeground, dpi);
                            dc.DrawText(label, new Point(_editor.TextOrigin - 22 - label.Width, y));
                        }
                        if (_editor.CanFoldAtLine(documentLine))
                            DrawChevron(dc, _editor.TextOrigin - 8, y + _editor.LineHeight / 2,
                                _editor.IsFoldCollapsedAtLine(documentLine));
                    }

                    int visibleLength = line.Length;
                    int lineEnd = offset + fullLine.Length;
                    bool isFoldCollapsed = _editor.IsFoldCollapsedAtLine(documentLine);
                    double markerX = _editor.TextOrigin + _editor.MeasureTextWidth(line + "  ");
                    double markerWidth = _editor.MeasureTextWidth("…");
                    if (firstSegment && !_editor.IsWordWrapEnabled &&
                        _editor.IsFoldHeaderLineFullySelected(documentLine, offset, lineEnd))
                    {
                        double selectionEndX = _editor.TextOrigin + _editor.MeasureTextWidth(fullLine + "  …") + CodeEditor.FoldMarkerPadding;
                        dc.DrawRectangle(SelectionBackground, null,
                            new Rect(_editor.TextOrigin, y, Math.Max(0, selectionEndX - _editor.TextOrigin), _editor.LineHeight));
                    }
                    foreach (var span in _editor.GetSelectionSpansForLine(documentLine))
                    {
                        int from = Math.Max(span.Start, offset + segmentStart);
                        int to = Math.Min(span.Start + span.Length, offset + segmentStart + segmentLength);
                        if (to <= from) continue;
                        int fromColumn = from - offset - segmentStart;
                        int toColumn = to - offset - segmentStart;
                        double x = _editor.TextOrigin + _editor.MeasureTextWidth(line.Substring(0, fromColumn));
                        double w = _editor.MeasureTextWidth(line.Substring(0, toColumn)) - _editor.MeasureTextWidth(line.Substring(0, fromColumn));
                        dc.DrawRectangle(SelectionBackground, null, new Rect(x, y, w, _editor.LineHeight));
                    }
                    if (matchingBracket >= offset + segmentStart && matchingBracket < offset + segmentStart + segmentLength)
                        DrawBracketHighlight(dc, line, y, matchingBracket - offset - segmentStart);
                    if (bracketCandidate >= offset + segmentStart && bracketCandidate < offset + segmentStart + segmentLength)
                        DrawBracketHighlight(dc, line, y, bracketCandidate - offset - segmentStart);
                    if (line.Length > 0)
                    {
                        var formatted = MakeText(line, typeface, lineBrush, dpi);
                        if (_editor.EnableSyntaxHighlighting)
                            _editor.ApplySyntaxHighlighting(formatted, line, documentLine);
                        dc.DrawText(formatted, new Point(_editor.TextOrigin, y));
                    }
                    if (isFoldCollapsed && lastSegment)
                    {
                        bool markerSelected = _editor.IsFoldPlaceholderSelected(documentLine);
                        var markerBox = new Rect(markerX - CodeEditor.FoldMarkerPadding, y + 2,
                            markerWidth + CodeEditor.FoldMarkerPadding * 2, _editor.LineHeight - 4);
                        dc.DrawRoundedRectangle(markerSelected ? SelectionBackground : FoldMarkerBackground,
                            markerSelected ? SelectedFoldMarkerPen : FoldMarkerPen, markerBox, 3, 3);
                        dc.DrawText(MakeText("…", typeface, FoldPlaceholderForeground, dpi), new Point(markerX, y));
                    }
                    bool caretAtPlaceholder = _editor.CaretAtFoldPlaceholder &&
                        _editor.LineIndexAt(_editor.Caret) == documentLine && _editor.IsFoldCollapsedAtLine(documentLine);
                    if (_editor.HasFocus && _editor.CaretVisible && _editor.SelectionLength == 0 &&
                        (!_editor.IsRectangularSelection || _editor.GetSelectionSpansForLine(documentLine).Count == 0) &&
                        _editor.VisibleRowForPosition(_editor.Caret) == row &&
                        _editor.LineIndexAt(_editor.Caret) == documentLine && _editor.Caret >= offset && _editor.Caret <= lineEnd)
                    {
                        int col = Math.Max(0, Math.Min(visibleLength, _editor.Caret - offset - segmentStart));
                        double cx = caretAtPlaceholder
                            ? _editor.TextOrigin + _editor.MeasureTextWidth(line + "  …")
                            : _editor.TextOrigin + _editor.MeasureTextWidth(line.Substring(0, col));
                        dc.DrawRectangle(CaretForeground, null,
                            new Rect(cx, y + 1, 1.4, _editor.LineHeight - 2));
                    }
                    foreach (int secondaryCaret in _editor.AdditionalCaretPositions)
                    {
                        if (!_editor.HasFocus) break;
                        if (_editor.VisibleRowForPosition(secondaryCaret) != row) continue;
                        int localColumn = Math.Max(0, Math.Min(visibleLength, secondaryCaret - offset - segmentStart));
                        double x = _editor.TextOrigin + _editor.MeasureTextWidth(line.Substring(0, localColumn));
                        dc.DrawRectangle(CaretForeground, null, new Rect(x, y + 2, 1.2, _editor.LineHeight - 4));
                    }
                }
            }

            private void DrawBracketHighlight(DrawingContext dc, string line, double y, int column)
            {
                if (column < 0 || column >= line.Length) return;
                double x = _editor.TextOrigin + _editor.MeasureTextWidth(line.Substring(0, column));
                double width = _editor.MeasureTextWidth(line.Substring(column, 1));
                dc.DrawRectangle(BracketBackground, null, new Rect(x, y + 1, Math.Max(2, width), _editor.LineHeight - 2));
            }

            private static void DrawChevron(DrawingContext dc, double x, double y, bool collapsed)
            {
                if (collapsed)
                {
                    dc.DrawLine(ChevronPen, new Point(x - 2, y - 3), new Point(x + 1, y));
                    dc.DrawLine(ChevronPen, new Point(x + 1, y), new Point(x - 2, y + 3));
                }
                else
                {
                    dc.DrawLine(ChevronPen, new Point(x - 3, y - 1), new Point(x, y + 2));
                    dc.DrawLine(ChevronPen, new Point(x, y + 2), new Point(x + 3, y - 1));
                }
            }

            private FormattedText MakeText(string value, Typeface typeface, Brush brush, double dpi)
            {
                return new FormattedText(value, CultureInfo.CurrentUICulture, _editor.FlowDirection,
                    typeface, _editor.FontSize, brush, dpi);
            }
        }
    }
}
