# -*- coding: utf-8 -*-
"""Shared interop runtime helpers for pyRevit commands."""

from pyrevit import forms

from loader import AssemblyLoadError
from loader import load_excel_exporter_importer_assembly


def _format_paths(paths):
    return "\n".join(["- {0}".format(path) for path in paths])


def show_error(message, loaded_path=None, attempted_paths=None, details=None):
    lines = [message]

    if loaded_path:
        lines.extend(["", "DLL path:", loaded_path])

    if attempted_paths:
        lines.extend(["", "Searched paths:", _format_paths(attempted_paths)])

    lines.extend(
        [
            "",
            "Next steps:",
            "1. Run .\\scripts\\Build-PyRevitHybrid.ps1 -Configuration Release from the repo root.",
            "2. Confirm the matching Revit20xx folder under axis\\ExcelExporterImporter.extension\\bin contains ExcelExporterImporter.dll.",
            "3. Reload pyRevit and try again.",
        ]
    )

    if details:
        lines.extend(["", "Details:", details])

    forms.alert("\n".join(lines), title="Excel Exporter Importer")


def load_interop_or_alert():
    """Load C# assembly and return (loaded_path, interop_module_classes_tuple)."""
    try:
        loaded_path = load_excel_exporter_importer_assembly()
    except AssemblyLoadError as exc:
        show_error(
            str(exc),
            loaded_path=exc.resolved_path,
            attempted_paths=exc.attempted_paths,
            details=str(exc.inner_error) if exc.inner_error else None,
        )
        return None

    try:
        from ExcelExporterImporter.Interop import ExcelExporterImporterInterop
        from ExcelExporterImporter.Interop import ExportSchedulesRequest
        from ExcelExporterImporter.Interop import ExportStandardsRequest
        from ExcelExporterImporter.Interop import ImportRequest
        from ExcelExporterImporter.Interop import ProgressInfo

        return (
            loaded_path,
            ExcelExporterImporterInterop,
            ExportSchedulesRequest,
            ExportStandardsRequest,
            ImportRequest,
            ProgressInfo,
        )
    except Exception as exc:
        show_error(
            "The hybrid assembly loaded, but the interop entry point could not be imported.",
            loaded_path=loaded_path,
            details=str(exc),
        )
        return None
