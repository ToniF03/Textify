# Installation

**Textify** is distributed as a Visual Studio project in this repository. It does not currently define a NuGet package.

## Requirements

- Windows with WPF support
- Visual Studio with the .NET Framework 4.7.2 targeting pack
- A WPF application targeting .NET Framework 4.7.2 or a compatible framework

The library references only framework assemblies: `System`, `System.Core`, `System.Xaml`, `WindowsBase`, `PresentationCore`, and `PresentationFramework`.

## Build the standalone library

1. Open [`Textify.sln`](../Textify.sln) in Visual Studio.
2. Build the **Textify** project.
3. Find the output at `bin/Debug/Textify.dll` or `bin/Release/Textify.dll`, depending on the selected configuration.

You can also build from a Visual Studio Developer PowerShell:

```powershell
MSBuild.exe .\Textify.sln /t:Build /p:Configuration=Release
```

## Add a project reference

In Visual Studio, right-click the WPF application project, choose **Add > Project Reference**, and select the **Textify** project. A project reference is convenient while developing because changes to the control are built with the host application.

Alternatively, choose **Add > Reference > Browse** and select the generated `Textify.dll`. A DLL reference is useful when the control is built and distributed separately.

## Use the control in XAML

Declare the CLR namespace and assembly in the root element of your view:

```xml
xmlns:editor="clr-namespace:Textify;assembly=Textify"
```

Then add the control to a layout container:

```xml
<Grid>
    <editor:CodeEditor x:Name="Editor" />
</Grid>
```

The control measures itself to the available layout space. A parent `Grid` row or column should normally use `Height="*"` or `Width="*"` if the editor is expected to fill the remaining area.

## Namespace details

| Item | Value |
| --- | --- |
| CLR namespace | `Textify` |
| Assembly name | `Textify` |
| Control type | `CodeEditor` |
| Base class | `System.Windows.Controls.Grid` |
| Target framework | .NET Framework 4.7.2 |

## Include the documentation

Markdown files are part of the source repository. They are not embedded into the assembly. The API reference is available at [`docs/reference/api-reference.md`](reference/api-reference.md).
