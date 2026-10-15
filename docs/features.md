# Feature Guide

The control keeps optional editor behavior opt-in. Set the relevant property on the control or in XAML to enable it.

## Editing basics

Text entry, caret movement, mouse-based selection, standard clipboard actions, undo, and redo are part of the core control. `IsReadOnly` blocks edits while leaving navigation and selection available. `Text` can be set or bound by the host application.

The document is represented as a .NET string. Newline input from keyboard and clipboard is normalized to line feeds (`\n`) internally. When reading or setting `Text` from a host, account for newline conventions if the host requires CRLF.

## Line numbers and appearance

Set `ShowLineNumbers` to display one-based document line numbers. Hovering a number shows the standard arrow cursor. Clicking it selects and highlights the complete line; Shift-click extends the selection by line. `LineNumberBackground` and `LineNumberForeground` control the gutter colors. Folding also requires a gutter, so it is shown when code folding is enabled even if line-number labels are off.

`FontFamily`, `FontSize`, `Foreground`, and `CurrentLineBackground` configure text and current-line visuals. `HighlightCurrentLine` enables the current-line background.

## Scrolling and wrapping

| Property | Effect |
| --- | --- |
| `IsWordWrapEnabled` | Wraps long lines to the editor viewport. |
| `IsHorizontalScrollEnabled` | Allows horizontal scrolling for long lines. |
| `EnableWordWrapShortcut` | Enables `Alt+Z` to toggle word wrap. |

Word wrap and horizontal scrolling are independent options. Choose the combination that suits the host editor. The optional context menu includes toggles for wrap and horizontal scrolling when the context menu is enabled.

## Code folding

Folding ranges are supplied by the host application; the control does not parse source code to discover them. Enable folding and add one-based, inclusive ranges:

```csharp
Editor.EnableCodeFolding = true;
var region = Editor.Collapsibles.Add(3, 8);
region.IsCollapsed = true;  // collapse it from code
region.IsCollapsed = false; // expand it from code
```

The range must contain at least two lines. Its first line is the header and remains visible; the remaining lines through `To` are hidden while collapsed. The range begins expanded when added. Click the chevron in the gutter to toggle it, or call `ExpandAllFolds()` to expand every region.

Regions may be nested, but overlapping ranges must be fully nested; partially overlapping ranges are rejected. Only one region may start on a given line. `Add` throws `ArgumentOutOfRangeException` for invalid line numbers/ranges and `InvalidOperationException` for duplicate starts or invalid overlap.

When an edit occurs before a region, the range is shifted to account for inserted or deleted lines. Edits wholly inside the region adjust its end. An edit that crosses a region boundary removes that region. If the host replaces `Text` from outside the control, review and rebuild the host-provided ranges as needed.

The placeholder (`…`) represents hidden text. The caret can move to the placeholder boundary, then continue after the folded range; the hidden lines themselves are not visited by normal caret navigation while the region is collapsed. A selection spanning a folded range can visibly include the fold header and marker.

## Syntax and bracket highlighting

Syntax highlighting is disabled by default. Set `EnableSyntaxHighlighting` to true and add one or more `SyntaxHighlightRule` entries to `SyntaxHighlighting` to color text. Textify applies no built-in Markdown or language-specific syntax colors, and an empty rule collection produces no syntax highlighting.

```csharp
Editor.EnableSyntaxHighlighting = true;
Editor.SyntaxHighlighting.Add(
    new SyntaxHighlightRule(@"\b(class|public|return)\b", Brushes.MediumVioletRed));
```

Patterns are .NET regular expressions evaluated against each line. Rules set the matched text's foreground brush only. Add rules before or after changing the enable flag as convenient; the collection is observable and refreshes the view when changed.

`EnableBracketHighlighting` highlights a matching bracket pair around the caret. Supported bracket types are `()`, `[]`, and `{}`. This visual feature is independent of `AutoCloseBrackets`.

## Automatic typing helpers

| Property | Behavior when enabled |
| --- | --- |
| `AutoIndent` | Enter inserts a line break followed by the current line's leading indentation. |
| `AutoCloseBrackets` | Typing `(`, `[`, or `{` inserts its closing pair and leaves the caret between them. Typing the existing closing character advances past it. |
| `EnableTabIndentation` | Tab inserts four spaces; Tab/Shift+Tab can indent or outdent selected lines. |

## Multiple carets and rectangular selection

Set `EnableMultipleCarets` to enable `Ctrl+D` next-occurrence selection, `Ctrl+Alt+Up/Down` adjacent visible-line carets, and `Ctrl+Alt+left-click` to add a caret. The API also exposes `AddCaret`, `ClearSecondaryCarets`, `SecondaryCaretOffsets`, and `SelectNextOccurrence()`.

Set `EnableRectangularSelection` to enable Alt+Shift+drag rectangular selection. Multiple-caret and rectangular-selection state is included in selection reporting and editing behavior; host code can read `IsRectangularSelection` and `GetSelectedText()`.

## Line operations and context menu

`EnableLineOperations` enables line comment toggling and moving the current line with shortcuts. The comment command prefixes selected/current lines with `//` or removes an existing leading `//` convention; it is a simple text operation, not a language-aware comment service.

`EnableEditorContextMenu` enables the built-in right-click menu. It includes Copy, Copy line, wrap and horizontal-scroll toggles, and—when their respective options are enabled—line commands and Expand all regions. Disabling the option removes the built-in menu.

## Optional features at a glance

All rows below default to `false`:

| Property | Capability |
| --- | --- |
| `ShowLineNumbers` | Numbered gutter labels |
| `IsWordWrapEnabled` | Wrap long text lines |
| `IsHorizontalScrollEnabled` | Horizontal scrolling |
| `HighlightCurrentLine` | Highlight the caret's line |
| `AutoIndent` | Copy leading indentation after Enter |
| `AutoCloseBrackets` | Insert matching bracket pairs |
| `EnableCodeFolding` | Render and interact with host-defined fold regions |
| `EnableSyntaxHighlighting` | Apply only configured custom regex rules |
| `EnableBracketHighlighting` | Highlight matching bracket characters |
| `EnableMultipleCarets` | Enable extra carets and next-occurrence selection |
| `EnableRectangularSelection` | Enable Alt+Shift drag selection |
| `EnableEditorContextMenu` | Show the built-in context menu |
| `EnableLineOperations` | Enable line movement and comment shortcuts/menu entries |
| `EnableWordWrapShortcut` | Enable Alt+Z wrap toggle |
| `EnableTabIndentation` | Intercept Tab for four-space indentation |
