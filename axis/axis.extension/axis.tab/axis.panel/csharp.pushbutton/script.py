# -*- coding: utf-8 -*-
"""Launch the original C# WPF window (legacy fallback)."""

import os
import sys

from pyrevit import forms
from pyrevit import revit

this_dir = os.path.dirname(__file__)
lib_dir = os.path.abspath(os.path.join(this_dir, "..", "..", "..", "lib"))
if lib_dir not in sys.path:
    sys.path.append(lib_dir)

from interop_runtime import load_interop_or_alert
from interop_runtime import show_error
from support_logging import OperationLogger
from support_logging import get_python_log_path


def main():
    operation_logger = OperationLogger("legacy-wpf-launch")
    doc = revit.doc
    if doc is None:
        operation_logger.warning("no-active-document")
        forms.alert("No active Revit document.", title="Excel Exporter Importer")
        return

    loaded = load_interop_or_alert(operation_logger)
    if loaded is None:
        return

    loaded_path = loaded[0]
    backend_log_path = loaded[1]
    interop = loaded[2]
    operation_logger.set_backend_log_path(backend_log_path)

    try:
        operation_logger.info("legacy-wpf-requested", documentTitle=doc.Title, assemblyPath=loaded_path)
        ok = interop.ShowMainWindow(doc)
    except Exception as exc:
        operation_logger.exception("legacy-wpf-launch-failed", exc, assemblyPath=loaded_path)
        show_error(
            "The C# WPF window threw an exception during launch.",
            loaded_path=loaded_path,
            details=str(exc),
            python_log_path=get_python_log_path(),
            backend_log_path=operation_logger.backend_log_path,
        )
        return

    if not ok:
        operation_logger.warning("legacy-wpf-launch-returned-false", assemblyPath=loaded_path)
        show_error(
            "The C# dialog did not start.",
            loaded_path=loaded_path,
            details="ShowMainWindow returned False.",
            python_log_path=get_python_log_path(),
            backend_log_path=operation_logger.backend_log_path,
        )
        return

    operation_logger.info("legacy-wpf-launch-succeeded", assemblyPath=loaded_path)


if __name__ == "__main__":
    main()
