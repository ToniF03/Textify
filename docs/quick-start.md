# Quick Start

This guide adds the standalone editor to a WPF view, binds its text, and enables a few optional capabilities.

## 1. Add the control

After adding a project or assembly reference, import the control namespace in XAML:

```xml
<Window x:Class="Sample.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:editor="clr-namespace:Textify;assembly=Textify"
        Title="Editor" Height="500" Width="800">
    <Grid>
        <editor:CodeEditor x:Name="Editor" />
    </Grid>
</Window>
```

The control starts with an empty document. Its editing surface uses a white background, Consolas 14, and an I-beam cursor.

## 2. Bind or set the text

`Text` is a WPF dependency property. Set it in code:

```csharp
Editor.Text = "First line\nSecond line";
```

For an editable view-model binding, set the binding mode explicitly:

```xml
<editor:CodeEditor Text="{Binding SourceCode,
                              Mode=TwoWay,
                              UpdateSourceTrigger=PropertyChanged}" />
```

The `Text` dependency property does not set two-way binding as its default metadata, so `Mode=TwoWay` is needed when edits should update the source.

## 3. Enable only the features you need

All optional editor features are disabled by default. A common setup might enable line numbers, automatic indentation, and code folding:

```csharp
public MainWindow()
{
    InitializeComponent();

    Editor.ShowLineNumbers = true;
    Editor.AutoIndent = true;
    Editor.EnableCodeFolding = true;
    Editor.Collapsibles.Add(3, 8);
}
```

`Collapsibles.Add(3, 8)` defines an inclusive range of document lines 3 through 8. The first line remains visible as the fold header. The range starts expanded; click its gutter chevron to collapse it.

## 4. Configure appearance

The control exposes dependency properties, so appearance can be configured in XAML or code:

```xml
<editor:CodeEditor Text="{Binding SourceCode, Mode=TwoWay}"
                   ShowLineNumbers="True"
                   FontFamily="Cascadia Mono"
                   FontSize="14"
                   Foreground="DarkSlateGray"
                   LineNumberBackground="#FFF2F4F7"
                   LineNumberForeground="#FF77808B"
                   HighlightCurrentLine="True"
                   CurrentLineBackground="#FFF5F8FC" />
```

## 5. Add a custom syntax color rule

Custom syntax rules are regular expressions applied to each rendered line when syntax highlighting is enabled:

```csharp
using System.Windows.Media;
using Textify;

Editor.EnableSyntaxHighlighting = true;
Editor.SyntaxHighlighting.Add(
    new SyntaxHighlightRule(@"\b(TODO|FIXME)\b", Brushes.DarkOrange));
```

Rules affect foreground color only. Textify has no built-in Markdown or language-specific syntax colors; only text matched by configured rules is colored.

## 6. Respond to editor changes

```csharp
Editor.CaretPositionChanged += (sender, args) =>
{
    int offset = Editor.CaretOffset;
    // Update host UI, for example a status bar.
};

Editor.SelectionChanged += (sender, args) =>
{
    string selectedText = Editor.GetSelectedText();
};
```

See the [API Reference](reference/api-reference.md) for offsets and all public members, and the [Keyboard Shortcuts](reference/keyboard-shortcuts.md) page for optional gestures.
