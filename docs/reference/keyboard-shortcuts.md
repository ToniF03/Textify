# Keyboard Shortcuts

The core editing keys work without enabling optional features. Feature-specific commands require their associated option to be enabled.

## Core commands

| Shortcut | Action |
| --- | --- |
| `Ctrl+A` | Select all text |
| `Ctrl+C` | Copy selection |
| `Ctrl+X` | Cut selection |
| `Ctrl+V` | Paste text |
| `Ctrl+Z` | Undo |
| `Ctrl+Y` | Redo |
| `Ctrl+Home` / `Ctrl+End` | Move to document start / end |
| `Home` / `End` | Move to current line start / end |
| `Left` / `Right` | Move caret horizontally |
| `Up` / `Down` | Move caret vertically through visible rows |
| `PageUp` / `PageDown` | Scroll and move approximately one viewport |
| `Backspace` / `Delete` | Delete before / after the caret |
| `Enter` | Insert a line break |

Holding Shift extends the selection for navigation commands. Holding Ctrl with Left/Right moves by word.

## Optional commands

| Shortcut or gesture | Required option | Action |
| --- | --- | --- |
| `Tab` / `Shift+Tab` | `EnableTabIndentation` | Insert four spaces, or indent/outdent selected lines |
| `Ctrl+/` | `EnableLineOperations` | Toggle line comment |
| `Ctrl+Shift+7` | `EnableLineOperations` | Toggle line comment on keyboard layouts where `/` uses Shift+7 |
| `Ctrl+Shift+Up` / `Ctrl+Shift+Down` | `EnableLineOperations` | Move current line |
| `Alt+Z` | `EnableWordWrapShortcut` | Toggle word wrap |
| `Ctrl+D` | `EnableMultipleCarets` | Select the next occurrence |
| `Ctrl+Alt+Up` / `Ctrl+Alt+Down` | `EnableMultipleCarets` | Add a caret on the adjacent visible row |
| `Ctrl+Alt+left-click` | `EnableMultipleCarets` | Add a caret at the clicked position |
| `Alt+Shift+left-drag` | `EnableRectangularSelection` | Create a rectangular selection |

Fold chevrons are toggled by clicking them. When `EnableEditorContextMenu` is true, right-click opens the built-in menu; some entries also depend on `EnableLineOperations` and `EnableCodeFolding`.

## Key mapping notes

- `Ctrl+/` uses WPF's `Key.OemQuestion` mapping, which can vary with keyboard layout. `Ctrl+Shift+7` is provided as an alternate mapping.
- `Ctrl+D` selects the next match of the current primary selection; it does not start a search panel.
- All optional shortcut properties are off by default. See the [Feature Guide](../features.md) for configuration examples.
