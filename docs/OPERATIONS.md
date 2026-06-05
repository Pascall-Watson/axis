# Excel Exporter Importer Operations Guide

## Scope

This guide covers staged rollout and support of the pyRevit extension deployment introduced in Sprint 5. The extension entrypoint is the Python workflow.

## Pilot rollout

Use a conservative rollout sequence.

1. Build and stage the extension with `./scripts/Build-PyRevitHybrid.ps1 -Configuration Release`.
2. Confirm the staged payloads exist under `axis/axis.extension/bin/Revit2024`, `axis/axis.extension/bin/Revit2025`, `axis/axis.extension/bin/Revit2026`, and `axis/axis.extension/bin/Revit2027`.
3. Pick a small pilot group with known-good test models and one support contact who can collect logs.
4. Communicate rollback steps (restore staged payload + reload pyRevit) before pilot starts.
5. Require the pilot group to report the operation ID shown in the result dialog for any failure or unexpected skip.

## Rollback

Rollback is file-based and does not require code changes.

1. Close Revit.
2. Restore the last known-good staged payload under `axis/axis.extension/bin/Revit20xx` for the affected version.
3. Reload pyRevit.
4. If the Python workflow still appears unstable, keep the rolled-back staged payload active until investigation is complete.
5. Keep the failed payload available for investigation until the support logs have been collected.

## Log collection

Collect both log files from `%APPDATA%\Pascall-Watson\Axis`.

1. `python-ui.log`
2. `backend.log`

Ask the user for:

1. Revit version.
2. Which Python workflow operation they ran (export schedules or import workbook).
3. The operation ID shown in the result dialog or error alert.
4. The workbook path or export target path involved.

The log format is `key="value"`. Search for `operationId="..."` first, then review adjacent `event="..."` lines.

## Troubleshooting

### DLL not found or assembly load failure

1. Rebuild with `./scripts/Build-PyRevitHybrid.ps1 -Configuration Release`.
2. Confirm the matching staged DLL exists under the extension `bin` folder for the active Revit version:
   - `Revit2024/net481/ExcelExporterImporter.dll`
   - `Revit2025/net8.0-windows/ExcelExporterImporter.dll`
   - `Revit2026/net8.0-windows/ExcelExporterImporter.dll`
   - `Revit2027/net10.0-windows/ExcelExporterImporter.dll`
3. Reload pyRevit.
4. If the issue persists, collect `python-ui.log` and confirm the searched paths in the alert match the expected staged payload.

### Import failed because workbook is locked

1. Close Excel or any other process holding the workbook open.
2. Rerun the import.
3. If the workflow still fails, collect both logs and the operation ID.

### Partial success with warnings or skipped items

1. Review the result dialog counts first.
2. Search both logs for the operation ID.
3. Check whether the skipped items were missing from the workbook, read-only by design, or absent from the active Revit model.
4. If production work is blocked, restore the previous staged payload and reload pyRevit while investigating.

## Regression checks

Run these checks for every release candidate.

1. Python workflow opens and completes one schedule export.
2. Python workflow completes one import from a workbook exported by the tool.
3. Failure alert shows support log paths when the staged DLL is missing.
4. Both `python-ui.log` and `backend.log` receive new entries.

## Automated tests and limits

Automated coverage in this repo is intentionally limited to pure support and result-formatting logic that does not require a live Revit process or Revit API document.

What is automated:

1. Structured log message formatting.
2. Operation result metadata behavior for cancellation and support context display fields.

What remains manual:

1. Revit document inventory and workbook inspection against live models.
2. Export and import workflows that require Revit API transactions.
3. pyRevit UI interaction and dialog behavior.

This split is deliberate. It keeps the automated tests stable in CI while leaving Revit-hosted behavior in the documented manual regression pack.
