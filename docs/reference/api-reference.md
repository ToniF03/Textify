# API Reference

Namespace: `Textify`  
Assembly: `Textify`  
Control: `CodeEditor`

The control derives from WPF `Grid` and exposes dependency properties for styling, XAML attributes, and binding. Unless specified otherwise, Boolean options listed below default to `false`.

## Text and appearance

| Member | Type | Default / behavior |
| --- | --- | --- |
| `Text` | `string` | Empty string. Main document contents; `null` assignments are normalized to empty. |
| `IsReadOnly` | `bool` | `false`. Prevents text edits. |
| `FontFamily` | `FontFamily` | `Consolas`. |
| `FontSize` | `double` | `14`. |
| `Foreground` | `Brush` | `DarkSlateGray`. |
| `ShowLineNumbers` | `bool` | `false`. |
| `LineNumberBackground` | `Brush` | `#F7F8FA`. |
| `LineNumberForeground` | `Brush` | `#9199A3`. |
| `HighlightCurrentLine` | `bool` | `false`. |
| `CurrentLineBackground` | `Brush` | `#F6F8FB`. |
| `IsWordWrapEnabled` | `bool` | `false`. |
| `IsHorizontalScrollEnabled` | `bool` | `false`. |

For editable data binding, explicitly use `Mode=TwoWay`; the `Text` property metadata does not request two-way binding by default.

## Optional feature properties

| Member | Type | Default | Enables |
| --- | --- | --- | --- |
| `AutoIndent` | `bool` | `false` | Leading indentation on Enter |
| `AutoCloseBrackets` | `bool` | `false` | Automatic pairing for `()`, `[]`, `{}` |
| `EnableCodeFolding` | `bool` | `false` | Fold chevrons and collapse interaction |
| `EnableSyntaxHighlighting` | `bool` | `false` | Apply configured custom regex rules only |
| `EnableBracketHighlighting` | `bool` | `false` | Matching bracket visualization |
| `EnableMultipleCarets` | `bool` | `false` | Secondary carets and next occurrence |
| `EnableRectangularSelection` | `bool` | `false` | Alt+Shift rectangular mouse selection |
| `EnableEditorContextMenu` | `bool` | `false` | Built-in right-click menu |
| `EnableLineOperations` | `bool` | `false` | Line move/comment commands |
| `EnableWordWrapShortcut` | `bool` | `false` | `Alt+Z` wrap toggle |
| `EnableTabIndentation` | `bool` | `false` | Tab indentation handling |

## Caret and selection

Offsets are zero-based .NET string indices. `SelectionStart` is the lower endpoint; `SelectionLength` is the distance between the caret and selection anchor.

| Member | Type | Description |
| --- | --- | --- |
| `CaretOffset` | `int` | Gets or sets the primary caret offset. |
| `SelectionStart` | `int` | Start of the primary selection. |
| `SelectionLength` | `int` | Length of the primary selection. |
| `SelectionEnd` | `int` | `SelectionStart + SelectionLength`. |
| `HasSelection` | `bool` | True if the primary, secondary, or rectangular selection has content. |
| `IsRectangularSelection` | `bool` | Whether the current selection is rectangular. |
| `SecondaryCaretOffsets` | `IList<int>` | Read-only view of secondary caret offsets. |
| `CaretPositionChanged` | `EventHandler` | Raised when published caret/selection state changes position. |
| `SelectionChanged` | `EventHandler` | Raised when published selection state changes. |

Methods:

```csharp
void Select(int start, int length);
string GetSelectedText();
void AddCaret(int offset);
void ClearSecondaryCarets();
bool SelectNextOccurrence();
```

`Select` uses a zero-based start and a length. `AddCaret` and `SelectNextOccurrence` require `EnableMultipleCarets=true`. `SelectNextOccurrence` returns `false` if there is no primary selection or another occurrence cannot be selected. `GetSelectedText()` joins multiple or rectangular selection pieces with `Environment.NewLine`.

## Syntax highlighting

```csharp
ObservableCollection<SyntaxHighlightRule> SyntaxHighlighting { get; }

public sealed class SyntaxHighlightRule
{
    public SyntaxHighlightRule(string pattern, Brush foreground);
    public Brush Foreground { get; }
}
```

`pattern` is a .NET regular expression matched separately against each line. A null pattern or foreground throws `ArgumentNullException`; an invalid regular expression throws `ArgumentException` from `Regex`. Rules color foreground only and are used when `EnableSyntaxHighlighting` is true.

## Code folding

```csharp
CollapsibleRegionCollection Collapsibles { get; }
CollapsibleRegion Add(int from, int to);
void ExpandAllFolds();
```

`from` and `to` are one-based inclusive line numbers. The header (`from`) remains visible; lines after it through `to` are hidden. A range must contain at least two lines (`to > from`). `Add` begins expanded and returns the region object:

```csharp
var region = Editor.Collapsibles.Add(3, 8);
region.IsCollapsed = true;
int headerLine = region.From;
int lastLine = region.To;
```

`CollapsibleRegion` exposes read-only `From` and `To`, and a mutable `IsCollapsed`. `CollapsibleRegionCollection` exposes `Count`, an indexer, `Add`, `Remove`, `Clear`, and enumeration.

Ranges with a duplicate start line are rejected. Partial overlap is rejected, while nested or disjoint ranges are allowed. `from < 1` or `to <= from` throws `ArgumentOutOfRangeException`; duplicate starts or illegal overlaps throw `InvalidOperationException`.

## WPF dependency properties

All appearance, text, and feature options listed above are dependency properties. They can be set in XAML, bound, styled, or changed from code. `Text` is not configured for two-way binding by default; specify `Mode=TwoWay` when appropriate.
