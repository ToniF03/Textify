# Troubleshooting

## XAML says the control or namespace cannot be found

Check all three integration values:

```xml
xmlns:editor="clr-namespace:Textify;assembly=Textify"
```

- `Textify` is the CLR namespace.
- `Textify` is the assembly name (without `.dll`).
- The host project must reference the control project or assembly.

If the reference was just added, rebuild the solution and make sure the host is targeting a framework that can reference the .NET Framework 4.7.2 WPF library.

## The text binding does not update my view model

Set `Mode=TwoWay` explicitly:

```xml
Text="{Binding SourceCode, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
```

The `Text` dependency property does not default to two-way binding.

## A folding chevron is missing

Confirm that `EnableCodeFolding` is `true`, then add a valid range whose `From` line exists in `Text`:

```csharp
Editor.EnableCodeFolding = true;
Editor.Collapsibles.Add(3, 8);
```

Line numbers in folding APIs are one-based. The range needs at least two lines and the header is `From`. Folding creates a gutter even when line-number labels are hidden.

## A folding range throws an exception

- `from` must be at least `1`.
- `to` must be greater than `from`.
- Only one range can start on a given line.
- Overlapping regions must be nested completely; partial overlaps are not allowed.

Recreate fold ranges after replacing the full document if the new line structure is different.

## Syntax color rules do not appear

Set `EnableSyntaxHighlighting = true`, add a valid .NET regular expression, and use a non-null WPF `Brush`:

```csharp
Editor.EnableSyntaxHighlighting = true;
Editor.SyntaxHighlighting.Add(
    new SyntaxHighlightRule(@"\bTODO\b", Brushes.OrangeRed));
```

Rules set foreground color only. They are not applied if syntax highlighting is disabled.

## Tab inserts focus navigation instead of spaces

`EnableTabIndentation` defaults to `false`. Set it to `true` to have the editor handle Tab. With the option disabled, WPF's normal Tab focus-navigation behavior can apply.

## A shortcut has no effect

Check the corresponding option in [Keyboard Shortcuts](../reference/keyboard-shortcuts.md). Most editor-specific commands are disabled until their feature option is enabled. The `Ctrl+/` mapping can also vary by keyboard layout; try `Ctrl+Shift+7` for line comment toggling.

## The text is read-only

Check `IsReadOnly`. It defaults to `false`; when set to `true`, text-changing input and edit commands are blocked, but the document remains navigable and selectable.

## Wrapping and horizontal scrolling behave unexpectedly

`IsWordWrapEnabled` and `IsHorizontalScrollEnabled` are separate options. Choose explicitly which behavior the host should expose. If using the context menu, set `EnableEditorContextMenu=true` to show its wrap and horizontal-scroll toggles.

## Report an issue

Include the target framework, Visual Studio version, a minimal XAML/code sample, the feature flags involved, and the exact steps to reproduce the behavior. Do not include private source text or credentials in issue attachments.
