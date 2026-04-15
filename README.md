# Axis

## pyRevit Hybrid (Revit 2025-2026)

This repository now includes a hybrid migration scaffold under `axis/`:

- pyRevit extension wrapper: `axis/ExcelExporterImporter.extension`
- Python runtime loader: `axis/ExcelExporterImporter.extension/lib/loader.py`
- pyRevit command button: `axis/ExcelExporterImporter.extension/Excel Tools.tab/Excel Exporter.panel/Launch.pushbutton/script.py`
- Build/staging script: `scripts/Build-PyRevitHybrid.ps1`

The C# project now accepts a `RevitVersion` build property (`2025`, `2026`) and publishes version-specific outputs. pyRevit loads the matching DLL and calls `ExcelExporterImporter.Interop.ExcelExporterImporterInterop.ShowMainWindow(doc)`.

Sprint 1 keeps that behavior unchanged. The pyRevit button is still only a launcher for the existing C# WPF window. There is no Python-owned UI yet.

### Build and stage for pyRevit

```powershell
./scripts/Build-PyRevitHybrid.ps1 -Configuration Release
```

If your Revit installs are not under `%ProgramW6432%\Autodesk`:

```powershell
./scripts/Build-PyRevitHybrid.ps1 -Configuration Release -RevitInstallRootBase "D:\Autodesk"
```

What the staging script does:

1. Builds `ExcelExporterImporter.csproj` once per requested Revit version.
2. Uses the local Revit install folder for API references.
3. Does not deploy a `.addin` into `%AppData%\Autodesk\Revit\Addins`.
4. Copies the build output into `axis/ExcelExporterImporter.extension/bin/Revit2025` and `axis/ExcelExporterImporter.extension/bin/Revit2026`.

To stage only one supported Revit version:

```powershell
./scripts/Build-PyRevitHybrid.ps1 -Configuration Release -RevitVersions 2025
./scripts/Build-PyRevitHybrid.ps1 -Configuration Release -RevitVersions 2026
```

### Production-safe baseline workflow

Use this sequence when validating the hybrid launcher without changing the current WPF workflow.

1. From the repo root, back up the current staged payloads if you already have validated binaries under `axis/ExcelExporterImporter.extension/bin/Revit2025` or `axis/ExcelExporterImporter.extension/bin/Revit2026`.
2. Run the staging script from PowerShell.
3. Confirm the staged DLL exists for each target version:
   - `axis/ExcelExporterImporter.extension/bin/Revit2025/net8.0-windows/ExcelExporterImporter.dll`
   - `axis/ExcelExporterImporter.extension/bin/Revit2026/net8.0-windows/ExcelExporterImporter.dll`
4. Register the repo `axis` folder as a pyRevit extension source if it is not already registered.
5. Reload pyRevit.
6. Open Revit 2025 or Revit 2026 and run `Excel Tools > Excel Exporter > Excel Exporter Importer`.
7. Confirm the existing C# WPF window opens.

### Rollback

If the staged hybrid payload needs to be rolled back:

1. Close Revit.
2. Restore your backup copies of `axis/ExcelExporterImporter.extension/bin/Revit2025` and `axis/ExcelExporterImporter.extension/bin/Revit2026`.
3. If you need to remove the repo extension entirely, unregister the repo `axis` folder from pyRevit or disable that extension source in your pyRevit configuration.
4. Reload pyRevit and reopen Revit.

If you did not create a backup and only want to remove the staged hybrid payload, delete the affected `axis/ExcelExporterImporter.extension/bin/Revit2025` or `axis/ExcelExporterImporter.extension/bin/Revit2026` folder and restage a known-good build.

### Manual smoke tests

Run these tests in both Revit 2025 and Revit 2026.

1. Launcher smoke test
   - Build and stage with `./scripts/Build-PyRevitHybrid.ps1 -Configuration Release`.
   - Reload pyRevit.
   - Open a model with an active document.
   - Click the pyRevit button and confirm the existing WPF dialog opens.

2. Export smoke test
   - In the WPF dialog, select at least one known-good schedule.
   - Export to a temporary `.xlsx` file.
   - Confirm the file is created and opens in Excel.

3. Import smoke test
   - Start from a workbook created by the add-in in bidirectional mode.
   - Change one writable value only.
   - Import the workbook through the same WPF dialog.
   - Confirm the updated value appears in Revit.

4. Failure-path smoke test
   - If the launcher fails, confirm the pyRevit alert reports the DLL path it loaded or the paths it searched, then follow the suggested rebuild and reload steps.

## Description

The Revit add-in Import-Export Excel 2015-2021 allows you to facilitate the management of your data in your digital models by processing it outside of Revit. First, you must export your schedule to an Excel file via the add-in, which allows you to modify your data directly in Excel (or other compatible spreadsheets). Once your information has been modified, all you have to do is import your Excel file via the add-in and your Revit schedule will automatically update with the new data. With this tool, you can ease the data management process by delegating tasks related to the information of digital models to all team members who do not have a Revit license.

As of version 20.1.0.0, a new feature has been added allowing you to export to an Excel file while respecting the layout of the schedule, for viewing purposes only.

## Addin limitations

### Read-only

Some Revit parameters are considered "read-only" when their value is controlled by Revit. You will not be able to modify this type of parameter even if you modify it in the Excel file because you are not authorized to write in these parameters. Read-only parameters are colored gray and locked when exported to Excel so that you can distinguish them from others.

### Import constraints

The import works only with files that have been exported in bidirectional mode using this add-in. The schedules are therefore not all importable.

Once the file has been exported, you can only modify the existing data in the fields generated during the export. Adding rows / columns will not work because they will not be recognized by Revit. However, you can fill in empty fields as long as they belong to a row / column generated by the export tool.
