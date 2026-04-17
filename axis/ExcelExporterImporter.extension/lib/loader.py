# -*- coding: utf-8 -*-
"""Runtime assembly loader for the hybrid pyRevit + C# workflow."""

import os

import clr
from pyrevit import HOST_APP


class AssemblyLoadError(IOError):
    """Raised when the hybrid assembly cannot be found or loaded."""

    def __init__(self, message, revit_major, attempted_paths, resolved_path=None, inner_error=None):
        IOError.__init__(self, message)
        self.revit_major = revit_major
        self.attempted_paths = attempted_paths
        self.resolved_path = resolved_path
        self.inner_error = inner_error


def _extension_root():
    return os.path.dirname(os.path.dirname(__file__))


def _revit_major_version():
    return str(HOST_APP.version)


def _candidate_paths(revit_major):
    ext_root = _extension_root()
    # Support all framework versions: net481 (2024), net8.0-windows (2025-2026), net10.0-windows (2027)
    return [
        os.path.join(ext_root, "bin", "Revit{0}".format(revit_major), "net10.0-windows", "ExcelExporterImporter.dll"),
        os.path.join(ext_root, "bin", "Revit{0}".format(revit_major), "net8.0-windows", "ExcelExporterImporter.dll"),
        os.path.join(ext_root, "bin", "Revit{0}".format(revit_major), "net481", "ExcelExporterImporter.dll"),
        os.path.join(ext_root, "bin", "Revit{0}".format(revit_major), "ExcelExporterImporter.dll"),
        os.path.join(ext_root, "bin", "ExcelExporterImporter.dll"),
    ]


def _raise_load_error(message, revit_major, attempted_paths, resolved_path=None, inner_error=None):
    raise AssemblyLoadError(message, revit_major, attempted_paths, resolved_path, inner_error)


def load_excel_exporter_importer_assembly():
    """Load the version-matched C# assembly and return loaded path."""
    revit_major = _revit_major_version()
    attempted_paths = _candidate_paths(revit_major)

    for path in attempted_paths:
        if os.path.exists(path):
            try:
                clr.AddReferenceToFileAndPath(path)
                return path
            except Exception as exc:
                _raise_load_error(
                    "ExcelExporterImporter.dll was found but could not be loaded.",
                    revit_major,
                    attempted_paths,
                    resolved_path=path,
                    inner_error=exc,
                )

    _raise_load_error(
        "ExcelExporterImporter.dll was not found for Revit {0}.".format(revit_major),
        revit_major,
        attempted_paths,
    )
