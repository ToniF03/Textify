# Architecture

`CodeEditor` is a self-contained WPF control library. It owns document text, caret and selection state, input handling, folding state, and drawing. It does not depend on any host application and it does not derive from WPF's `TextBox`.

## Technology and design

| Area | Implementation |
| --- | --- |
| UI framework | WPF |
| Target | .NET Framework 4.7.2 |
| Control base | `System.Windows.Controls.Grid` |
| Rendering | Custom `FrameworkElement` rendered with `DrawingContext` and `FormattedText` |
| Input | WPF keyboard, text composition, and mouse events |
| State | Dependency properties plus internal document, selection, undo, and folding state |
| Dependencies | .NET/WPF framework assemblies only |

The control uses a single text string and line-start index for document positions. Visible-row maps translate between document lines and visual rows, including folded and wrapped lines. This lets caret navigation and rendering follow the visual rows while selections continue to use document offsets.

## Source tree

```text
Textify/
├── Textify.sln
├── Textify.csproj
├── CodeEditor.cs
├── CodeEditor.Rendering.cs
├── CodeEditor.Behavior.cs
├── CodeEditor.Advanced.cs
├── Collapsibles.cs
├── SyntaxHighlightRule.cs
├── Properties/
│   └── AssemblyInfo.cs
├── README.md
└── docs/
    ├── README.md
    ├── installation.md
    ├── quick-start.md
    ├── features.md
    ├── reference/
    ├── development/
    └── help/
```

## Responsibilities by file

| File | Responsibility |
| --- | --- |
| `Textify.sln` | Standalone Visual Studio solution containing only the editor library project. |
| `Textify.csproj` | .NET Framework target, WPF references, compiler settings, and source file inclusion. |
| `CodeEditor.cs` | Main control type; dependency properties and defaults; document and line indexes; visible-row mapping; caret/selection state; fold integration; invalidation and event publication. |
| `CodeEditor.Rendering.cs` | Nested `EditorSurface`; measure and render text, line-number gutter, selection, caret, brackets, fold chevrons, and fold placeholder. |
| `CodeEditor.Behavior.cs` | Keyboard/text input; caret movement; selection through mouse and keyboard; text replacement; clipboard; undo/redo; core editing. |
| `CodeEditor.Advanced.cs` | Optional context menu; syntax rules; multiple caret and rectangular selection operations; line movement/comment operations; related selection helpers. |
| `Collapsibles.cs` | Public fold-region model and collection validation for one-based inclusive ranges. |
| `SyntaxHighlightRule.cs` | Public regex-based foreground rule model and compiled matcher. |
| `Properties/AssemblyInfo.cs` | Assembly identity and metadata. |

## Component overview

```text
Host WPF view / view model
          │ Text, options, events
          ▼
Textify (Grid)
  ├── Dependency properties and document state
  ├── Keyboard, text input, and mouse handlers
  ├── Caret, selection, undo/redo, and fold models
  └── ScrollViewer
       └── EditorSurface (FrameworkElement)
            └── DrawingContext / FormattedText
```

The host owns feature configuration and provides fold ranges. The control owns the interactive editor state and renders the visible document. Drawing is custom and retained only as WPF visuals; text remains in the `Text` property.

## Input and update flow

1. WPF delivers a key, text composition, or mouse event to the control or its editor surface.
2. The behavior layer maps it to a caret/selection move or a text edit.
3. For edits, the control records an undo snapshot and updates the `Text` dependency property.
4. The text property callback rebuilds line indexes, adjusts fold ranges where possible, rebuilds visible rows, normalizes positions, and invalidates the surface.
5. The custom surface redraws the visible portion using cached text measurements and the current options.
6. Caret and selection events notify the host when published state changes.

External replacement of `Text` is treated as a document replacement, not as a granular edit operation. The host should keep its code-defined fold ranges in sync after replacing the whole document.

## Rendering and layout

`EditorSurface.MeasureOverride` computes the scrollable extent from visible line rows. With wrapping enabled, rows are split to fit the viewport. Without wrapping, the widest text line determines horizontal extent. `OnRender` draws the background and gutter, then visible text rows, selection spans, bracket and current-line highlights, fold markers, and the caret.

Text measurements are cached by string. The cache is cleared when DPI changes and is bounded by clearing after it grows beyond 4096 entries. The renderer iterates over the visible-row range for drawing, while line and fold maps are rebuilt when document or layout state changes.

## Optional feature design

Feature switches are dependency properties and default to `false`. The render, input, and context-menu paths check these switches before applying optional behavior. This leaves a simple editing control as the default and lets the host choose the editor configuration.

Folding uses explicit `CollapsibleRegion` objects supplied by the host. Ranges are line-based rather than syntax-based. The first line remains visible and the hidden lines map to the visible header row until expanded.

## Build and extension notes

- Open `Textify.sln` to build the standalone library.
- Keep the project independent from any host application.
- Add public API behavior to the API and feature guides when changing the source.
- Keep new optional capabilities off by default and document their enabling property/gesture.
- Avoid describing this control as a WPF `TextBox` subclass: it is a custom `Grid` control.
