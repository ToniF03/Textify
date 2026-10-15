using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Textify
{
    /// <summary>A small, self-contained WPF text editor. It does not derive from TextBox.</summary>
    public sealed partial class CodeEditor : Grid
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text), typeof(string), typeof(CodeEditor), new FrameworkPropertyMetadata(string.Empty, OnTextChanged));
        public static readonly DependencyProperty ShowLineNumbersProperty = DependencyProperty.Register(
            nameof(ShowLineNumbers), typeof(bool), typeof(CodeEditor), new FrameworkPropertyMetadata(false, OnVisualOptionChanged));
        public static readonly DependencyProperty LineNumberBackgroundProperty = DependencyProperty.Register(
            nameof(LineNumberBackground), typeof(Brush), typeof(CodeEditor),
            new FrameworkPropertyMetadata(CreateFrozenBrush(Color.FromRgb(247, 248, 250)), OnVisualOptionChanged));
        public static readonly DependencyProperty LineNumberForegroundProperty = DependencyProperty.Register(
            nameof(LineNumberForeground), typeof(Brush), typeof(CodeEditor),
            new FrameworkPropertyMetadata(CreateFrozenBrush(Color.FromRgb(145, 153, 163)), OnVisualOptionChanged));
        public static readonly DependencyProperty FontFamilyProperty = DependencyProperty.Register(
            nameof(FontFamily), typeof(FontFamily), typeof(CodeEditor), new FrameworkPropertyMetadata(new FontFamily("Consolas"), OnVisualOptionChanged));
        public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register(
            nameof(FontSize), typeof(double), typeof(CodeEditor), new FrameworkPropertyMetadata(14d, OnVisualOptionChanged));
        public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register(
            nameof(Foreground), typeof(Brush), typeof(CodeEditor), new FrameworkPropertyMetadata(Brushes.DarkSlateGray, OnVisualOptionChanged));
        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
            nameof(IsReadOnly), typeof(bool), typeof(CodeEditor), new FrameworkPropertyMetadata(false));
        public static readonly DependencyProperty IsWordWrapEnabledProperty = DependencyProperty.Register(
            nameof(IsWordWrapEnabled), typeof(bool), typeof(CodeEditor), new FrameworkPropertyMetadata(false, OnWordWrapChanged));
        public static readonly DependencyProperty IsHorizontalScrollEnabledProperty = DependencyProperty.Register(
            nameof(IsHorizontalScrollEnabled), typeof(bool), typeof(CodeEditor), new FrameworkPropertyMetadata(false, OnHorizontalScrollChanged));
        public static readonly DependencyProperty HighlightCurrentLineProperty = DependencyProperty.Register(
            nameof(HighlightCurrentLine), typeof(bool), typeof(CodeEditor), new FrameworkPropertyMetadata(false, OnVisualOptionChanged));
        public static readonly DependencyProperty CurrentLineBackgroundProperty = DependencyProperty.Register(
            nameof(CurrentLineBackground), typeof(Brush), typeof(CodeEditor),
            new FrameworkPropertyMetadata(CreateFrozenBrush(Color.FromRgb(246, 248, 251)), OnVisualOptionChanged));
        public static readonly DependencyProperty AutoIndentProperty = DependencyProperty.Register(
            nameof(AutoIndent), typeof(bool), typeof(CodeEditor), new FrameworkPropertyMetadata(false));
        public static readonly DependencyProperty AutoCloseBracketsProperty = DependencyProperty.Register(
            nameof(AutoCloseBrackets), typeof(bool), typeof(CodeEditor), new FrameworkPropertyMetadata(false));
        public static readonly DependencyProperty EnableCodeFoldingProperty = DependencyProperty.Register(
            nameof(EnableCodeFolding), typeof(bool), typeof(CodeEditor), new FrameworkPropertyMetadata(false, OnFeatureOptionChanged));
        public static readonly DependencyProperty EnableSyntaxHighlightingProperty = DependencyProperty.Register(
            nameof(EnableSyntaxHighlighting), typeof(bool), typeof(CodeEditor), new FrameworkPropertyMetadata(false, OnFeatureOptionChanged));
        public static readonly DependencyProperty EnableBracketHighlightingProperty = DependencyProperty.Register(
            nameof(EnableBracketHighlighting), typeof(bool), typeof(CodeEditor), new FrameworkPropertyMetadata(false, OnFeatureOptionChanged));
        public static readonly DependencyProperty EnableMultipleCaretsProperty = DependencyProperty.Register(
            nameof(EnableMultipleCarets), typeof(bool), typeof(CodeEditor), new FrameworkPropertyMetadata(false, OnFeatureOptionChanged));
        public static readonly DependencyProperty EnableRectangularSelectionProperty = DependencyProperty.Register(
            nameof(EnableRectangularSelection), typeof(bool), typeof(CodeEditor), new FrameworkPropertyMetadata(false, OnFeatureOptionChanged));
        public static readonly DependencyProperty EnableEditorContextMenuProperty = DependencyProperty.Register(
            nameof(EnableEditorContextMenu), typeof(bool), typeof(CodeEditor), new FrameworkPropertyMetadata(false, OnFeatureOptionChanged));
        public static readonly DependencyProperty EnableLineOperationsProperty = DependencyProperty.Register(
            nameof(EnableLineOperations), typeof(bool), typeof(CodeEditor), new FrameworkPropertyMetadata(false, OnFeatureOptionChanged));
        public static readonly DependencyProperty EnableWordWrapShortcutProperty = DependencyProperty.Register(
            nameof(EnableWordWrapShortcut), typeof(bool), typeof(CodeEditor), new FrameworkPropertyMetadata(false));
        public static readonly DependencyProperty EnableTabIndentationProperty = DependencyProperty.Register(
            nameof(EnableTabIndentation), typeof(bool), typeof(CodeEditor), new FrameworkPropertyMetadata(false));

        private readonly ScrollViewer _scroll;
        private readonly EditorSurface _surface;
        private readonly List<EditSnapshot> _undo = new List<EditSnapshot>();
        private readonly List<EditSnapshot> _redo = new List<EditSnapshot>();
        private readonly Dictionary<string, double> _textWidthCache = new Dictionary<string, double>(StringComparer.Ordinal);
        private readonly List<int> _lineStarts = new List<int> { 0 };
        private readonly List<int> _secondaryCarets = new List<int>();
        private readonly List<SelectionSpan> _secondarySelections = new List<SelectionSpan>();
        private readonly CollapsibleRegionCollection _collapsibles;
        private readonly Dictionary<int, CollapsibleRegion> _foldsByStartLine = new Dictionary<int, CollapsibleRegion>();
        private string[] _lineCache = new[] { string.Empty };
        private int[] _visibleLines = new[] { 0 };
        private int[] _documentToVisibleRow = new[] { 0 };
        private int[] _visibleRowStartColumns = new[] { 0 };
        private int[] _visibleRowLengths = new[] { 0 };
        private double _wrapTextWidth = double.PositiveInfinity;
        private bool _isRectangularSelection;
        private int _rectangleAnchorLine;
        private int _rectangleAnchorColumn;
        private int _rectangleEndLine;
        private int _rectangleEndColumn;
        private int _lastPublishedCaret = -1;
        private bool _lastPublishedCaretAtPlaceholder;
        private int _lastPublishedSelectionStart = -1;
        private int _lastPublishedSelectionLength = -1;
        private int _lastPublishedSecondaryCaretCount = -1;
        private string _lastPublishedSecondaryState;
        private bool _lastPublishedRectangle;
        private string _matchingBracketCacheText;
        private int _matchingBracketCacheCaret = -1;
        private int _matchingBracketCacheResult = -1;
        private double _metricDpi = -1;
        private readonly DispatcherTimer _caretTimer;
        private int _caret;
        private int _selectionAnchor;
        private bool _selectionAnchorAtFoldPlaceholder;
        private int _preferredColumn = -1;
        private bool _caretVisible = true;
        private bool _caretAtFoldPlaceholder;
        private int _pendingEditStart = -1;
        private int _pendingEditLength;
        private string _pendingEditInsertion;
        private List<FoldSnapshot> _pendingFoldRestore;
        private const double LeftInset = 10;
        private const double TopInset = 8;
        private const double FoldMarkerPadding = 3;

        private static Brush CreateFrozenBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        public string Text { get { return (string)GetValue(TextProperty); } set { SetValue(TextProperty, value ?? string.Empty); } }
        public bool ShowLineNumbers { get { return (bool)GetValue(ShowLineNumbersProperty); } set { SetValue(ShowLineNumbersProperty, value); } }
        public Brush LineNumberBackground { get { return (Brush)GetValue(LineNumberBackgroundProperty); } set { SetValue(LineNumberBackgroundProperty, value); } }
        public Brush LineNumberForeground { get { return (Brush)GetValue(LineNumberForegroundProperty); } set { SetValue(LineNumberForegroundProperty, value); } }
        public FontFamily FontFamily { get { return (FontFamily)GetValue(FontFamilyProperty); } set { SetValue(FontFamilyProperty, value); } }
        public double FontSize { get { return (double)GetValue(FontSizeProperty); } set { SetValue(FontSizeProperty, value); } }
        public Brush Foreground { get { return (Brush)GetValue(ForegroundProperty); } set { SetValue(ForegroundProperty, value); } }
        public bool IsReadOnly { get { return (bool)GetValue(IsReadOnlyProperty); } set { SetValue(IsReadOnlyProperty, value); } }
        public bool IsWordWrapEnabled { get { return (bool)GetValue(IsWordWrapEnabledProperty); } set { SetValue(IsWordWrapEnabledProperty, value); } }
        public bool IsHorizontalScrollEnabled { get { return (bool)GetValue(IsHorizontalScrollEnabledProperty); } set { SetValue(IsHorizontalScrollEnabledProperty, value); } }
        public bool HighlightCurrentLine { get { return (bool)GetValue(HighlightCurrentLineProperty); } set { SetValue(HighlightCurrentLineProperty, value); } }
        public Brush CurrentLineBackground { get { return (Brush)GetValue(CurrentLineBackgroundProperty); } set { SetValue(CurrentLineBackgroundProperty, value); } }
        public bool AutoIndent { get { return (bool)GetValue(AutoIndentProperty); } set { SetValue(AutoIndentProperty, value); } }
        public bool AutoCloseBrackets { get { return (bool)GetValue(AutoCloseBracketsProperty); } set { SetValue(AutoCloseBracketsProperty, value); } }
        public bool EnableCodeFolding { get { return (bool)GetValue(EnableCodeFoldingProperty); } set { SetValue(EnableCodeFoldingProperty, value); } }
        public bool EnableSyntaxHighlighting { get { return (bool)GetValue(EnableSyntaxHighlightingProperty); } set { SetValue(EnableSyntaxHighlightingProperty, value); } }
        public bool EnableBracketHighlighting { get { return (bool)GetValue(EnableBracketHighlightingProperty); } set { SetValue(EnableBracketHighlightingProperty, value); } }
        public bool EnableMultipleCarets { get { return (bool)GetValue(EnableMultipleCaretsProperty); } set { SetValue(EnableMultipleCaretsProperty, value); } }
        public bool EnableRectangularSelection { get { return (bool)GetValue(EnableRectangularSelectionProperty); } set { SetValue(EnableRectangularSelectionProperty, value); } }
        public bool EnableEditorContextMenu { get { return (bool)GetValue(EnableEditorContextMenuProperty); } set { SetValue(EnableEditorContextMenuProperty, value); } }
        public bool EnableLineOperations { get { return (bool)GetValue(EnableLineOperationsProperty); } set { SetValue(EnableLineOperationsProperty, value); } }
        public bool EnableWordWrapShortcut { get { return (bool)GetValue(EnableWordWrapShortcutProperty); } set { SetValue(EnableWordWrapShortcutProperty, value); } }
        public bool EnableTabIndentation { get { return (bool)GetValue(EnableTabIndentationProperty); } set { SetValue(EnableTabIndentationProperty, value); } }
        public ObservableCollection<SyntaxHighlightRule> SyntaxHighlighting { get; private set; }
        /// <summary>Foldable line ranges supplied by the host application.</summary>
        public CollapsibleRegionCollection Collapsibles { get { return _collapsibles; } }

        public event EventHandler CaretPositionChanged;
        public event EventHandler SelectionChanged;

        public CodeEditor()
        {
            _collapsibles = new CollapsibleRegionCollection(OnCollapsiblesChanged);
            SyntaxHighlighting = new ObservableCollection<SyntaxHighlightRule>();
            SyntaxHighlighting.CollectionChanged += OnSyntaxHighlightingChanged;
            ClipToBounds = true;
            Focusable = true;
            Cursor = Cursors.IBeam;
            Background = Brushes.White;
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            _surface = new EditorSurface(this);
            _scroll = new ScrollViewer { Content = _surface, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto, CanContentScroll = false,
                Background = Background, Focusable = false };
            _scroll.ScrollChanged += (s, e) => _surface.InvalidateVisual();
            _scroll.SizeChanged += OnEditorViewportSizeChanged;
            Grid.SetRow(_scroll, 1);
            Children.Add(_scroll);
            InitializeAdvancedUi();
            _caretTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(530) };
            _caretTimer.Tick += (s, e) => { _caretVisible = !_caretVisible; _surface.InvalidateVisual(); };
            _caretTimer.Start();
            PreviewKeyDown += HandleKeyDown;
            TextInput += HandleTextInput;
            GotKeyboardFocus += (s, e) => { _caretVisible = true; _surface.InvalidateVisual(); };
            LostKeyboardFocus += (s, e) => { _surface.InvalidateVisual(); };
            _surface.MouseDown += HandleMouseDown;
            _surface.MouseMove += HandleMouseMove;
            _surface.MouseUp += (s, e) => _surface.ReleaseMouseCapture();
            _surface.MouseLeave += (s, e) => _surface.Cursor = Cursors.IBeam;
        }

        internal int Caret { get { return _caret; } }
        internal bool CaretAtFoldPlaceholder { get { return _caretAtFoldPlaceholder; } }
        internal bool SelectionAnchorAtFoldPlaceholder { get { return _selectionAnchorAtFoldPlaceholder; } }
        public int CaretOffset { get { return _caret; } set { SetCaret(value, false); } }
        public int SelectionStart { get { return Math.Min(_selectionAnchor, _caret); } }
        public int SelectionLength { get { return Math.Abs(_selectionAnchor - _caret); } }
        internal double LineHeight { get { return Math.Ceiling(FontSize * 1.45); } }
        internal double CharacterWidth { get { return Math.Ceiling(FontSize * 0.61); } }
        internal bool GutterVisible { get { return ShowLineNumbers || EnableCodeFolding; } }
        internal double TextOrigin { get { return LeftInset + (GutterVisible ? GutterWidth : 0); } }
        internal double GutterWidth
        {
            get
            {
                int digits = Math.Max(1, _lineCache.Length.ToString().Length);
                double width = MeasureTextWidth(new string('8', digits));
                return Math.Max(40, width + 30);
            }
        }
        internal bool HasFocus { get { return IsKeyboardFocusWithin; } }
        internal bool CaretVisible { get { return _caretVisible; } }

        internal double MeasureTextWidth(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            if (Math.Abs(_metricDpi - dpi) > 0.001)
            {
                _textWidthCache.Clear();
                _metricDpi = dpi;
            }
            double cachedWidth;
            if (_textWidthCache.TryGetValue(value, out cachedWidth)) return cachedWidth;
            var typeface = new Typeface(FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            var formatted = new FormattedText(value, System.Globalization.CultureInfo.CurrentUICulture,
                FlowDirection, typeface, FontSize, Foreground, dpi);
            double width = formatted.WidthIncludingTrailingWhitespace;
            if (_textWidthCache.Count >= 4096) _textWidthCache.Clear();
            _textWidthCache[value] = width;
            return width;
        }

        internal string[] Lines { get { return _lineCache; } }
        internal int LineCount { get { return _lineStarts.Count; } }
        internal int FirstVisibleLine { get { return Math.Min(VisibleLineCount - 1, Math.Max(0, (int)Math.Floor(_scroll.VerticalOffset / LineHeight))); } }
        internal int VisibleLineEnd
        {
            get
            {
                double height = Math.Max(LineHeight, _scroll.ViewportHeight);
                return Math.Min(VisibleLineCount, (int)Math.Ceiling((_scroll.VerticalOffset + height) / LineHeight) + 1);
            }
        }
        internal int VisibleLineCount { get { return _visibleLines.Length; } }
        internal int DocumentLineAtVisibleRow(int row) { return _visibleLines[Math.Max(0, Math.Min(_visibleLines.Length - 1, row))]; }
        internal int VisibleRowStartColumn(int row) { return _visibleRowStartColumns[Math.Max(0, Math.Min(_visibleRowStartColumns.Length - 1, row))]; }
        internal int VisibleRowLength(int row) { return _visibleRowLengths[Math.Max(0, Math.Min(_visibleRowLengths.Length - 1, row))]; }
        internal int VisibleRowForDocumentLine(int line) { return _documentToVisibleRow[Math.Max(0, Math.Min(_documentToVisibleRow.Length - 1, line))]; }
        internal int GetLineEndAtLine(int lineIndex)
        {
            int start = GetLineStart(lineIndex);
            int end = lineIndex + 1 < LineCount ? GetLineStart(lineIndex + 1) - 1 : Text.Length;
            if (end > start && Text[end - 1] == '\r') end--;
            return end;
        }
        internal bool CanFoldAtLine(int line) { return EnableCodeFolding && _foldsByStartLine.ContainsKey(line); }
        internal bool IsFoldCollapsedAtLine(int line)
        {
            CollapsibleRegion region;
            return EnableCodeFolding && _foldsByStartLine.TryGetValue(line, out region) && region.IsCollapsed;
        }

        internal bool IsFoldPlaceholderSelected(int line)
        {
            CollapsibleRegion region;
            if (!EnableCodeFolding || SelectionLength == 0 || !_foldsByStartLine.TryGetValue(line, out region) || !region.IsCollapsed)
                return false;

            if ((_caretAtFoldPlaceholder && LineIndexAt(_caret) == line) ||
                (_selectionAnchorAtFoldPlaceholder && LineIndexAt(_selectionAnchor) == line))
                return true;

            int hiddenStart = GetLineEndAtLine(line);
            int hiddenEnd = region.EndLineExclusive < LineCount ? GetLineStart(region.EndLineExclusive) : Text.Length;
            int selectionEnd = SelectionStart + SelectionLength;
            return SelectionStart < hiddenEnd && selectionEnd > hiddenStart;
        }

        internal bool IsFoldHeaderLineFullySelected(int line, int lineStart, int lineEnd)
        {
            return IsFoldPlaceholderSelected(line) && SelectionStart <= lineStart &&
                SelectionStart + SelectionLength >= lineEnd;
        }

        internal bool TryGetCollapsedRegionAtHeaderEnd(int position, out CollapsibleRegion region)
        {
            if (!EnableCodeFolding) { region = null; return false; }
            int line = LineIndexAt(position);
            return _foldsByStartLine.TryGetValue(line, out region) && region.IsCollapsed &&
                position == GetLineEndAtLine(line);
        }

        internal bool TryGetCollapsedRegionBeforePosition(int position, out CollapsibleRegion region)
        {
            if (!EnableCodeFolding) { region = null; return false; }
            int line = LineIndexAt(position);
            CollapsibleRegion nearest = null;
            if (position == GetLineStart(line))
            {
                foreach (var candidate in _collapsibles)
                {
                    if (candidate.IsCollapsed && candidate.EndLineExclusive == line &&
                        _visibleLines[_documentToVisibleRow[candidate.StartLine]] == candidate.StartLine &&
                        (nearest == null || candidate.StartLine > nearest.StartLine))
                    {
                        nearest = candidate;
                    }
                }
            }
            if (nearest != null) { region = nearest; return true; }
            region = null;
            return false;
        }

        internal int PositionAfterCollapsedRegion(CollapsibleRegion region)
        {
            return region.EndLineExclusive < LineCount ? GetLineStart(region.EndLineExclusive) : -1;
        }

        internal bool TryGetVisibleCollapsedRegionContainingLine(int line, out CollapsibleRegion region)
        {
            if (!EnableCodeFolding) { region = null; return false; }
            int row = VisibleRowForDocumentLine(line);
            int visibleLine = DocumentLineAtVisibleRow(row);
            if (visibleLine != line && _foldsByStartLine.TryGetValue(visibleLine, out region) &&
                region.IsCollapsed && line > region.StartLine && line < region.EndLineExclusive)
                return true;
            region = null;
            return false;
        }
        private struct FoldSnapshot
        {
            public int StartLine;
            public int EndLineExclusive;
            public bool IsCollapsed;
            public FoldSnapshot(int startLine, int endLineExclusive, bool isCollapsed)
            { StartLine = startLine; EndLineExclusive = endLineExclusive; IsCollapsed = isCollapsed; }
        }
        internal int LineIndexAt(int position)
        {
            int bounded = Math.Max(0, Math.Min(Text.Length, position));
            int found = _lineStarts.BinarySearch(bounded);
            return found >= 0 ? found : Math.Max(0, ~found - 1);
        }
        internal int GetLineStart(int lineIndex) { return _lineStarts[Math.Max(0, Math.Min(_lineStarts.Count - 1, lineIndex))]; }

        private void RebuildLineIndex()
        {
            _lineStarts.Clear();
            _lineStarts.Add(0);
            string text = Text ?? string.Empty;
            for (int i = 0; i < text.Length; i++)
                if (text[i] == '\n') _lineStarts.Add(i + 1);
            _lineCache = text.Split('\n');
            RebuildVisibleLineMap();
        }

        private void AdjustFoldRegionsForEdit(string oldText, int editStart, int removedLength, string insertion)
        {
            int editEnd = editStart + removedLength;
            int lineDelta = CountLineBreaks(insertion) - CountLineBreaks(oldText, editStart, removedLength);
            var regions = _collapsibles.Items;
            for (int i = regions.Count - 1; i >= 0; i--)
            {
                CollapsibleRegion region = regions[i];
                int regionStart = _lineStarts[Math.Max(0, Math.Min(_lineStarts.Count - 1, region.StartLine))];
                int regionEnd = region.EndLineExclusive < _lineStarts.Count
                    ? _lineStarts[region.EndLineExclusive] : oldText.Length;
                if (editEnd <= regionStart)
                {
                    int shiftedStart = Math.Max(0, region.StartLine + lineDelta);
                    region.SetRangeWithoutNotification(shiftedStart,
                        Math.Max(shiftedStart + 2, region.EndLineExclusive + lineDelta));
                }
                else if (editStart >= regionStart && editStart < regionEnd && editEnd <= regionEnd)
                {
                    region.SetRangeWithoutNotification(region.StartLine,
                        Math.Max(region.StartLine + 2, region.EndLineExclusive + lineDelta));
                }
                else if (editStart < regionEnd && editEnd > regionStart)
                {
                    regions.RemoveAt(i);
                }
            }
        }

        private static int CountLineBreaks(string value, int start, int length)
        {
            int end = Math.Min(value.Length, start + length), count = 0;
            for (int i = Math.Max(0, start); i < end; i++) if (value[i] == '\n') count++;
            return count;
        }

        private static int CountLineBreaks(string value) { return CountLineBreaks(value ?? string.Empty, 0, value == null ? 0 : value.Length); }

        private List<FoldSnapshot> CaptureFoldSnapshots()
        {
            var result = new List<FoldSnapshot>(_collapsibles.Count);
            foreach (var region in _collapsibles)
                result.Add(new FoldSnapshot(region.StartLine, region.EndLineExclusive, region.IsCollapsed));
            return result;
        }

        private void RestoreFoldSnapshots(List<FoldSnapshot> snapshots)
        {
            var regions = new List<CollapsibleRegion>();
            if (snapshots != null)
            {
                foreach (var snapshot in snapshots)
                {
                    var region = new CollapsibleRegion(snapshot.StartLine + 1, snapshot.EndLineExclusive, OnCollapsiblesChanged);
                    region.SetCollapsedWithoutNotification(snapshot.IsCollapsed && EnableCodeFolding);
                    regions.Add(region);
                }
            }
            _collapsibles.ReplaceAll(regions);
        }

        private void OnCollapsiblesChanged()
        {
            RebuildVisibleLineMap();
            NormalizeSelectionAnchorToVisibleLine();
            int caretLine = LineIndexAt(_caret);
            if (_caretAtFoldPlaceholder && !IsFoldCollapsedAtLine(caretLine))
                _caretAtFoldPlaceholder = false;
            int anchorLine = LineIndexAt(_selectionAnchor);
            if (_selectionAnchorAtFoldPlaceholder && !IsFoldCollapsedAtLine(anchorLine))
                _selectionAnchorAtFoldPlaceholder = false;
            int visibleRow = VisibleRowForDocumentLine(caretLine);
            if (DocumentLineAtVisibleRow(visibleRow) != caretLine)
            {
                _caret = GetLineEndAtLine(DocumentLineAtVisibleRow(visibleRow));
                _selectionAnchor = _caret;
                _caretAtFoldPlaceholder = false;
                _selectionAnchorAtFoldPlaceholder = false;
                _preferredColumn = -1;
                EnsureCaretVisible();
            }
            InvalidateEditor();
            PublishSelectionState();
        }

        private void NormalizeSelectionAnchorToVisibleLine()
        {
            int anchorLine = LineIndexAt(_selectionAnchor);
            int visibleAnchorRow = VisibleRowForDocumentLine(anchorLine);
            int visibleAnchorLine = DocumentLineAtVisibleRow(visibleAnchorRow);
            if (visibleAnchorLine == anchorLine) return;
            _selectionAnchor = GetLineEndAtLine(visibleAnchorLine);
            _selectionAnchorAtFoldPlaceholder = IsFoldCollapsedAtLine(visibleAnchorLine);
        }

        private void RebuildVisibleLineMap()
        {
            _foldsByStartLine.Clear();
            foreach (var region in _collapsibles)
                if (region.StartLine >= 0 && region.StartLine < _lineCache.Length &&
                    Math.Min(region.EndLineExclusive, _lineCache.Length) > region.StartLine + 1)
                    _foldsByStartLine[region.StartLine] = region;
            var visible = new List<int>();
            var starts = new List<int>();
            var lengths = new List<int>();
            _documentToVisibleRow = new int[_lineCache.Length];
            int line = 0;
            while (line < _lineCache.Length)
            {
                int firstRow = visible.Count;
                _documentToVisibleRow[line] = firstRow;
                AddVisualRowsForLine(line, visible, starts, lengths);
                CollapsibleRegion region;
                if (EnableCodeFolding && _foldsByStartLine.TryGetValue(line, out region) && region.IsCollapsed)
                {
                    int endExclusive = Math.Min(region.EndLineExclusive, _lineCache.Length);
                    for (int hidden = line + 1; hidden < endExclusive; hidden++) _documentToVisibleRow[hidden] = firstRow;
                    line = endExclusive;
                }
                else line++;
            }
            _visibleLines = visible.ToArray();
            _visibleRowStartColumns = starts.ToArray();
            _visibleRowLengths = lengths.ToArray();
            if (_visibleLines.Length == 0)
            {
                _visibleLines = new[] { 0 };
                _visibleRowStartColumns = new[] { 0 };
                _visibleRowLengths = new[] { 0 };
            }
        }

        private void AddVisualRowsForLine(int line, List<int> visible, List<int> starts, List<int> lengths)
        {
            string value = _lineCache[line].TrimEnd('\r');
            if (!IsWordWrapEnabled || double.IsInfinity(_wrapTextWidth) || value.Length == 0)
            {
                visible.Add(line);
                starts.Add(0);
                lengths.Add(value.Length);
                return;
            }

            int column = 0;
            while (column < value.Length)
            {
                bool foldedTailFits = IsFoldCollapsedAtLine(line) &&
                    MeasureTextWidth(value.Substring(column)) + MeasureTextWidth("  …") + FoldMarkerPadding <= _wrapTextWidth;
                double segmentLimit = foldedTailFits
                    ? _wrapTextWidth - MeasureTextWidth("  …") - FoldMarkerPadding
                    : _wrapTextWidth;
                int low = column, high = value.Length;
                while (low < high)
                {
                    int middle = (low + high + 1) / 2;
                    if (MeasureTextWidth(value.Substring(column, middle - column)) <= segmentLimit) low = middle;
                    else high = middle - 1;
                }
                int end = low;
                if (end == column) end++;
                if (end < value.Length)
                {
                    int wordBreak = value.LastIndexOf(' ', end - 1, end - column);
                    if (wordBreak >= column) end = wordBreak + 1;
                }
                visible.Add(line);
                starts.Add(column);
                lengths.Add(end - column);
                column = end;
            }
        }

        internal int VisibleRowForPosition(int position)
        {
            int line = LineIndexAt(position);
            int row = VisibleRowForDocumentLine(line);
            if (DocumentLineAtVisibleRow(row) != line) return row;
            int column = Math.Max(0, position - GetLineStart(line));
            while (row + 1 < VisibleLineCount && DocumentLineAtVisibleRow(row + 1) == line &&
                column >= VisibleRowStartColumn(row) + VisibleRowLength(row))
                row++;
            return row;
        }

        internal void ToggleFoldAtVisibleRow(int row)
        {
            if (!EnableCodeFolding) return;
            int line = DocumentLineAtVisibleRow(row);
            CollapsibleRegion region;
            if (!_foldsByStartLine.TryGetValue(line, out region)) return;
            region.IsCollapsed = !region.IsCollapsed;
            int caretLine = LineIndexAt(_caret);
            if (region.IsCollapsed && caretLine > region.StartLine && caretLine < region.EndLineExclusive)
            {
                _caret = GetLineEndAtLine(region.StartLine);
                _selectionAnchor = _caret;
            }
            _surface.InvalidateMeasure();
            _surface.InvalidateVisual();
            EnsureCaretVisible();
        }

        public void ExpandAllFolds()
        {
            bool changed = false;
            foreach (var region in _collapsibles)
                if (region.IsCollapsed) { region.SetCollapsedWithoutNotification(false); changed = true; }
            if (!changed) return;
            _caretAtFoldPlaceholder = false;
            _selectionAnchorAtFoldPlaceholder = false;
            RebuildVisibleLineMap();
            _surface.InvalidateMeasure();
            _surface.InvalidateVisual();
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var editor = (CodeEditor)d;
            string oldText = e.OldValue as string ?? string.Empty;
            bool externalTextChange = editor._pendingFoldRestore == null && editor._pendingEditStart < 0;
            if (externalTextChange)
            {
                editor._secondaryCarets.Clear();
                editor._secondarySelections.Clear();
                editor._isRectangularSelection = false;
            }
            if (editor._pendingFoldRestore != null)
            {
                editor.RestoreFoldSnapshots(editor._pendingFoldRestore);
                editor._pendingFoldRestore = null;
            }
            else if (editor._pendingEditStart >= 0)
            {
                editor.AdjustFoldRegionsForEdit(oldText, editor._pendingEditStart,
                    editor._pendingEditLength, editor._pendingEditInsertion ?? string.Empty);
            }
            editor._pendingEditStart = -1;
            editor._pendingEditLength = 0;
            editor._pendingEditInsertion = null;
            editor.RebuildLineIndex();
            editor.UpdateWrapWidth();
            editor._caret = Math.Min(editor._caret, editor.Text.Length);
            editor._selectionAnchor = Math.Min(editor._selectionAnchor, editor.Text.Length);
            int caretLine = editor.LineIndexAt(editor._caret);
            if (editor._caretAtFoldPlaceholder && !editor.IsFoldCollapsedAtLine(caretLine))
                editor._caretAtFoldPlaceholder = false;
            int anchorLine = editor.LineIndexAt(editor._selectionAnchor);
            if (editor._selectionAnchorAtFoldPlaceholder && !editor.IsFoldCollapsedAtLine(anchorLine))
                editor._selectionAnchorAtFoldPlaceholder = false;
            editor.NormalizeSelectionAnchorToVisibleLine();
            int mappedRow = editor.VisibleRowForDocumentLine(caretLine);
            if (editor.DocumentLineAtVisibleRow(mappedRow) != caretLine)
            {
                bool expanded = false;
                foreach (var region in editor._collapsibles)
                {
                    if (region.IsCollapsed && caretLine > region.StartLine && caretLine < region.EndLineExclusive)
                    { region.SetCollapsedWithoutNotification(false); expanded = true; }
                }
                if (expanded) editor.RebuildVisibleLineMap();
                else
                {
                    editor._caret = editor.GetLineEndAtLine(editor.DocumentLineAtVisibleRow(mappedRow));
                    editor._selectionAnchor = editor._caret;
                    editor._caretAtFoldPlaceholder = false;
                    editor._selectionAnchorAtFoldPlaceholder = false;
                }
            }
            editor.InvalidateEditor();
            editor.PublishSelectionState();
        }

        private static void OnVisualOptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var editor = (CodeEditor)d;
            if (e.Property == FontFamilyProperty || e.Property == FontSizeProperty || e.Property == ShowLineNumbersProperty)
            {
                editor._textWidthCache.Clear();
                editor.UpdateWrapWidth();
                editor.RebuildVisibleLineMap();
            }
            editor.InvalidateEditor();
        }

        private static void OnFeatureOptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var editor = (CodeEditor)d;
            if (e.Property == EnableCodeFoldingProperty)
            {
                if (!editor.EnableCodeFolding)
                {
                    foreach (var region in editor._collapsibles)
                        region.SetCollapsedWithoutNotification(false);
                    editor._caretAtFoldPlaceholder = false;
                    editor._selectionAnchorAtFoldPlaceholder = false;
                }
                editor._textWidthCache.Clear();
                editor.UpdateWrapWidth();
                editor.RebuildVisibleLineMap();
            }
            if (e.Property == EnableMultipleCaretsProperty && !editor.EnableMultipleCarets)
            {
                editor._secondaryCarets.Clear();
                editor._secondarySelections.Clear();
            }
            if (e.Property == EnableRectangularSelectionProperty && !editor.EnableRectangularSelection)
            {
                editor._isRectangularSelection = false;
                editor._selectionAnchor = editor._caret;
            }
            if (e.Property == EnableEditorContextMenuProperty || e.Property == EnableLineOperationsProperty ||
                e.Property == EnableCodeFoldingProperty)
                editor.InitializeAdvancedUi();
            editor.InvalidateEditor();
            editor.PublishSelectionState();
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == Panel.BackgroundProperty) InvalidateEditor();
            if (e.Property == FlowDirectionProperty) _textWidthCache.Clear();
        }

        private void InvalidateEditor()
        {
            if (_surface == null) return;
            _surface.InvalidateMeasure();
            _surface.InvalidateVisual();
        }

        private void OnEditorViewportSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateWrapWidth();
        }

        private void UpdateWrapWidth()
        {
            if (!IsWordWrapEnabled || _scroll == null) return;
            double width = Math.Max(CharacterWidth, _scroll.ViewportWidth - TextOrigin - 8);
            if (Math.Abs(width - _wrapTextWidth) < 1) return;
            _wrapTextWidth = width;
            RebuildVisibleLineMap();
            InvalidateEditor();
        }

        private static void OnWordWrapChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var editor = (CodeEditor)d;
            editor.UpdateHorizontalScrollBar();
            if (!(bool)e.NewValue) editor._wrapTextWidth = double.PositiveInfinity;
            else editor.UpdateWrapWidth();
            editor.RebuildVisibleLineMap();
            editor.InvalidateEditor();
        }

        private static void OnHorizontalScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((CodeEditor)d).UpdateHorizontalScrollBar();
        }

        private void UpdateHorizontalScrollBar()
        {
            if (_scroll == null) return;
            _scroll.HorizontalScrollBarVisibility = IsWordWrapEnabled || !IsHorizontalScrollEnabled
                ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
        }

        private void OnSyntaxHighlightingChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_surface != null) _surface.InvalidateVisual();
        }

        private struct EditSnapshot
        {
            public string Text;
            public int Caret;
            public int Anchor;
            public List<FoldSnapshot> Folds;
            public List<int> SecondaryCarets;
            public List<SelectionSpan> SecondarySelections;
            public bool IsRectangularSelection;
            public int RectangleAnchorLine;
            public int RectangleAnchorColumn;
            public int RectangleEndLine;
            public int RectangleEndColumn;
            public bool CaretAtFoldPlaceholder;
            public bool SelectionAnchorAtFoldPlaceholder;

            public EditSnapshot(string text, int caret, int anchor, List<FoldSnapshot> folds,
                List<int> secondaryCarets, List<SelectionSpan> secondarySelections,
                bool isRectangularSelection, int rectangleAnchorLine, int rectangleAnchorColumn,
                int rectangleEndLine, int rectangleEndColumn, bool caretAtFoldPlaceholder,
                bool selectionAnchorAtFoldPlaceholder)
            {
                Text = text; Caret = caret; Anchor = anchor; Folds = folds;
                SecondaryCarets = secondaryCarets; SecondarySelections = secondarySelections;
                IsRectangularSelection = isRectangularSelection;
                RectangleAnchorLine = rectangleAnchorLine; RectangleAnchorColumn = rectangleAnchorColumn;
                RectangleEndLine = rectangleEndLine; RectangleEndColumn = rectangleEndColumn;
                CaretAtFoldPlaceholder = caretAtFoldPlaceholder;
                SelectionAnchorAtFoldPlaceholder = selectionAnchorAtFoldPlaceholder;
            }
        }
    }
}
