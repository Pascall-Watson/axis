# axis Hybrid Extension

This folder contains the pyRevit wrapper for the C# `ExcelExporterImporter` project.

## Build and stage binaries

From repo root:

```powershell
./scripts/Build-PyRevitHybrid.ps1 -Configuration Release
```

If Revit is installed in a custom folder, pass:

```powershell
./scripts/Build-PyRevitHybrid.ps1 -Configuration Release -RevitInstallRootBase "D:\Autodesk"
```

This builds Revit `2025-2026` binaries and copies them into:

- `axis/ExcelExporterImporter.extension/bin/Revit2025`
- `axis/ExcelExporterImporter.extension/bin/Revit2026`

## pyRevit setup

1. Add this repo's `axis` folder as an extension source.
2. Reload pyRevit.
3. Run the `Excel Exporter Importer` button under `Excel Tools` tab.

The Python button loads the matching DLL for the active Revit major version and calls:

`ExcelExporterImporter.Interop.ExcelExporterImporterInterop.ShowMainWindow(doc)`
