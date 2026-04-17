# -*- coding: utf-8 -*-
"""Python-owned pyRevit workflow for export/import using C# backend services."""

import os
import sys

from pyrevit import forms
from pyrevit import revit
from pyrevit import script

from System import Action
from System.Threading import CancellationToken

this_dir = os.path.dirname(__file__)
lib_dir = os.path.abspath(os.path.join(this_dir, "..", "..", "..", "lib"))
if lib_dir not in sys.path:
    sys.path.append(lib_dir)

from interop_runtime import load_interop_or_alert
from interop_runtime import show_error
from support_logging import OperationLogger
from support_logging import get_python_log_path


def _prepare_output_path(output_path, operation_logger, cancel_event_name):
    if not os.path.exists(output_path):
        return True

    overwrite_ok = forms.alert(
        "The selected file already exists. Overwrite it?",
        yes=True,
        no=True,
        title="Confirm Overwrite",
    )
    if not overwrite_ok:
        operation_logger.info(cancel_event_name, reason="Overwrite declined", outputPath=output_path)
        return False

    try:
        os.remove(output_path)
    except Exception as exc:
        operation_logger.exception(
            "output-overwrite-delete-failed",
            exc,
            outputPath=output_path,
            operation=cancel_event_name,
        )
        show_error(
            "Could not overwrite the selected file. Close it in Excel (if open) and try again.",
            details=str(exc),
            python_log_path=get_python_log_path(),
            backend_log_path=operation_logger.backend_log_path,
        )
        return False

    return True


def _select_many(items, title, button_name):
    labels = [item[0] for item in items]
    selected = forms.SelectFromList.show(
        labels,
        multiselect=True,
        title=title,
        button_name=button_name,
    )
    if not selected:
        return []

    selected_set = set(selected)
    return [item[1] for item in items if item[0] in selected_set]


def _build_unique_items(entries):
    counts = {}
    unique_items = []
    for display_name, unique_id in entries:
        count = counts.get(display_name, 0) + 1
        counts[display_name] = count
        if count == 1:
            label = display_name
        else:
            label = "{0} [{1}]".format(display_name, unique_id)
        unique_items.append((label, unique_id))

    return unique_items


def _format_summary(result, python_log_path, backend_log_path=None):
    lines = []
    status = "Cancelled" if result.IsCancelled else "Succeeded" if result.Success else "Failed"
    lines.append("Status: {0}".format(status))

    if result.OperationName:
        lines.append("Operation: {0}".format(result.OperationName))
    if result.OperationId:
        lines.append("Operation ID: {0}".format(result.OperationId))

    summary = result.Summary
    if summary is not None:
        lines.append("")
        lines.extend(
            [
                "Requested: {0}".format(summary.RequestedCount),
                "Succeeded: {0}".format(summary.SucceededCount),
                "Failed: {0}".format(summary.FailedCount),
                "Skipped: {0}".format(summary.SkippedCount),
            ]
        )

    if result.Warnings and len(result.Warnings) > 0:
        lines.append("")
        lines.append("Warnings:")
        lines.extend(["- {0}".format(w) for w in result.Warnings])

    if result.Errors and len(result.Errors) > 0:
        lines.append("")
        lines.append("Errors:")
        lines.extend(["- {0}".format(e) for e in result.Errors])

    lines.append("")
    lines.append("Support logs:")
    lines.append("- Python UI: {0}".format(python_log_path))
    lines.append("- Backend: {0}".format(result.LogFilePath or backend_log_path or "Not available"))

    return "\n".join(lines) if lines else "Operation completed."


def _build_progress_callback(progress_info_type, operation_name):
    output = script.get_output()
    state = {"last_percent": -1, "last_message": None}

    def _on_progress(info):
        max_value = info.MaxValue if info.MaxValue > 0 else 1
        percent = int((float(info.Value) / float(max_value)) * 100.0)
        message = info.Message or operation_name

        if percent != state["last_percent"] or message != state["last_message"]:
            state["last_percent"] = percent
            state["last_message"] = message
            print("[{0:>3}%] {1}".format(percent, message))

    print("Starting {0}...".format(operation_name))
    output.set_title("Excel Exporter Importer - {0}".format(operation_name))
    return Action[progress_info_type](_on_progress)


