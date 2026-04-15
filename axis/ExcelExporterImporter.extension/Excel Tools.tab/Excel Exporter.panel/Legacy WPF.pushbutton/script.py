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


def main():
    doc = revit.doc
    if doc is None:
        forms.alert("No active Revit document.", title="Excel Exporter Importer")
        return

    loaded = load_interop_or_alert()
    if loaded is None:
        return

    loaded_path = loaded[0]
    interop = loaded[1]

    try:
        ok = interop.ShowMainWindow(doc)
    except Exception as exc:
        show_error(
            "The C# WPF window threw an exception during launch.",
            loaded_path=loaded_path,
            details=str(exc),
        )
        return

    if not ok:
        show_error(
            "The C# dialog did not start.",
            loaded_path=loaded_path,
            details="ShowMainWindow returned False.",
        )


if __name__ == "__main__":
    main()
