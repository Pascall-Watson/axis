# -*- coding: utf-8 -*-

from System import Action
from System.Threading import CancellationToken


class WorkflowService(object):
    def __init__(
        self,
        interop,
        export_schedules_request_type,
        import_request_type,
        progress_info_type,
    ):
        self._interop = interop
        self._export_schedules_request_type = export_schedules_request_type
        self._import_request_type = import_request_type
        self._progress_info_type = progress_info_type

    @staticmethod
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

    def get_exportable_schedules(self, doc):
        schedules = list(self._interop.GetExportableSchedules(doc))
        pairs = sorted([(s.Name, s.UniqueId) for s in schedules], key=lambda x: x[0].lower())
        return self._build_unique_items(pairs)

    def inspect_import_workbook(self, doc, workbook_path):
        inspection = self._interop.InspectImportWorkbook(doc, workbook_path)
        data = {
            "is_valid": bool(inspection.IsValidWorkbook),
            "importable": [],
            "read_only": [],
        }

        for item in list(inspection.ImportableItems):
            data["importable"].append((item.DisplayName, item.UniqueId))

        for item in list(inspection.ReadOnlyItems):
            reason = ""
            if item.Reason:
                reason = " ({0})".format(item.Reason)
            data["read_only"].append("{0}{1}".format(item.DisplayName, reason))

        data["importable"] = self._build_unique_items(sorted(data["importable"], key=lambda x: x[0].lower()))
        data["read_only"] = sorted(data["read_only"])
        return data

    def _build_progress_callback(self, operation_name):
        def _on_progress(info):
            return

        return Action[self._progress_info_type](_on_progress)

    def run_export_schedules(self, doc, output_path, selected_ids):
        request = self._export_schedules_request_type()
        request.OutputFilePath = output_path
        request.UseBasicMode = False
        for unique_id in selected_ids:
            request.ScheduleUniqueIds.Add(unique_id)

        callback = self._build_progress_callback("schedule export")
        return self._interop.ExecuteExportSchedules(doc, request, callback, CancellationToken.None)

    def run_import(self, doc, workbook_path, selected_ids):
        request = self._import_request_type()
        request.WorkbookFilePath = workbook_path
        for unique_id in selected_ids:
            request.ItemUniqueIds.Add(unique_id)

        callback = self._build_progress_callback("import")
        return self._interop.ExecuteImport(doc, request, callback, CancellationToken.None)

    @staticmethod
    def format_summary(result, python_log_path, backend_log_path=None):
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
            lines.extend(["- {0}".format(warning) for warning in result.Warnings])

        if result.Errors and len(result.Errors) > 0:
            lines.append("")
            lines.append("Errors:")
            lines.extend(["- {0}".format(error) for error in result.Errors])

        lines.append("")
        lines.append("Support logs:")
        lines.append("- Python UI: {0}".format(python_log_path))
        lines.append("- Backend: {0}".format(result.LogFilePath or backend_log_path or "Not available"))

        return "\n".join(lines) if lines else "Operation completed."