def _run_export_schedules(doc, interop, export_schedules_request_type, progress_info_type, operation_logger):
    schedules = list(interop.GetExportableSchedules(doc))
    if not schedules:
        operation_logger.warning("schedule-export-no-schedules", documentTitle=doc.Title)
        forms.alert("No exportable schedules were found in the active model.", title="Excel Exporter Importer")
        return

    items = _build_unique_items(sorted([(s.Name, s.UniqueId) for s in schedules], key=lambda x: x[0].lower()))
    selected_ids = _select_many(items, "Select Schedules to Export", "Export")
    if not selected_ids:
        operation_logger.info("schedule-export-cancelled-before-run", reason="No schedules selected")
        return

    use_basic = forms.alert(
        "Use Basic export mode?\n\nYes = Basic mode (data-only).\nNo = Advanced mode (color coding + legend).",
        yes=True,
        no=True,
        title="Export Mode",
    )

    output_path = forms.save_file(file_ext="xlsx", default_name="revit-schedules-export")
    if not output_path:
        operation_logger.info("schedule-export-cancelled-before-run", reason="No output path selected")
        return

    if not _prepare_output_path(output_path, operation_logger, "schedule-export-cancelled-before-run"):
        return

    request = export_schedules_request_type()
    request.OutputFilePath = output_path
    request.UseBasicMode = bool(use_basic)
    for unique_id in selected_ids:
        request.ScheduleUniqueIds.Add(unique_id)

    operation_logger.info(
        "schedule-export-requested",
        documentTitle=doc.Title,
        outputPath=output_path,
        requestedCount=len(selected_ids),
        useBasicMode=bool(use_basic),
    )
    callback = _build_progress_callback(progress_info_type, "schedule export")
    result = interop.ExecuteExportSchedules(doc, request, callback, CancellationToken.None)
    operation_logger.info(
        "schedule-export-completed",
        outputPath=output_path,
        success=result.Success,
        cancelled=result.IsCancelled,
        operationId=result.OperationId,
    )
    forms.alert(_format_summary(result, get_python_log_path(), operation_logger.backend_log_path), title="Schedule Export Result")


def _run_export_standards(doc, interop, export_standards_request_type, progress_info_type, operation_logger):
    standards = list(interop.GetExportableStandards())
    if not standards:
        operation_logger.warning("standards-export-no-items", documentTitle=doc.Title)
        forms.alert("No standards groups were returned by the backend.", title="Excel Exporter Importer")
        return

    items = []
    for standard in standards:
        label = standard.DisplayName
        if standard.IsReadOnly:
            label += " [Export-only]"
        items.append((label, standard.GroupUniqueId))

    items = _build_unique_items(sorted(items, key=lambda x: x[0].lower()))
    selected_ids = _select_many(items, "Select Standards Groups to Export", "Export")
    if not selected_ids:
        operation_logger.info("standards-export-cancelled-before-run", reason="No standards selected")
        return

    output_path = forms.save_file(file_ext="xlsx", default_name="revit-standards-export")
    if not output_path:
        operation_logger.info("standards-export-cancelled-before-run", reason="No output path selected")
        return

    if not _prepare_output_path(output_path, operation_logger, "standards-export-cancelled-before-run"):
        return

    request = export_standards_request_type()
    request.OutputFilePath = output_path
    for unique_id in selected_ids:
        request.StandardGroupUniqueIds.Add(unique_id)

    operation_logger.info(
        "standards-export-requested",
        documentTitle=doc.Title,
        outputPath=output_path,
        requestedCount=len(selected_ids),
    )
    callback = _build_progress_callback(progress_info_type, "standards export")
    result = interop.ExecuteExportStandards(doc, request, callback, CancellationToken.None)
    operation_logger.info(
        "standards-export-completed",
        outputPath=output_path,
        success=result.Success,
        cancelled=result.IsCancelled,
        operationId=result.OperationId,
    )
    forms.alert(_format_summary(result, get_python_log_path(), operation_logger.backend_log_path), title="Standards Export Result")


