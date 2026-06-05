# ExcelExporterImporter pyRevit extension

This folder contains the pyRevit extension wrapper used for the hybrid rollout.

## Commands

- `Excel Exporter Importer`: Python-owned workflow for export/import using the C# interop backend.

## Support logs

The command writes support logs under `%APPDATA%\Pascall-Watson\Axis`.

- `python-ui.log`: pyRevit command entry, workflow choice, user-facing failures.
- `backend.log`: C# interop and backend workflow events, item-level failures, startup events.

When support is investigating an issue, ask for both files and the operation ID shown in the result dialog.

## Rollback shortcut

If the Python workflow blocks production work, restore the previously staged `axis/axis.extension/bin/Revit20xx` payload from backup and reload pyRevit.
