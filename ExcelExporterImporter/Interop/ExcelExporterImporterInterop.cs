using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Windows.Interop;
using Autodesk.Revit.DB;
using ExcelExporterImporter.Services;
using ExcelExporterImporter.Views;

namespace ExcelExporterImporter.Interop
{
    public static class ExcelExporterImporterInterop
    {
        private static readonly DocumentInventoryService InventoryService = new DocumentInventoryService();
        private static readonly BackendWorkflowService WorkflowService = new BackendWorkflowService();

        // ─────────────────────────────────────────────────────────────────────
        // Legacy WPF launcher – preserved exactly as before.
        // ─────────────────────────────────────────────────────────────────────

        public static bool ShowMainWindow(Document document)
        {
            return ShowMainWindow(document, IntPtr.Zero);
        }

        public static bool ShowMainWindow(Document document, IntPtr ownerHandle)
        {
            if (document == null)
                return false;

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            var dialog = new MainWindow(document);
            var helper = new WindowInteropHelper(dialog);

            if (ownerHandle != IntPtr.Zero)
            {
                helper.Owner = ownerHandle;
            }

            dialog.ShowDialog();
            return true;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Sprint 2: Backend service API
        // These methods expose no WPF types, dialog types, or window handles.
        // They are safe to call from a pyRevit Python script via pythonnet/clr.
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns all schedules in <paramref name="document"/> that are candidates for
        /// export or import. Titleblock revision schedules are excluded from this list.
        /// </summary>
        public static IReadOnlyList<ScheduleInfo> GetExportableSchedules(Document document)
        {
            return InventoryService.GetExportableSchedules(document);
        }

        /// <summary>
        /// Returns the fixed list of standards groups that can be exported from any document.
        /// Groups with <see cref="StandardInfo.IsReadOnly"/> true are export-only and
        /// cannot be imported back.
        /// </summary>
        public static IReadOnlyList<StandardInfo> GetExportableStandards()
        {
            return InventoryService.GetExportableStandards();
        }

        /// <summary>
        /// Opens a workbook and returns which items it contains and whether each can be
        /// imported into <paramref name="document"/>. The file is opened read-only and
        /// released before this method returns.
        /// </summary>
        /// <param name="document">The Revit document that is the import target.</param>
        /// <param name="filePath">Absolute path to the .xlsx workbook.</param>
        public static ImportWorkbookInspection InspectImportWorkbook(Document document, string filePath)
        {
            return InventoryService.InspectImportWorkbook(document, filePath);
        }

        /// <summary>
        /// Exports selected schedules to an Excel file without showing any dialog.
        /// Calls <paramref name="onProgress"/> (if not null) to report progress.
        /// The operation respects <paramref name="cancellationToken"/>.
        /// </summary>
        public static OperationResult ExecuteExportSchedules(
            Document document,
            ExportSchedulesRequest request,
            Action<ProgressInfo> onProgress,
            CancellationToken cancellationToken)
        {
            var progress = WorkflowService.CreateCallbackProgress(
                onProgress,
                WorkflowService.EstimateScheduleExportProgressMaximum(request));
            return WorkflowService.ExecuteExportSchedules(document, request, cancellationToken, progress);
        }

        /// <summary>
        /// Exports selected standard groups to an Excel file without showing any dialog.
        /// Calls <paramref name="onProgress"/> (if not null) to report progress.
        /// The operation respects <paramref name="cancellationToken"/>.
        /// </summary>
        public static OperationResult ExecuteExportStandards(
            Document document,
            ExportStandardsRequest request,
            Action<ProgressInfo> onProgress,
            CancellationToken cancellationToken)
        {
            var progress = WorkflowService.CreateCallbackProgress(
                onProgress,
                WorkflowService.EstimateStandardsExportProgressMaximum(request));
            return WorkflowService.ExecuteExportStandards(document, request, cancellationToken, progress);
        }

        /// <summary>
        /// Imports items from a workbook into <paramref name="document"/> without showing
        /// any dialog. Calls <paramref name="onProgress"/> (if not null) to report progress.
        /// The operation respects <paramref name="cancellationToken"/>.
        /// </summary>
        public static OperationResult ExecuteImport(
            Document document,
            ImportRequest request,
            Action<ProgressInfo> onProgress,
            CancellationToken cancellationToken)
        {
            var progress = WorkflowService.CreateCallbackProgress(
                onProgress,
                WorkflowService.EstimateImportProgressMaximum(request));
            return WorkflowService.ExecuteImport(document, request, cancellationToken, progress);
        }
    }
}
