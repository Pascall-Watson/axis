# Axis

## pyRevit Hybrid (Revit 2024-2027)

This repository now includes a hybrid migration scaffold under `axis/`:

- pyRevit extension wrapper: `axis/axis.extension`
- Python runtime loader: `axis/axis.extension/lib/loader.py`
- pyRevit command button: `axis/axis.extension/axis.tab/axis.panel/pyrevit.pushbutton/script.py`
- Build/staging script: `scripts/Build-PyRevitHybrid.ps1`

The C# backend ships as version-specific projects (`2024`, `2025`, `2026`, `2027`) and publishes versioned outputs for pyRevit staging.

Sprint 4 introduces a Python-owned pyRevit workflow for export/import using the C# interop backend service API.

The extension now ships a single pyRevit command:

- `Excel Exporter Importer` -> Python workflow (export schedules, import workbook)

## Sprint 5: Deployment hardening

Sprint 5 keeps the Python workflow as the extension entrypoint and adds supportability for office rollout:

- Structured support logs for the pyRevit UI and the C# backend under `%APPDATA%\Pascall-Watson\Axis`.
- Operation IDs surfaced in Python result dialogs for faster support triage.
- Clearer summaries for export/import outcomes, including warnings, skips, and log file locations.
- A log4net upgrade from `2.0.3` to `3.3.0` to address the previously flagged vulnerabilities.

The Python workflow writes `python-ui.log`. The C# backend writes `backend.log`. Both use a simple structured `key="value"` format so support staff can search by `operationId`, `event`, `document`, or file path.

See `docs/OPERATIONS.md` for pilot rollout, rollback, troubleshooting, log collection, and regression checks.

---

## Sprint 2: C# Backend Facade (interop boundary)

Sprint 2 adds a clean, WPF-free backend API in
`ExcelExporterImporter/Interop/ExcelExporterImporterInterop.cs`.
Python scripts can call these methods directly via `pythonnet` / `clr.AddReference`
without needing window handles, dialog types, or any WPF assembly.

### New public types (`ExcelExporterImporter.Interop` namespace)

| Type | Purpose |
| --- | --- |
| `ScheduleInfo` | Describes a schedule returned by `GetExportableSchedules` |
| `StandardInfo` | Describes an exportable standard group returned by `GetExportableStandards` |
| `ImportWorkbookItem` | One worksheet in an import workbook (importable or read-only) |
| `ImportWorkbookInspection` | Result of `InspectImportWorkbook` |
| `ExportSchedulesRequest` | Input for `ExecuteExportSchedules` |
| `ExportStandardsRequest` | Input for `ExecuteExportStandards` |
| `ImportRequest` | Input for `ExecuteImport` |
| `OperationResult` | Result returned by all Execute* methods |
| `ProgressInfo` | Progress notification passed to the `onProgress` callback |

### New service methods on `ExcelExporterImporterInterop`

```csharp
// Returns all non-titleblock schedules in the document.
IReadOnlyList<ScheduleInfo> GetExportableSchedules(Document document)

// Returns the fixed list of exportable standard groups.
IReadOnlyList<StandardInfo> GetExportableStandards()

// Opens a workbook read-only and reports which items can be imported.
ImportWorkbookInspection InspectImportWorkbook(Document document, string filePath)

// Exports schedules to an Excel file without showing dialogs.
OperationResult ExecuteExportSchedules(
    Document document,
    ExportSchedulesRequest request,
    Action<ProgressInfo> onProgress,       // pass null to ignore progress
    CancellationToken cancellationToken)

// Exports standard groups to an Excel file without showing dialogs.
OperationResult ExecuteExportStandards(
    Document document,
    ExportStandardsRequest request,
    Action<ProgressInfo> onProgress,
    CancellationToken cancellationToken)

// Imports selected items from a workbook without showing dialogs.
OperationResult ExecuteImport(
    Document document,
    ImportRequest request,
    Action<ProgressInfo> onProgress,
    CancellationToken cancellationToken)
```

The existing `ShowMainWindow(document)` / `ShowMainWindow(document, ownerHandle)` methods
remain available for internal diagnostics and compatibility paths, but no dedicated pyRevit button is shipped for direct WPF launch.

### Calling the backend from Python (example)

```python
import clr
clr.AddReference("ExcelExporterImporter")
from ExcelExporterImporter.Interop import (
    ExcelExporterImporterInterop,
    ExportSchedulesRequest,
)
from System.Threading import CancellationToken

# List schedules
schedules = ExcelExporterImporterInterop.GetExportableSchedules(doc)
for s in schedules:
    print(s.Name, s.UniqueId)

# Export two schedules
request = ExportSchedulesRequest()
request.OutputFilePath = r"C:\temp\my_export.xlsx"
request.ScheduleUniqueIds.Add(schedules[0].UniqueId)
request.ScheduleUniqueIds.Add(schedules[1].UniqueId)

result = ExcelExporterImporterInterop.ExecuteExportSchedules(
    doc, request, None, CancellationToken.None
)
if not result.Success:
    for err in result.Errors:
        print("ERROR:", err)
```

### `StandardInfo.IsReadOnly` flag

Three standard groups are **export-only** and cannot be imported back:

- Family Listing
- Shared Parameters Settings
- Project Parameters Settings

`GetExportableStandards()` returns them with `IsReadOnly = True`. They will also appear
in `ImportWorkbookInspection.ReadOnlyItems` when found in a workbook.

---

### Build and stage for pyRevit

```powershell
./scripts/Build-PyRevitHybrid.ps1 -Configuration Release
```

If your dependency binaries are in a custom location:

```powershell
./scripts/Build-PyRevitHybrid.ps1 -Configuration Release -DependenciesRootBase "D:\RevitDependencies"
```