def _run_import(doc, interop, import_request_type, progress_info_type, operation_logger):
    workbook_path = forms.pick_file(file_ext="xlsx", title="Select Workbook to Import")
    if not workbook_path:
        operation_logger.info("import-cancelled-before-run", reason="No workbook selected")
        return

    inspection = interop.InspectImportWorkbook(doc, workbook_path)
    if not inspection.IsValidWorkbook:
        operation_logger.warning("import-invalid-workbook", workbookPath=workbook_path)
        forms.alert(
            "The selected workbook does not contain recognized schedule or standard sheets exported by this tool.",
            title="Import Not Available",
        )
        return

    importable_items = list(inspection.ImportableItems)
    if not importable_items:
        operation_logger.warning("import-no-importable-items", workbookPath=workbook_path)
        details = ["No importable items were found in this workbook."]
        if inspection.ReadOnlyItems and len(inspection.ReadOnlyItems) > 0:
            details.append("")
            details.append("Read-only or unsupported sheets:")
            for item in inspection.ReadOnlyItems:
                reason = " ({0})".format(item.Reason) if item.Reason else ""
                details.append("- {0}{1}".format(item.DisplayName, reason))
        forms.alert("\n".join(details), title="Import Not Available")
        return

    item_rows = []
    for item in importable_items:
        item_rows.append((item.DisplayName, item.UniqueId))

    if inspection.ReadOnlyItems and len(inspection.ReadOnlyItems) > 0:
        print("Read-only/unsupported sheets found: {0}".format(len(inspection.ReadOnlyItems)))
        for item in inspection.ReadOnlyItems:
            reason = " ({0})".format(item.Reason) if item.Reason else ""
            print("- {0}{1}".format(item.DisplayName, reason))

    selected_ids = _select_many(
        _build_unique_items(sorted(item_rows, key=lambda x: x[0].lower())),
        "Select Workbook Items to Import",
        "Import",
    )
    if not selected_ids:
        operation_logger.info("import-cancelled-before-run", reason="No workbook items selected", workbookPath=workbook_path)
        return

    confirm = forms.alert(
        "Import will write values into the current Revit model and may overwrite existing data. Continue?",
        yes=True,
        no=True,
        title="Confirm Import",
    )
    if not confirm:
        operation_logger.info("import-cancelled-before-run", reason="Confirmation declined", workbookPath=workbook_path)
        return

    request = import_request_type()
    request.WorkbookFilePath = workbook_path
    for unique_id in selected_ids:
        request.ItemUniqueIds.Add(unique_id)

    operation_logger.info(
        "import-requested",
        documentTitle=doc.Title,
        workbookPath=workbook_path,
        requestedCount=len(selected_ids),
    )
    callback = _build_progress_callback(progress_info_type, "import")
    result = interop.ExecuteImport(doc, request, callback, CancellationToken.None)
    operation_logger.info(
        "import-completed",
        workbookPath=workbook_path,
        success=result.Success,
        cancelled=result.IsCancelled,
        operationId=result.OperationId,
    )
    forms.alert(_format_summary(result, get_python_log_path(), operation_logger.backend_log_path), title="Import Result")


def main():
    operation_logger = OperationLogger("python-workflow")
    doc = revit.doc
    if doc is None:
        operation_logger.warning("no-active-document")
        forms.alert("No active Revit document.", title="Excel Exporter Importer")
        return

    loaded = load_interop_or_alert(operation_logger)
    if loaded is None:
        return

    (
        loaded_path,
        backend_log_path,
        interop,
        export_schedules_request_type,
        export_standards_request_type,
        import_request_type,
        progress_info_type,
    ) = loaded

    mode = forms.CommandSwitchWindow.show(
        ["Export Schedules", "Export Standards", "Import Workbook"],
        message="Choose workflow:",
    )
    if not mode:
        operation_logger.info("workflow-selection-cancelled")
        return

    operation_logger.set_backend_log_path(backend_log_path)
    operation_logger.info("workflow-selected", workflow=mode, assemblyPath=loaded_path)

    try:
        if mode == "Export Schedules":
            _run_export_schedules(doc, interop, export_schedules_request_type, progress_info_type, operation_logger)
        elif mode == "Export Standards":
            _run_export_standards(doc, interop, export_standards_request_type, progress_info_type, operation_logger)
        else:
            _run_import(doc, interop, import_request_type, progress_info_type, operation_logger)
    except Exception as exc:
        operation_logger.exception("workflow-execution-failed", exc, workflow=mode, assemblyPath=loaded_path)
        show_error(
            "The backend call failed during workflow execution.",
            loaded_path=loaded_path,
            details=str(exc),
            python_log_path=get_python_log_path(),
            backend_log_path=operation_logger.backend_log_path,
        )


if __name__ == "__main__":
    main()
