# ExcelExporterImporter pyRevit extension

This folder contains the pyRevit extension wrapper used for the hybrid rollout.

## Commands

- `Excel Exporter Importer`: Python-owned workflow for export/import using the C# interop backend.
- `Excel Exporter Importer (Legacy WPF)`: original WPF fallback.

## Support logs

Both commands write support logs under `%LOCALAPPDATA%\Axis\ExcelExporterImporter\Logs`.

- `python-ui.log`: pyRevit command entry, workflow choice, user-facing failures.
- `backend.log`: C# interop and backend workflow events, item-level failures, startup events.

When support is investigating an issue, ask for both files and the operation ID shown in the result dialog.

## Rollback shortcut

If the Python workflow blocks production work, use the legacy WPF button immediately and restore the previously staged `axis/ExcelExporterImporter.extension/bin/Revit20xx` payload from backup.
