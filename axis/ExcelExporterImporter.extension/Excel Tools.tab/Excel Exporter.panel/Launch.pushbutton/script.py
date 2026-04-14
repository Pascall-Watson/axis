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

from loader import load_excel_exporter_importer_assembly


def main():
    loaded_path = load_excel_exporter_importer_assembly()

    from ExcelExporterImporter.Interop import ExcelExporterImporterInterop

    doc = revit.doc
    if doc is None:
        forms.alert("No active Revit document.", title="Excel Exporter Importer")
        return

    ok = ExcelExporterImporterInterop.ShowMainWindow(doc)
    if not ok:
        forms.alert(
            "The C# dialog did not start. Verify assembly dependencies and build outputs.\n\n"
            "Loaded: {0}".format(loaded_path),
            title="Excel Exporter Importer"
        )


if __name__ == "__main__":
    main()
