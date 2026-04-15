# -*- coding: utf-8 -*-
"""Shared interop runtime helpers for pyRevit commands."""

from pyrevit import forms

from loader import AssemblyLoadError
from loader import load_excel_exporter_importer_assembly
from support_logging import get_python_log_path
from support_logging import log_exception


def _format_paths(paths):
    return "\n".join(["- {0}".format(path) for path in paths])


def show_error(
    message,
    loaded_path=None,
    attempted_paths=None,
    details=None,
    python_log_path=None,
    backend_log_path=None,
    operation_id=None,
):
    lines = [message]

    if loaded_path:
        lines.extend(["", "DLL path:", loaded_path])

    if attempted_paths:
        lines.extend(["", "Searched paths:", _format_paths(attempted_paths)])

    if operation_id:
        lines.extend(["", "Operation ID:", operation_id])

    log_lines = []
    if python_log_path:
        log_lines.append("Python UI: {0}".format(python_log_path))
    if backend_log_path:
        log_lines.append("Backend: {0}".format(backend_log_path))

    if log_lines:
        lines.extend(["", "Support logs:"])
        lines.extend(log_lines)

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


def load_interop_or_alert(operation_logger=None):
    """Load C# assembly and return (loaded_path, interop_module_classes_tuple)."""
    try:
        loaded_path = load_excel_exporter_importer_assembly()
    except AssemblyLoadError as exc:
        log_exception(
            "interop-load-failed",
            exc,
            {"resolvedPath": exc.resolved_path, "revitMajor": exc.revit_major},
        )
        show_error(
            str(exc),
            loaded_path=exc.resolved_path,
            attempted_paths=exc.attempted_paths,
            details=str(exc.inner_error) if exc.inner_error else None,
            python_log_path=get_python_log_path(),
        )
        return None

    try:
        from ExcelExporterImporter.Interop import ExcelExporterImporterInterop
        from ExcelExporterImporter.Interop import ExportSchedulesRequest
        from ExcelExporterImporter.Interop import ExportStandardsRequest
        from ExcelExporterImporter.Interop import ImportRequest
        from ExcelExporterImporter.Interop import ProgressInfo

        backend_log_path = ExcelExporterImporterInterop.GetBackendLogFilePath()
        if operation_logger is not None:
            operation_logger.set_backend_log_path(backend_log_path)

        return (
            loaded_path,
            backend_log_path,
            ExcelExporterImporterInterop,
            ExportSchedulesRequest,
            ExportStandardsRequest,
            ImportRequest,
            ProgressInfo,
        )
    except Exception as exc:
        log_exception("interop-import-failed", exc, {"loadedPath": loaded_path})
        show_error(
            "The hybrid assembly loaded, but the interop entry point could not be imported.",
            loaded_path=loaded_path,
            details=str(exc),
            python_log_path=get_python_log_path(),
        )
        return None
