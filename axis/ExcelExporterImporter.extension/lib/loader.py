# -*- coding: utf-8 -*-
"""Runtime assembly loader for the hybrid pyRevit + C# workflow."""

import os

import clr
from pyrevit import HOST_APP


def _extension_root():
    return os.path.dirname(os.path.dirname(__file__))


def _revit_major_version():
    return str(HOST_APP.version)


def _candidate_paths(revit_major):
    ext_root = _extension_root()
    return [
        os.path.join(ext_root, "bin", "Revit{0}".format(revit_major), "net8.0-windows", "ExcelExporterImporter.dll"),
        os.path.join(ext_root, "bin", "Revit{0}".format(revit_major), "net48", "ExcelExporterImporter.dll"),
        os.path.join(ext_root, "bin", "Revit{0}".format(revit_major), "ExcelExporterImporter.dll"),
        os.path.join(ext_root, "bin", "ExcelExporterImporter.dll"),
    ]


def load_excel_exporter_importer_assembly():
    """Load the version-matched C# assembly and return loaded path."""
    revit_major = _revit_major_version()
    for path in _candidate_paths(revit_major):
        if os.path.exists(path):
            clr.AddReferenceToFileAndPath(path)
            return path

    raise IOError(
        "ExcelExporterImporter.dll not found for Revit {0}. "
        "Expected one of: {1}".format(revit_major, ", ".join(_candidate_paths(revit_major)))
    )
