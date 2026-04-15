# -*- coding: utf-8 -*-
"""Launch the existing C# WPF window from pyRevit."""

import os
import sys

from pyrevit import forms
from pyrevit import revit

this_dir = os.path.dirname(__file__)
lib_dir = os.path.abspath(os.path.join(this_dir, "..", "..", "..", "lib"))
if lib_dir not in sys.path:
    sys.path.append(lib_dir)

from loader import AssemblyLoadError
from loader import load_excel_exporter_importer_assembly


def _format_paths(paths):
    return "\n".join(["- {0}".format(path) for path in paths])


def _show_error(message, loaded_path=None, attempted_paths=None, details=None):
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


def main():
    try:
        loaded_path = load_excel_exporter_importer_assembly()
    except AssemblyLoadError as exc:
        _show_error(
            str(exc),
            loaded_path=exc.resolved_path,
            attempted_paths=exc.attempted_paths,
            details=str(exc.inner_error) if exc.inner_error else None,
        )
        return

    try:
        from ExcelExporterImporter.Interop import ExcelExporterImporterInterop
    except Exception as exc:
        _show_error(
            "The hybrid assembly loaded, but the interop entry point could not be imported.",
            loaded_path=loaded_path,
            details=str(exc),
        )
        return

    doc = revit.doc
    if doc is None:
        forms.alert("No active Revit document.", title="Excel Exporter Importer")
        return

    try:
        ok = ExcelExporterImporterInterop.ShowMainWindow(doc)
    except Exception as exc:
        _show_error(
            "The C# WPF window threw an exception during launch.",
            loaded_path=loaded_path,
            details=str(exc),
        )
        return

    if not ok:
        _show_error(
            "The C# dialog did not start.",
            loaded_path=loaded_path,
            details="ShowMainWindow returned False.",
        )


if __name__ == "__main__":
    main()
