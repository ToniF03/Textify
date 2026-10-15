# Textify Documentation

Welcome to the documentation for **Textify**, a custom WPF editor control. Its public type is `Textify.CodeEditor`. The guides cover adding the control to a host application, choosing editor capabilities, and understanding the source.

## 📖 Guides

### Getting started

- [Installation](installation.md) — build the standalone library and add it to a WPF application.
- [Quick Start](quick-start.md) — create an editor, bind text, and enable selected features.

### Features and reference

- [Feature guide](features.md) — understand the optional editor features and their interactions.
- [API Reference](reference/api-reference.md) — public properties, methods, events, and code-folding API.
- [Keyboard Shortcuts](reference/keyboard-shortcuts.md) — built-in commands, gestures, and feature flags.

### Development and support

- [Architecture](development/architecture.md) — source files, component responsibilities, and rendering/input flow.
- [Troubleshooting](help/troubleshooting.md) — solve common build, XAML, binding, and editor behavior issues.

## 🧭 Suggested reading order

1. Follow [Installation](installation.md) to reference the assembly.
2. Use [Quick Start](quick-start.md) to add the control to a view.
3. Consult the [Feature guide](features.md) and [API Reference](reference/api-reference.md) to configure it.
4. Read [Architecture](development/architecture.md) if you plan to modify or extend the control.

## 📌 Scope

The control is a custom-rendered WPF `Grid`, not a `TextBox` subclass. The documentation describes the APIs in this project as they currently exist. Search and replace are not included.
