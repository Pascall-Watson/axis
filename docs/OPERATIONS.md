# Excel Exporter Importer Operations Guide

## Scope

This guide covers staged rollout and support of the hybrid pyRevit plus C# deployment introduced in Sprint 5. It assumes the legacy WPF fallback remains available during pilot and early office rollout.

## Pilot rollout

Use a conservative rollout sequence.

1. Build and stage the extension with `./scripts/Build-PyRevitHybrid.ps1 -Configuration Release`.
2. Confirm the staged payloads exist under `axis/ExcelExporterImporter.extension/bin/Revit2025` and `axis/ExcelExporterImporter.extension/bin/Revit2026`.
3. Pick a small pilot group with known-good test models and one support contact who can collect logs.
4. Keep the legacy WPF button visible and communicate that it is the immediate fallback if the Python workflow blocks a task.
5. Require the pilot group to report the operation ID shown in the result dialog for any failure or unexpected skip.

## Rollback

Rollback is file-based and does not require code changes.

1. Close Revit.
2. Restore the last known-good staged payload under `axis/ExcelExporterImporter.extension/bin/Revit2025` or `axis/ExcelExporterImporter.extension/bin/Revit2026`.
3. Reload pyRevit.
4. If the Python workflow still appears unstable, direct users to `Excel Exporter Importer (Legacy WPF)` until the pilot issue is resolved.
5. Keep the failed payload available for investigation until the support logs have been collected.

## Log collection

Collect both log files from `%LOCALAPPDATA%\Axis\ExcelExporterImporter\Logs`.

1. `python-ui.log`
2. `backend.log`

Ask the user for:

1. Revit version.
2. Whether they used the Python workflow or the legacy WPF fallback.
3. The operation ID shown in the result dialog or error alert.
4. The workbook path or export target path involved.

The log format is `key="value"`. Search for `operationId="..."` first, then review adjacent `event="..."` lines.

## Troubleshooting

### DLL not found or assembly load failure

1. Rebuild with `./scripts/Build-PyRevitHybrid.ps1 -Configuration Release`.
2. Confirm the matching `Revit20xx/net8.0-windows/ExcelExporterImporter.dll` exists under the extension `bin` folder.
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
4. If production work is blocked, rerun using the legacy WPF fallback while investigating.

## Regression checks

Run these checks for every release candidate.

1. Python workflow opens and completes one schedule export.
2. Python workflow completes one standards export.
3. Python workflow completes one import from a workbook exported by the tool.
4. Failure alert shows support log paths when the staged DLL is missing.
5. Legacy WPF fallback still opens.
6. Both `python-ui.log` and `backend.log` receive new entries.

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