What the staging script does:

1. Builds the version-specific project (`ExcelExporterImporter.<version>.csproj`) for each requested Revit version.
2. Uses API dependency binaries from the repo `dependencies/<version>` folder.
3. Does not generate or deploy `.addin` manifests.
4. Copies the build output into `axis/axis.extension/bin/Revit20xx`.

To stage only one supported Revit version:

```powershell
./scripts/Build-PyRevitHybrid.ps1 -Configuration Release -RevitVersions 2024
./scripts/Build-PyRevitHybrid.ps1 -Configuration Release -RevitVersions 2025
./scripts/Build-PyRevitHybrid.ps1 -Configuration Release -RevitVersions 2026
./scripts/Build-PyRevitHybrid.ps1 -Configuration Release -RevitVersions 2027
```

### Production-safe baseline workflow

Use this sequence when validating the pyRevit-only launcher.

1. From the repo root, back up the current staged payloads if you already have validated binaries under `axis/axis.extension/bin/Revit2024`, `axis/axis.extension/bin/Revit2025`, `axis/axis.extension/bin/Revit2026`, or `axis/axis.extension/bin/Revit2027`.
2. Run the staging script from PowerShell.
3. Confirm the staged DLL exists for each target version:
    - `axis/axis.extension/bin/Revit2024/net481/ExcelExporterImporter.dll`
    - `axis/axis.extension/bin/Revit2025/net8.0-windows/ExcelExporterImporter.dll`
    - `axis/axis.extension/bin/Revit2026/net8.0-windows/ExcelExporterImporter.dll`
    - `axis/axis.extension/bin/Revit2027/net10.0-windows/ExcelExporterImporter.dll`
4. Register the repo `axis` folder as a pyRevit extension source if it is not already registered.
5. Reload pyRevit.
6. Open Revit 2024, Revit 2025, Revit 2026, or Revit 2027 and run `Excel Tools > Excel Exporter > Excel Exporter Importer`.
7. Confirm the Python workflow opens and you can choose Export Schedules or Import Workbook.
8. Confirm there is no legacy WPF fallback button exposed in the extension UI.

### Rollback

If the staged hybrid payload needs to be rolled back:

1. Close Revit.
2. Restore your backup copies of `axis/axis.extension/bin/Revit2024`, `axis/axis.extension/bin/Revit2025`, `axis/axis.extension/bin/Revit2026`, and `axis/axis.extension/bin/Revit2027`.
3. If you need to remove the repo extension entirely, unregister the repo `axis` folder from pyRevit or disable that extension source in your pyRevit configuration.
4. Reload pyRevit and reopen Revit.

If you did not create a backup and only want to remove the staged hybrid payload, delete the affected `axis/axis.extension/bin/Revit20xx` folder and restage a known-good build.

### Manual smoke tests

Run these tests in each supported version (Revit 2024, Revit 2025, Revit 2026, and Revit 2027).

1. Python workflow smoke test
   - Build and stage with `./scripts/Build-PyRevitHybrid.ps1 -Configuration Release`.
   - Reload pyRevit.
   - Open a model with an active document.
   - Click `Excel Exporter Importer` and confirm the Python workflow chooser appears.

2. Export schedules smoke test
   - In the Python workflow, choose `Export Schedules`.
   - Select at least one known-good schedule and complete export.
   - Confirm the file is created and opens in Excel.

3. Import smoke test
   - Start from a workbook created by the add-in in bidirectional mode.
   - Change one writable value only.
   - In the Python workflow, choose `Import Workbook` and import the updated sheet.
   - Confirm the updated value appears in Revit.

4. Failure-path smoke test
   - If the launcher fails, confirm the pyRevit alert reports the DLL path it loaded or the paths it searched, then follow the suggested rebuild and reload steps.

### Sprint 5 regression checks

Run these checks before widening rollout beyond a pilot group:

1. Confirm `Excel Exporter Importer` still opens the Python chooser.
2. Run one successful schedule export and one successful import. Confirm the result dialog includes a status, requested/succeeded/failed counts, and log file locations.
3. Force one known failure path, such as selecting a locked workbook for import or temporarily removing the staged DLL. Confirm the alert includes next steps and the support log paths.
4. Check `%APPDATA%\Pascall-Watson\Axis\python-ui.log` and `%APPDATA%\Pascall-Watson\Axis\backend.log` for a matching `operationId` on the failed or successful workflow.
5. Validate rollback by restoring a previously known-good staged payload and reloading pyRevit.

## Description

The Revit add-in Import-Export Excel 2015-2021 allows you to facilitate the management of your data in your digital models by processing it outside of Revit. First, you must export your schedule to an Excel file via the add-in, which allows you to modify your data directly in Excel (or other compatible spreadsheets). Once your information has been modified, all you have to do is import your Excel file via the add-in and your Revit schedule will automatically update with the new data. With this tool, you can ease the data management process by delegating tasks related to the information of digital models to all team members who do not have a Revit license.

As of version 20.1.0.0, a new feature has been added allowing you to export to an Excel file while respecting the layout of the schedule, for viewing purposes only.

## Addin limitations

### Read-only

Some Revit parameters are considered "read-only" when their value is controlled by Revit. You will not be able to modify this type of parameter even if you modify it in the Excel file because you are not authorized to write in these parameters. Read-only parameters are colored gray and locked when exported to Excel so that you can distinguish them from others.

### Import constraints

The import works only with files that have been exported in bidirectional mode using this add-in. The schedules are therefore not all importable.

Once the file has been exported, you can only modify the existing data in the fields generated during the export. Adding rows / columns will not work because they will not be recognized by Revit. However, you can fill in empty fields as long as they belong to a row / column generated by the export tool.
