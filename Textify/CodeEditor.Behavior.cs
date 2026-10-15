using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace Textify
{
    public sealed partial class CodeEditor
    {
        private void HandleTextInput(object sender, TextCompositionEventArgs e)
        {
            if (IsReadOnly || string.IsNullOrEmpty(e.Text)) return;
            var value = e.Text.Replace("\r\n", "\n").Replace('\r', '\n');
            if (AutoCloseBrackets && value.Length == 1 && _secondaryCarets.Count == 0 &&
                _secondarySelections.Count == 0 && !_isRectangularSelection)
            {
                char ch = value[0];
                char closing = MatchingBracket(ch);
                if (IsOpeningBracket(ch))
                {
                    ReplaceSelection(new string(ch, 1) + new string(closing, 1));
                    _caret = Math.Max(0, _caret - 1);
                    _selectionAnchor = _caret;
                    Changed();
                    e.Handled = true;
                    return;
                }
                if (!IsOpeningBracket(ch) && _caret < Text.Length && Text[_caret] == ch && SelectionLength == 0)
                {
                    MoveTo(_caret + 1, false);
                    e.Handled = true;
                    return;
                }
            }
            ReplaceSelection(value);
            e.Handled = true;
        }

        private void HandleKeyDown(object sender, KeyEventArgs e)
        {
            bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
            bool control = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
            bool alt = (Keyboard.Modifiers & ModifierKeys.Alt) != 0;
            if (EnableWordWrapShortcut && alt && e.Key == Key.Z)
            { IsWordWrapEnabled = !IsWordWrapEnabled; e.Handled = true; return; }
            if (EnableLineOperations && control && shift && e.Key == Key.D7)
            { ToggleLineComment(); e.Handled = true; return; }
            if (EnableLineOperations && control && e.Key == Key.OemQuestion)
            { ToggleLineComment(); e.Handled = true; return; }
            if (EnableMultipleCarets && control && e.Key == Key.D)
            { SelectNextOccurrence(); e.Handled = true; return; }
            if (EnableLineOperations && control && shift && e.Key == Key.Up)
            { MoveCurrentLine(-1); e.Handled = true; return; }
            if (EnableLineOperations && control && shift && e.Key == Key.Down)
            { MoveCurrentLine(1); e.Handled = true; return; }
            if (EnableMultipleCarets && control && alt && e.Key == Key.Up)
            { AddCaretOnAdjacentVisibleLine(-1); e.Handled = true; return; }
            if (EnableMultipleCarets && control && alt && e.Key == Key.Down)
            { AddCaretOnAdjacentVisibleLine(1); e.Handled = true; return; }
            if (control)
            {
                switch (e.Key)
                {
                    case Key.A:
                        _isRectangularSelection = false;
                        _secondaryCarets.Clear();
                        _secondarySelections.Clear();
                        _selectionAnchor = 0; _caret = Text.Length;
                        _caretAtFoldPlaceholder = false; _selectionAnchorAtFoldPlaceholder = false;
                        Changed(); e.Handled = true; return;
                    case Key.C: Copy(); e.Handled = true; return;
                    case Key.X: if (!IsReadOnly) { Copy(); ReplaceSelection(string.Empty); } e.Handled = true; return;
                    case Key.V: if (!IsReadOnly) Paste(); e.Handled = true; return;
                    case Key.Z: if (!IsReadOnly) Undo(); e.Handled = true; return;
                    case Key.Y: if (!IsReadOnly) Redo(); e.Handled = true; return;
                    case Key.Home: MoveTo(0, shift); e.Handled = true; return;
                    case Key.End: MoveTo(Text.Length, shift); e.Handled = true; return;
                }
            }
            if (e.Key == Key.Back && !IsReadOnly) { Backspace(); e.Handled = true; return; }
            if (e.Key == Key.Delete && !IsReadOnly) { Delete(); e.Handled = true; return; }
            if (e.Key == Key.Enter && !IsReadOnly)
            {
                if (AutoIndent && !_isRectangularSelection && _secondaryCarets.Count == 0)
                    InsertIndentedNewline();
                else ReplaceSelection("\n");
                e.Handled = true; return;
            }
            if (e.Key == Key.Tab && EnableTabIndentation && !IsReadOnly)
            {
                if (shift) IndentSelectedLines(false);
                else if (SelectionLength > 0) IndentSelectedLines(true);
                else ReplaceSelection("    ");
                e.Handled = true; return;
            }
            if (e.Key == Key.Left) { MoveHorizontal(-1, shift, control); e.Handled = true; return; }
            if (e.Key == Key.Right) { MoveHorizontal(1, shift, control); e.Handled = true; return; }
            if (e.Key == Key.Home) { MoveTo(LineStart(_caret), shift); e.Handled = true; return; }
            if (e.Key == Key.End) { MoveTo(LineEnd(_caret), shift); e.Handled = true; return; }
            if (e.Key == Key.Up || e.Key == Key.Down) { MoveVertical(e.Key == Key.Up ? -1 : 1, shift); e.Handled = true; return; }
            if (e.Key == Key.PageUp || e.Key == Key.PageDown)
            {
                _scroll.ScrollToVerticalOffset(_scroll.VerticalOffset + (e.Key == Key.PageUp ? -1 : 1) * _scroll.ViewportHeight);
                MoveVertical(e.Key == Key.PageUp ? -Math.Max(1, (int)(_scroll.ViewportHeight / LineHeight)) : Math.Max(1, (int)(_scroll.ViewportHeight / LineHeight)), shift);
                e.Handled = true; return;
            }
        }

        private void ReplaceSelection(string insertion)
        {
            if (_isRectangularSelection || _secondaryCarets.Count > 0 || _secondarySelections.Count > 0)
            {
                ReplaceMultipleCarets(insertion);
                return;
            }
            if (_caretAtFoldPlaceholder && SelectionLength == 0)
            {
                CollapsibleRegion region;
                if (TryGetCollapsedRegionAtHeaderEnd(_caret, out region))
                {
                    int afterRegion = PositionAfterCollapsedRegion(region);
                    if (afterRegion >= 0) _caret = _selectionAnchor = afterRegion;
                    else region.IsCollapsed = false;
                }
                _caretAtFoldPlaceholder = false;
                _selectionAnchorAtFoldPlaceholder = false;
            }
            int start = SelectionStart, length = SelectionLength;
            string updated = Text.Remove(start, length).Insert(start, insertion);
            if (!string.Equals(updated, Text, StringComparison.Ordinal))
            {
                SaveUndo();
                _pendingEditStart = start;
                _pendingEditLength = length;
                _pendingEditInsertion = insertion;
                Text = updated;
            }
            else
            {
                _pendingEditStart = -1;
                _pendingEditLength = 0;
                _pendingEditInsertion = null;
            }
            _caret = start + insertion.Length;
            _selectionAnchor = _caret;
            _selectionAnchorAtFoldPlaceholder = false;
            Changed();
        }

        private void Backspace()
        {
            if ((_isRectangularSelection || _secondaryCarets.Count > 0 || _secondarySelections.Count > 0) && !_caretAtFoldPlaceholder)
            { DeleteAtMultipleCarets(true); return; }
            if (_caretAtFoldPlaceholder) { ExpandFoldAtPlaceholder(); return; }
            if (SelectionLength > 0) { ReplaceSelection(string.Empty); return; }
            if (_caret == 0) return;
            if (_caret > 1 && Text[_caret - 1] == '\n' && Text[_caret - 2] == '\r') _caret--;
            _selectionAnchor = _caret - 1; ReplaceSelection(string.Empty);
        }

        private void Delete()
        {
            if ((_isRectangularSelection || _secondaryCarets.Count > 0 || _secondarySelections.Count > 0) && !_caretAtFoldPlaceholder)
            { DeleteAtMultipleCarets(false); return; }
            if (_caretAtFoldPlaceholder) { ExpandFoldAtPlaceholder(); return; }
            if (SelectionLength > 0) { ReplaceSelection(string.Empty); return; }
            if (_caret >= Text.Length) return;
            _selectionAnchor = _caret + 1; ReplaceSelection(string.Empty);
        }

        private void ExpandFoldAtPlaceholder()
        {
            CollapsibleRegion region;
            if (TryGetCollapsedRegionAtHeaderEnd(_caret, out region)) region.IsCollapsed = false;
            _caretAtFoldPlaceholder = false;
            Changed();
            EnsureCaretVisible();
        }

        private void MoveHorizontal(int direction, bool extend, bool byWord)
        {
            if (_secondaryCarets.Count > 0 || _secondarySelections.Count > 0 || _isRectangularSelection)
            {
                _secondaryCarets.Clear();
                _secondarySelections.Clear();
                _isRectangularSelection = false;
                if (!extend) { _selectionAnchor = _caret; _selectionAnchorAtFoldPlaceholder = _caretAtFoldPlaceholder; }
            }
            if (!extend && SelectionLength > 0) { MoveTo(direction < 0 ? SelectionStart : SelectionStart + SelectionLength, false); return; }
            CollapsibleRegion region;
            if (_caretAtFoldPlaceholder)
            {
                if (direction < 0)
                {
                    _caretAtFoldPlaceholder = false;
                    if (!extend) { _selectionAnchor = _caret; _selectionAnchorAtFoldPlaceholder = false; }
                    _preferredColumn = -1;
                    Changed();
                    EnsureCaretVisible();
                    return;
                }
                if (direction > 0 && TryGetCollapsedRegionAtHeaderEnd(_caret, out region))
                {
                    int afterRegion = PositionAfterCollapsedRegion(region);
                    if (afterRegion >= 0) MoveTo(afterRegion, extend);
                    return;
                }
                _caretAtFoldPlaceholder = false;
            }
            if (direction > 0 && SelectionLength == 0 && TryGetCollapsedRegionAtHeaderEnd(_caret, out region))
            {
                _caretAtFoldPlaceholder = true;
                if (!extend) { _selectionAnchor = _caret; _selectionAnchorAtFoldPlaceholder = false; }
                _preferredColumn = -1;
                Changed();
                EnsureCaretVisible();
                return;
            }
            if (direction < 0 && SelectionLength == 0 && TryGetCollapsedRegionBeforePosition(_caret, out region))
            {
                _caret = GetLineEndAtLine(region.StartLine);
                _caretAtFoldPlaceholder = true;
                if (!extend) { _selectionAnchor = _caret; _selectionAnchorAtFoldPlaceholder = true; }
                _preferredColumn = -1;
                Changed();
                EnsureCaretVisible();
                return;
            }
            int target = _caret + direction;
            if (byWord && target >= 0 && target <= Text.Length)
            {
                while (target > 0 && target < Text.Length && char.IsWhiteSpace(Text[target])) target += direction;
                while (target > 0 && target < Text.Length && !char.IsWhiteSpace(Text[target - (direction < 0 ? 1 : 0)])) target += direction;
            }
            MoveTo(Math.Max(0, Math.Min(Text.Length, target)), extend);
        }

        private void MoveVertical(int delta, bool extend)
        {
            int currentLine = LineIndexAt(_caret);
            CollapsibleRegion region;
            if (TryGetVisibleCollapsedRegionContainingLine(currentLine, out region))
            {
                currentLine = region.StartLine;
                _caret = GetLineEndAtLine(currentLine);
                _caretAtFoldPlaceholder = true;
            }
            int start = GetLineStart(currentLine);
            int currentRow = VisibleRowForPosition(_caret);
            if (_preferredColumn < 0)
            {
                _preferredColumn = _caret - start - VisibleRowStartColumn(currentRow);
                if (_caretAtFoldPlaceholder) _preferredColumn++;
            }

            // Navigate the visible-row list directly. Document lines hidden by a fold
            // all map to the header row, so stepping document line numbers here would
            // make Shift+Up/Down extend through every hidden line.
            int targetRow = Math.Max(0, Math.Min(VisibleLineCount - 1, currentRow + delta));
            int targetLine = DocumentLineAtVisibleRow(targetRow);
            int targetLineStart = GetLineStart(targetLine);
            int targetSegmentStart = VisibleRowStartColumn(targetRow);
            int targetSegmentLength = VisibleRowLength(targetRow);
            int targetSegmentEnd = targetSegmentStart + targetSegmentLength;
            int targetColumn = Math.Min(targetSegmentEnd, targetSegmentStart + _preferredColumn);
            bool isLastTargetSegment = targetRow + 1 >= VisibleLineCount || DocumentLineAtVisibleRow(targetRow + 1) != targetLine;
            bool targetAtPlaceholder = IsFoldCollapsedAtLine(targetLine) && isLastTargetSegment &&
                targetColumn >= GetLineEndAtLine(targetLine) - targetLineStart;

            MoveTo(targetLineStart + targetColumn, extend, preservePreferredColumn: true);
            if (targetAtPlaceholder)
            {
                _caretAtFoldPlaceholder = true;
                if (!extend) _selectionAnchorAtFoldPlaceholder = true;
                Changed();
            }
        }

        private void MoveTo(int position, bool extend, bool preservePreferredColumn = false)
        {
            _secondaryCarets.Clear();
            _secondarySelections.Clear();
            _isRectangularSelection = false;
            _caretAtFoldPlaceholder = false;
            int bounded = Math.Max(0, Math.Min(Text.Length, position));
            CollapsibleRegion region;
            if (TryGetVisibleCollapsedRegionContainingLine(LineIndexAt(bounded), out region))
            {
                _caret = GetLineEndAtLine(region.StartLine);
                _caretAtFoldPlaceholder = true;
            }
            else _caret = bounded;
            if (!extend)
            {
                _selectionAnchor = _caret;
                _selectionAnchorAtFoldPlaceholder = _caretAtFoldPlaceholder;
            }
            if (!preservePreferredColumn) _preferredColumn = -1;
            Changed(); EnsureCaretVisible();
        }

        private int LineStart(int index) { return GetLineStart(LineIndexAt(index)); }
        private int LineEnd(int index) { return GetLineEndAtLine(LineIndexAt(index)); }

        private void SaveUndo()
        {
            _undo.Add(CaptureEditSnapshot());
            if (_undo.Count > 300) _undo.RemoveAt(0);
            _redo.Clear();
        }

        private EditSnapshot CaptureEditSnapshot()
        {
            return new EditSnapshot(Text, _caret, _selectionAnchor, CaptureFoldSnapshots(),
                new List<int>(_secondaryCarets), new List<SelectionSpan>(_secondarySelections),
                _isRectangularSelection, _rectangleAnchorLine, _rectangleAnchorColumn,
                _rectangleEndLine, _rectangleEndColumn, _caretAtFoldPlaceholder,
                _selectionAnchorAtFoldPlaceholder);
        }

        private void Undo()
        {
            if (_undo.Count == 0) return;
            _redo.Add(CaptureEditSnapshot());
            var state = _undo[_undo.Count - 1]; _undo.RemoveAt(_undo.Count - 1);
            Restore(state);
        }

        private void Redo()
        {
            if (_redo.Count == 0) return;
            _undo.Add(CaptureEditSnapshot());
            var state = _redo[_redo.Count - 1]; _redo.RemoveAt(_redo.Count - 1);
            Restore(state);
        }

        private void Restore(EditSnapshot state)
        {
            _pendingFoldRestore = state.Folds;
            Text = state.Text;
            if (_pendingFoldRestore != null)
            {
                RestoreFoldSnapshots(_pendingFoldRestore);
                _pendingFoldRestore = null;
                RebuildVisibleLineMap();
            }
            _caret = state.Caret; _selectionAnchor = state.Anchor;
            _secondaryCarets.Clear();
            if (EnableMultipleCarets && state.SecondaryCarets != null) _secondaryCarets.AddRange(state.SecondaryCarets);
            _secondarySelections.Clear();
            if (EnableMultipleCarets && state.SecondarySelections != null) _secondarySelections.AddRange(state.SecondarySelections);
            _isRectangularSelection = EnableRectangularSelection && state.IsRectangularSelection;
            _rectangleAnchorLine = state.RectangleAnchorLine;
            _rectangleAnchorColumn = state.RectangleAnchorColumn;
            _rectangleEndLine = state.RectangleEndLine;
            _rectangleEndColumn = state.RectangleEndColumn;
            _caretAtFoldPlaceholder = state.CaretAtFoldPlaceholder;
            _selectionAnchorAtFoldPlaceholder = state.SelectionAnchorAtFoldPlaceholder;
            Changed(); EnsureCaretVisible();
        }

        private void Copy()
        {
            if (HasSelection) Clipboard.SetText(GetSelectedText());
        }

        private void Paste()
        {
            if (!Clipboard.ContainsText()) return;
            ReplaceSelection(Clipboard.GetText().Replace("\r\n", "\n").Replace('\r', '\n'));
        }

        private void HandleMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            ModifierKeys modifiers = Keyboard.Modifiers;
            bool extendSelection = (modifiers & ModifierKeys.Shift) != 0;
            bool rectangle = EnableRectangularSelection &&
                (modifiers & (ModifierKeys.Alt | ModifierKeys.Shift)) == (ModifierKeys.Alt | ModifierKeys.Shift);
            bool addCaret = EnableMultipleCarets &&
                (modifiers & (ModifierKeys.Control | ModifierKeys.Alt)) == (ModifierKeys.Control | ModifierKeys.Alt);
            Focus(); _surface.Focus();
            Point point = e.GetPosition(_surface);
            if (rectangle)
            {
                int position = PositionFromPoint(point);
                _isRectangularSelection = true;
                _secondaryCarets.Clear();
                _secondarySelections.Clear();
                _rectangleAnchorLine = _rectangleEndLine = LineIndexAt(position);
                _rectangleAnchorColumn = _rectangleEndColumn = position - GetLineStart(_rectangleAnchorLine);
                _selectionAnchor = _caret = position;
                _caretAtFoldPlaceholder = false;
                _preferredColumn = -1;
                _surface.CaptureMouse(); Changed(); e.Handled = true; return;
            }
            if (addCaret)
            {
                int position = PositionFromPoint(point);
                _isRectangularSelection = false;
                AddCaret(position);
                _surface.CaptureMouse(); e.Handled = true; return;
            }
            int row;
            if (IsChevronAtPoint(point, out row))
            {
                ToggleFoldAtVisibleRow(row);
                e.Handled = true;
                return;
            }
            if (IsLineNumberAtPoint(point, out row))
            {
                SelectWholeLineAtRow(row, extendSelection);
                e.Handled = true;
                return;
            }
            _isRectangularSelection = false;
            _secondaryCarets.Clear();
            _secondarySelections.Clear();
            int placeholderLine;
            if (IsFoldPlaceholderAtPoint(point, out placeholderLine))
            {
                _caret = GetLineEndAtLine(placeholderLine);
                _caretAtFoldPlaceholder = true;
                _preferredColumn = -1;
                if (!extendSelection)
                {
                    _selectionAnchor = _caret;
                    _selectionAnchorAtFoldPlaceholder = true;
                }
                Changed();
                _surface.CaptureMouse();
                e.Handled = true;
                return;
            }
            _caretAtFoldPlaceholder = false;
            _preferredColumn = -1;
            _caret = PositionFromPoint(point);
            if (!extendSelection)
            {
                _selectionAnchor = _caret;
                _selectionAnchorAtFoldPlaceholder = false;
            }
            _surface.CaptureMouse(); Changed(); e.Handled = true;
        }

        private void HandleMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _surface.IsMouseCaptured)
            {
                _surface.Cursor = Cursors.IBeam;
                Point point = e.GetPosition(_surface);
                if (_isRectangularSelection)
                {
                    UpdateRectangleFromPosition(PositionFromPoint(point));
                    return;
                }
                int placeholderLine;
                if (IsFoldPlaceholderAtPoint(point, out placeholderLine))
                {
                    _caret = GetLineEndAtLine(placeholderLine);
                    _caretAtFoldPlaceholder = true;
                }
                else
                {
                    _caret = PositionFromPoint(point);
                    _caretAtFoldPlaceholder = false;
                }
                _surface.InvalidateVisual();
                PublishSelectionState();
            }
            else
            {
                int row;
                Point point = e.GetPosition(_surface);
                if (IsChevronAtPoint(point, out row)) _surface.Cursor = Cursors.Hand;
                else if (IsLineNumberAtPoint(point, out row)) _surface.Cursor = Cursors.Arrow;
                else _surface.Cursor = Cursors.IBeam;
            }
        }

        private bool IsLineNumberAtPoint(Point point, out int row)
        {
            row = -1;
            if (!ShowLineNumbers || point.Y < TopInset) return false;
            row = (int)Math.Floor((point.Y - TopInset) / LineHeight);
            if (row < 0 || row >= VisibleLineCount || VisibleRowStartColumn(row) != 0) return false;

            int line = DocumentLineAtVisibleRow(row);
            string label = (line + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
            double labelRight = TextOrigin - 22;
            double labelLeft = labelRight - MeasureTextWidth(label);
            return point.X >= labelLeft - 3 && point.X <= labelRight + 3;
        }

        private void SelectWholeLineAtRow(int row, bool extendSelection)
        {
            int line = DocumentLineAtVisibleRow(row);
            int lineStart = GetLineStart(line);
            int selectionEnd = line + 1 < LineCount ? GetLineStart(line + 1) : Text.Length;
            bool collapsedHeader = EnableCodeFolding && IsFoldCollapsedAtLine(line);

            _isRectangularSelection = false;
            _secondaryCarets.Clear();
            _secondarySelections.Clear();

            if (!extendSelection)
            {
                _selectionAnchor = lineStart;
                _selectionAnchorAtFoldPlaceholder = false;
            }

            if (extendSelection && lineStart < _selectionAnchor)
            {
                _caret = lineStart;
                _caretAtFoldPlaceholder = false;
            }
            else if (collapsedHeader)
            {
                _caret = GetLineEndAtLine(line);
                _caretAtFoldPlaceholder = true;
            }
            else
            {
                _caret = selectionEnd;
                _caretAtFoldPlaceholder = false;
            }

            _preferredColumn = -1;
            Changed();
        }

        private bool IsChevronAtPoint(Point point, out int row)
        {
            row = -1;
            if (!EnableCodeFolding || point.Y < TopInset) return false;
            row = (int)Math.Floor((point.Y - TopInset) / LineHeight);
            if (row < 0 || row >= VisibleLineCount) return false;
            if (VisibleRowStartColumn(row) != 0) return false;
            double chevronCenterX = TextOrigin - 8;
            double chevronCenterY = TopInset + row * LineHeight + LineHeight / 2;
            if (point.X < chevronCenterX - 7 || point.X > chevronCenterX + 7 ||
                point.Y < chevronCenterY - 7 || point.Y > chevronCenterY + 7)
                return false;
            return CanFoldAtLine(DocumentLineAtVisibleRow(row));
        }

        private bool IsFoldPlaceholderAtPoint(Point point, out int line)
        {
            line = -1;
            if (point.Y < TopInset) return false;
            int row = (int)Math.Floor((point.Y - TopInset) / LineHeight);
            if (row < 0 || row >= VisibleLineCount) return false;
            line = DocumentLineAtVisibleRow(row);
            if (!IsFoldCollapsedAtLine(line)) return false;
            if (row + 1 < VisibleLineCount && DocumentLineAtVisibleRow(row + 1) == line) return false;
            double y = TopInset + row * LineHeight;
            if (point.Y < y + 2 || point.Y >= y + LineHeight - 2) return false;
            string fullValue = Lines[line].TrimEnd('\r');
            int segmentStart = VisibleRowStartColumn(row);
            string value = fullValue.Substring(segmentStart, Math.Min(VisibleRowLength(row), fullValue.Length - segmentStart));
            double startX = TextOrigin + MeasureTextWidth(value + "  ");
            double endX = startX + MeasureTextWidth("…");
            return point.X >= startX - FoldMarkerPadding && point.X <= endX + FoldMarkerPadding;
        }

        private int PositionFromPoint(Point point)
        {
            int row = Math.Max(0, Math.Min(VisibleLineCount - 1, (int)Math.Floor((point.Y - TopInset) / LineHeight)));
            int line = DocumentLineAtVisibleRow(row);
            int segmentStart = VisibleRowStartColumn(row);
            int lineStart = GetLineStart(line) + segmentStart;
            string visibleLine = Text.Substring(lineStart, Math.Min(VisibleRowLength(row), Math.Max(0, LineEnd(lineStart) - lineStart)));
            double targetX = Math.Max(0, point.X - TextOrigin);
            int low = 0, high = visibleLine.Length;
            while (low < high)
            {
                int mid = (low + high + 1) / 2;
                if (MeasureTextWidth(visibleLine.Substring(0, mid)) <= targetX) low = mid;
                else high = mid - 1;
            }
            int col = low;
            if (col < visibleLine.Length)
            {
                double left = MeasureTextWidth(visibleLine.Substring(0, col));
                double right = MeasureTextWidth(visibleLine.Substring(0, col + 1));
                if (targetX - left > (right - left) / 2) col++;
            }
            return Math.Min(LineEnd(lineStart), Math.Max(lineStart, lineStart + col));
        }

        private void HandleMouseUp(object sender, MouseButtonEventArgs e) { _surface.ReleaseMouseCapture(); }

        private void Changed()
        {
            _caretVisible = true; _surface.InvalidateVisual(); EnsureCaretVisible(); PublishSelectionState();
        }

        private void EnsureCaretVisible()
        {
            if (_surface == null || _scroll == null) return;
            int lineStart = LineStart(_caret);
            int lineEnd = LineEnd(lineStart);
            int visibleRow = VisibleRowForPosition(_caret);
            int visibleStart = lineStart + VisibleRowStartColumn(visibleRow);
            int prefixEnd = Math.Min(_caret, lineEnd);
            string prefix = Text.Substring(visibleStart, Math.Max(0, prefixEnd - visibleStart));
            double x = TextOrigin + MeasureTextWidth(prefix);
            if (_caretAtFoldPlaceholder) x += MeasureTextWidth("  …");
            double y = TopInset + visibleRow * LineHeight;
            if (x < _scroll.HorizontalOffset) _scroll.ScrollToHorizontalOffset(x);
            else if (x + CharacterWidth > _scroll.HorizontalOffset + _scroll.ViewportWidth) _scroll.ScrollToHorizontalOffset(x + CharacterWidth - _scroll.ViewportWidth);
            if (y < _scroll.VerticalOffset) _scroll.ScrollToVerticalOffset(y);
            else if (y + LineHeight > _scroll.VerticalOffset + _scroll.ViewportHeight) _scroll.ScrollToVerticalOffset(y + LineHeight - _scroll.ViewportHeight);
        }
    }
}
