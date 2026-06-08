# -*- coding: utf-8 -*-

import datetime
import os
import re
import sys

from pyrevit import forms
from pyrevit import revit

this_dir = os.path.dirname(__file__)
lib_dir = os.path.abspath(os.path.join(this_dir, "..", "..", "..", "..", "lib"))
if lib_dir not in sys.path:
    sys.path.append(lib_dir)

from interop_runtime import load_interop_or_alert
from interop_runtime import show_error
from support_logging import OperationLogger
from support_logging import get_python_log_path

from default.services.workflow_service import WorkflowService
from default.viewmodels.main_viewmodel import MainViewModel


def _status(vm, message):
    if vm is not None:
        vm.StatusMessage = message


def _safe_file_stem(value):
    cleaned = re.sub(r'[<>:"/\\|?*]', "_", (value or "").strip())
    cleaned = re.sub(r"\s+", "_", cleaned)
    cleaned = cleaned.strip("._")
    return cleaned or "Schedule"


def _build_export_default_name(vm):
    timestamp = datetime.datetime.now().strftime("%Y-%m-%dT%H-%M-%S")
    schedule_name = _safe_file_stem(vm.get_primary_selected_schedule_label())
    return "{0}_{1}".format(timestamp, schedule_name)


def _build_schedule_export_file_name(schedule_label):
    timestamp = datetime.datetime.now().strftime("%Y-%m-%dT%H-%M-%S")
    schedule_name = _safe_file_stem(schedule_label)
    return "{0}_{1}.xlsx".format(timestamp, schedule_name)


def _confirm_overwrite(path, operation_logger, cancel_event_name):
    if not os.path.exists(path):
        return True

    try:
        os.remove(path)
        operation_logger.info(cancel_event_name, reason="Existing output overwritten", outputPath=path)
        return True
    except Exception as exc:
        operation_logger.exception(
            "output-overwrite-delete-failed",
            exc,
            outputPath=path,
            operation=cancel_event_name,
        )
        show_error(
            "Could not overwrite the selected file. Close it in Excel and try again.",
            details=str(exc),
            python_log_path=get_python_log_path(),
            backend_log_path=operation_logger.backend_log_path,
        )
        return False


def _load_initial_data(doc, service, vm, operation_logger):
    schedules = service.get_exportable_schedules(doc)

    vm.set_export_schedules(schedules)

    if len(schedules) == 0:
        operation_logger.warning("schedule-export-no-schedules", documentTitle=doc.Title)


def _run_export_schedules(doc, service, vm, operation_logger):
    selected_schedules = vm.get_selected_schedules()
    if len(selected_schedules) == 0:
        _status(vm, "Select one or more schedules to export.")
        return

    is_multi_file = vm.SelectedExportMode == "Workbooks"

    if is_multi_file:
        export_folder = vm.ExportPath
        if not export_folder or not os.path.isdir(export_folder):
            selected_folder = forms.pick_folder(title="Select export folder")
            if not selected_folder:
                _status(vm, "Choose an export location to continue.")
                return
            export_folder = selected_folder
            vm.ExportPath = selected_folder

        total = len(selected_schedules)
        succeeded = 0
        cancelled = 0

        for item in selected_schedules:
            file_path = os.path.join(export_folder, _build_schedule_export_file_name(item.Label))
            if not _confirm_overwrite(file_path, operation_logger, "schedule-export-cancelled-before-run"):
                continue

            result = service.run_export_schedules(doc, file_path, [item.UniqueId])
            if result.Success and not result.IsCancelled:
                succeeded += 1
            elif result.IsCancelled:
                cancelled += 1

        if succeeded == total:
            _status(vm, "Export completed. {0} of {1} schedules exported.".format(succeeded, total))
        elif cancelled == total:
            _status(vm, "Export cancelled. No schedules exported.")
        elif succeeded == 0:
            _status(vm, "Export failed. Check support logs for details.")
        else:
            _status(
                vm,
                "Export partially completed. {0} of {1} schedules exported. Check support logs for details.".format(
                    succeeded,
                    total,
                ),
            )
        return

    if not vm.ExportPath:
        selected = forms.save_file(file_ext="xlsx", default_name=_build_export_default_name(vm))
        if not selected:
            _status(vm, "Choose an export location to continue.")
            return
        vm.ExportPath = selected

    if not _confirm_overwrite(vm.ExportPath, operation_logger, "schedule-export-cancelled-before-run"):
        return

    operation_logger.info(
        "schedule-export-requested",
        documentTitle=doc.Title,
        outputPath=vm.ExportPath,
        requestedCount=len(selected_schedules),
        useBasicMode=False,
    )
    result = service.run_export_schedules(doc, vm.ExportPath, [item.UniqueId for item in selected_schedules])

    operation_logger.info(
        "schedule-export-completed",
        outputPath=vm.ExportPath,
        success=result.Success,
        cancelled=result.IsCancelled,
        operationId=result.OperationId,
    )

    if result.IsCancelled:
        _status(vm, "Export cancelled.")
    elif result.Success:
        _status(vm, "Export completed.")
    else:
        _status(vm, "Export failed. Check support logs for details.")

