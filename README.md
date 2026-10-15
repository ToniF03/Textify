# Textify

![Platform](https://img.shields.io/badge/platform-Windows-blue)
![UI](https://img.shields.io/badge/UI-WPF-512BD4)
![Target framework](https://img.shields.io/badge/.NET%20Framework-4.7.2-purple)

**Textify** is a small, self-contained text editor control for Windows desktop applications built with WPF. Its public control type is `Textify.CodeEditor`. It provides a custom-rendered editing surface with keyboard and mouse editing, selection, undo/redo, scrolling, and optional editor features.

> **This is a WPF `Grid` control, not a subclass of `System.Windows.Controls.TextBox`.** It uses a custom drawing surface and implements its own editor behavior. It is designed for Windows WPF applications targeting .NET Framework 4.7.2 or a compatible framework.

## ✨ Features

- Text editing with caret navigation, mouse selection, clipboard operations, and undo/redo
- Optional line numbers and configurable gutter colors
- Optional word wrap, horizontal scrolling, and current-line highlighting
- Code folding with ranges provided by the host application
- Optional regular-expression foreground rules configured by the host; no built-in syntax colors
- Matching-bracket highlighting and optional automatic bracket closing
- Optional multiple carets, rectangular selection, line operations, and context menu
- WPF dependency properties for XAML, styles, and data binding

All optional editor features are **off by default**. Enable only the behavior your application needs. Search and replace are not part of this control.

## 🚀 Quick start

Open [`Textify.sln`](Textify.sln) in Visual Studio and build the **Textify** project. Add a project reference to your WPF application, then use the control in XAML:

```xml
<Window
    ...
    xmlns:editor="clr-namespace:Textify;assembly=Textify">
    <Grid>
        <editor:CodeEditor x:Name="Editor" />
    </Grid>
</Window>
```

Enable the features needed by the host application in code:

```csharp
Editor.ShowLineNumbers = true;
Editor.AutoIndent = true;
Editor.EnableCodeFolding = true;
Editor.Collapsibles.Add(3, 8); // 1-based, inclusive line numbers
```

See the [Quick Start](docs/quick-start.md) and [Installation](docs/installation.md) for complete setup instructions.

## 📚 Documentation

| Guide | What it covers |
| --- | --- |
| [Documentation index](docs/README.md) | All guides and recommended reading order |
| [Installation](docs/installation.md) | Build the library and reference it from another WPF project |
| [Quick Start](docs/quick-start.md) | XAML, data binding, feature setup, and common code examples |
| [Features](docs/features.md) | Optional behavior, folding, highlighting, selection, and editing |
| [API Reference](docs/reference/api-reference.md) | Public properties, methods, events, and range rules |
| [Keyboard Shortcuts](docs/reference/keyboard-shortcuts.md) | Built-in key bindings and the options that enable them |
| [Architecture](docs/development/architecture.md) | Source layout, component responsibilities, and rendering flow |
| [Troubleshooting](docs/help/troubleshooting.md) | Common integration and usage issues |

## 🧰 Requirements

- Windows
- Visual Studio with WPF and .NET Framework 4.7.2 targeting components installed
- A WPF host application targeting .NET Framework 4.7.2 or a compatible framework

The library has no third-party package dependencies. The solution is a Visual Studio solution and the project targets .NET Framework 4.7.2.

## 📦 Project layout

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
├── Properties/AssemblyInfo.cs
└── docs/
```

For a detailed file-by-file explanation, see [Architecture](docs/development/architecture.md).

## 🛠️ Build from source

1. Clone or download this repository.
2. Open `Textify.sln` in Visual Studio.
3. Select the **Textify** project and build the desired configuration.
4. Reference the project or the generated `Textify.dll` from your WPF application.

The generated assembly name is `Textify.dll`. The CLR namespace used by XAML is `Textify`.

## 🤝 Contributing

Keep the library independent from its host application. Public behavior should be configurable from the host, and new optional editor features should remain disabled by default unless they are necessary for core editing. Please update the relevant guide when changing a public property, method, gesture, or default.

## 📄 License

Textify is licensed under the [MIT License](LICENSE).

## 🙋 Author and support

Created by [ToniF03](https://github.com/ToniF03). For questions or issues, use the repository where this project is published.