def _refresh_import_items(doc, service, vm, operation_logger):
    if not vm.WorkbookPath:
        _status(vm, "Choose a workbook before loading importable items.")
        return

    inspection = service.inspect_import_workbook(doc, vm.WorkbookPath)
    if not inspection["is_valid"]:
        operation_logger.warning("import-invalid-workbook", workbookPath=vm.WorkbookPath)
        _status(vm, "The selected workbook does not contain recognized sheets exported by this tool.")
        vm.set_import_data([], [])
        return

    vm.set_import_data(inspection["importable"], inspection["read_only"])

    if len(inspection["importable"]) == 0:
        operation_logger.warning("import-no-importable-items", workbookPath=vm.WorkbookPath)
        _status(vm, "No importable items were found in this workbook.")
    else:
        _status(vm, "Workbook items loaded.")


def _run_import(doc, service, vm, operation_logger):
    selected_ids = vm.get_selected_import_ids()
    if len(selected_ids) == 0:
        _status(vm, "Select one or more workbook items to import.")
        return

    if not vm.WorkbookPath:
        _status(vm, "Choose a workbook before importing.")
        return

    operation_logger.info(
        "import-requested",
        documentTitle=doc.Title,
        workbookPath=vm.WorkbookPath,
        requestedCount=len(selected_ids),
    )
    result = service.run_import(doc, vm.WorkbookPath, selected_ids)

    operation_logger.info(
        "import-completed",
        workbookPath=vm.WorkbookPath,
        success=result.Success,
        cancelled=result.IsCancelled,
        operationId=result.OperationId,
    )

    if result.IsCancelled:
        _status(vm, "Import cancelled.")
    elif result.Success:
        _status(vm, "Import completed.")
    else:
        _status(vm, "Import failed. Check support logs for details.")


def _build_action_handler(window, doc, service, vm, operation_logger):
    def _handle(action_name, _viewmodel):
        if action_name == "Cancel":
            window.Close()
            return

        if action_name == "BrowseExportPath":
            if vm.SelectedExportMode == "Workbooks":
                selected = forms.pick_folder(title="Select export folder")
                if selected:
                    vm.ExportPath = selected
                    _status(vm, "Export folder selected.")
            else:
                selected = forms.save_file(file_ext="xlsx", default_name=_build_export_default_name(vm))
                if selected:
                    vm.ExportPath = selected
                    _status(vm, "Export location selected.")
            return

        if action_name == "BrowseWorkbookPath":
            selected = forms.pick_file(file_ext="xlsx", title="Select workbook to import")
            if selected:
                vm.WorkbookPath = selected
                _status(vm, "Import location selected. Loading sheets...")
                _refresh_import_items(doc, service, vm, operation_logger)
            return

        if action_name == "Run":
            if vm.SelectedTab == 1:
                _run_import(doc, service, vm, operation_logger)
            else:
                _run_export_schedules(doc, service, vm, operation_logger)

    return _handle


def main():
    operation_logger = OperationLogger("python-workflow")
    doc = revit.doc

    if doc is None:
        operation_logger.warning("no-active-document")
        forms.alert("No active Revit document.", title="Axis")
        return

    loaded = load_interop_or_alert(operation_logger)
    if loaded is None:
        return

    (
        loaded_path,
        backend_log_path,
        interop,
        export_schedules_request_type,
        _export_standards_request_type,
        import_request_type,
        progress_info_type,
    ) = loaded

    operation_logger.set_backend_log_path(backend_log_path)
    operation_logger.info("workflow-window-opened", assemblyPath=loaded_path)

    service = WorkflowService(
        interop,
        export_schedules_request_type,
        import_request_type,
        progress_info_type,
    )

    vm = MainViewModel("Axis")
    _load_initial_data(doc, service, vm, operation_logger)

    xaml_path = os.path.join(this_dir, "views", "MainView.xaml")
    window = forms.WPFWindow(xaml_path)
    window.DataContext = vm

    vm.set_action_handler(_build_action_handler(window, doc, service, vm, operation_logger))

    try:
        window.ShowDialog()
    except Exception as exc:
        operation_logger.exception("workflow-window-failed", exc, assemblyPath=loaded_path)
        show_error(
            "The workflow window failed to open.",
            loaded_path=loaded_path,
            details=str(exc),
            python_log_path=get_python_log_path(),
            backend_log_path=operation_logger.backend_log_path,
        )


if __name__ == "__main__":
    main()
