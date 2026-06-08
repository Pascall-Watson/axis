using System.Collections.Generic;

namespace ExcelExporterImporter.Interop
{
    /// <summary>
    /// Describes a schedule found in the Revit document.
    /// Returned by <see cref="ExcelExporterImporterInterop.GetExportableSchedules"/>.
    /// </summary>
    public sealed class ScheduleInfo
    {
        internal ScheduleInfo(
            string scheduleId,
            string uniqueId,
            string name,
            bool isTitleblockRevisionSchedule,
            bool isMaterialTakeoff)
        {
            ScheduleId = scheduleId;
            UniqueId = uniqueId;
            Name = name;
            IsTitleblockRevisionSchedule = isTitleblockRevisionSchedule;
            IsMaterialTakeoff = isMaterialTakeoff;
        }

        /// <summary>Revit ElementId serialised as a string.</summary>
        public string ScheduleId { get; }

        /// <summary>Revit persistent UniqueId of the ViewSchedule.</summary>
        public string UniqueId { get; }

        /// <summary>Display name of the schedule.</summary>
        public string Name { get; }

        /// <summary>True if the schedule is a titleblock revision schedule.</summary>
        public bool IsTitleblockRevisionSchedule { get; }

        /// <summary>True if the schedule is a material takeoff.</summary>
        public bool IsMaterialTakeoff { get; }
    }

    /// <summary>
    /// Describes a standards group that can be exported from any Revit document.
    /// Returned by <see cref="ExcelExporterImporterInterop.GetExportableStandards"/>.
    /// </summary>
    public sealed class StandardInfo
    {
        internal StandardInfo(
            string groupUniqueId,
            string displayName,
            bool isReadOnly,
            IReadOnlyList<string> itemUniqueIds)
        {
            GroupUniqueId = groupUniqueId;
            DisplayName = displayName;
            IsReadOnly = isReadOnly;
            ItemUniqueIds = itemUniqueIds;
        }

        /// <summary>The group-level GUID used as the key for export operations.</summary>
        public string GroupUniqueId { get; }

        /// <summary>Localised display name for this standards group.</summary>
        public string DisplayName { get; }

        /// <summary>
        /// True when this group is export-only (Family Listing, Shared Parameters,
        /// Project Parameters). Read-only groups cannot be imported back.
        /// </summary>
        public bool IsReadOnly { get; }

        /// <summary>
        /// Worksheet-level GUIDs written into cell A1 of exported sheets for this group.
        /// Used during workbook inspection to identify matching sheets.
        /// </summary>
        public IReadOnlyList<string> ItemUniqueIds { get; }
    }

    /// <summary>
    /// Represents one worksheet found inside an import workbook.
    /// </summary>
    public sealed class ImportWorkbookItem
    {
        internal ImportWorkbookItem(string uniqueId, string displayName, bool canImport, string reason = null)
        {
            UniqueId = uniqueId;
            DisplayName = displayName;
            CanImport = canImport;
            Reason = reason;
        }

        /// <summary>The unique-id read from cell A1 of the worksheet.</summary>
        public string UniqueId { get; }

        /// <summary>Display name (schedule name or standard name).</summary>
        public string DisplayName { get; }

        /// <summary>True if this item can be imported into the target document.</summary>
        public bool CanImport { get; }

        /// <summary>
        /// Human-readable reason why the item cannot be imported.
        /// Null when <see cref="CanImport"/> is true.
        /// </summary>
        public string Reason { get; }
    }

    /// <summary>
    /// Result of inspecting an import workbook.
    /// Returned by <see cref="ExcelExporterImporterInterop.InspectImportWorkbook"/>.
    /// </summary>
    public sealed class ImportWorkbookInspection
    {
        internal ImportWorkbookInspection(
            bool isValidWorkbook,
            IReadOnlyList<ImportWorkbookItem> importableItems,
            IReadOnlyList<ImportWorkbookItem> readOnlyItems)
        {
            IsValidWorkbook = isValidWorkbook;
            ImportableItems = importableItems;
            ReadOnlyItems = readOnlyItems;
        }

        /// <summary>
        /// True if the workbook contains at least one sheet produced by this add-in
        /// (a recognised schedule, standard, or legend sheet).
        /// </summary>
        public bool IsValidWorkbook { get; }

        /// <summary>Items in this workbook that can be selected for import.</summary>
        public IReadOnlyList<ImportWorkbookItem> ImportableItems { get; }

        /// <summary>
        /// Items that were recognised but cannot be imported: titleblock revision
        /// schedules, material takeoffs, read-only standard types, or unrecognised sheets.
        /// </summary>
        public IReadOnlyList<ImportWorkbookItem> ReadOnlyItems { get; }
    }

    /// <summary>
    /// Request to export schedules to an Excel file.
    /// Pass to <see cref="ExcelExporterImporterInterop.ExecuteExportSchedules"/>.
    /// </summary>
    public sealed class ExportSchedulesRequest
    {
        /// <summary>Absolute path of the output .xlsx file. Required.</summary>
        public string OutputFilePath { get; set; }

        /// <summary>
        /// UniqueIds (as returned by <see cref="ScheduleInfo.UniqueId"/>) of the
        /// ViewSchedules to export. Required.
        /// </summary>
        public IList<string> ScheduleUniqueIds { get; set; }

        /// <summary>
        /// When true, uses data-only basic export mode (no colour coding).
        /// When false (default), uses the full export with cell colouring and legend tab.
        /// </summary>
        public bool UseBasicMode { get; set; }

        public ExportSchedulesRequest()
        {
            ScheduleUniqueIds = new List<string>();
        }
    }

    /// <summary>
    /// Request to export standard groups to an Excel file.
    /// Pass to <see cref="ExcelExporterImporterInterop.ExecuteExportStandards"/>.
    /// </summary>
    public sealed class ExportStandardsRequest
    {
        /// <summary>Absolute path of the output .xlsx file. Required.</summary>
        public string OutputFilePath { get; set; }

        /// <summary>
        /// GroupUniqueIds (as returned by <see cref="StandardInfo.GroupUniqueId"/>) of
        /// the standard groups to export. Required.
        /// </summary>
        public IList<string> StandardGroupUniqueIds { get; set; }

        public ExportStandardsRequest()
        {
            StandardGroupUniqueIds = new List<string>();
        }
    }

    /// <summary>
    /// Request to import data from a workbook into Revit.
    /// Pass to <see cref="ExcelExporterImporterInterop.ExecuteImport"/>.
    /// </summary>
    public sealed class ImportRequest
    {
        /// <summary>Absolute path of the source .xlsx file. Required.</summary>
        public string WorkbookFilePath { get; set; }

        /// <summary>
        /// UniqueIds (as returned by <see cref="ImportWorkbookItem.UniqueId"/>) of the
        /// items to import. Required.
        /// </summary>
        public IList<string> ItemUniqueIds { get; set; }

        public ImportRequest()
        {
            ItemUniqueIds = new List<string>();
        }
    }

    /// <summary>
    /// Result returned by the Execute* service methods.
    /// </summary>
    public sealed class OperationResult
    {
        /// <summary>Returns a pre-built result representing a cancelled operation.</summary>
        public static OperationResult Cancelled
        {
            get
            {
                return new OperationResult(
                    false,
                    new List<string>(),
                    new List<string> { "Operation was cancelled." },
                    new OperationSummary(0, 0, 0, 0),
                    isCancelled: true);
            }
        }

        public OperationResult(bool success, IList<string> errors, IList<string> warnings)
            : this(success, errors, warnings, null, false, null, null, null)
        {
        }

        public OperationResult(bool success, IList<string> errors, IList<string> warnings, OperationSummary summary)
            : this(success, errors, warnings, summary, false, null, null, null)
        {
        }

        public OperationResult(
            bool success,
            IList<string> errors,
            IList<string> warnings,
            OperationSummary summary,
            bool isCancelled,
            string operationName = null,
            string operationId = null,
            string logFilePath = null)
        {
            Success = success;
            Errors = errors != null ? new List<string>(errors) : new List<string>();
            Warnings = warnings != null ? new List<string>(warnings) : new List<string>();
            Summary = summary;
            IsCancelled = isCancelled;
            OperationName = operationName;
            OperationId = operationId;
            LogFilePath = logFilePath;
        }

        /// <summary>True when the operation completed without errors.</summary>
        public bool Success { get; }

        /// <summary>True when the operation was cancelled by the user.</summary>
        public bool IsCancelled { get; }

        /// <summary>Error messages. Empty when the operation succeeded.</summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>Warning messages that do not indicate failure.</summary>
        public IReadOnlyList<string> Warnings { get; }

        /// <summary>Execution counts describing the completed workflow.</summary>
        public OperationSummary Summary { get; }

        /// <summary>Stable workflow name used for logging and support triage.</summary>
        public string OperationName { get; }

        /// <summary>Correlation id for the backend operation instance.</summary>
        public string OperationId { get; }

        /// <summary>Path to the backend support log file written by the add-in.</summary>
        public string LogFilePath { get; }

        internal OperationResult WithSupportContext(string operationName, string operationId, string logFilePath)
        {
            return new OperationResult(
                Success,
                new List<string>(Errors),
                new List<string>(Warnings),
                Summary,
                IsCancelled,
                operationName,
                operationId,
                logFilePath);
        }
    }

    /// <summary>
    /// Aggregated counts for an export or import workflow.
    /// </summary>
    public sealed class OperationSummary
    {
        public OperationSummary(int requestedCount, int succeededCount, int failedCount, int skippedCount)
        {
            RequestedCount = requestedCount;
            SucceededCount = succeededCount;
            FailedCount = failedCount;
            SkippedCount = skippedCount;
        }

        public int RequestedCount { get; }

        public int SucceededCount { get; }

        public int FailedCount { get; }

        public int SkippedCount { get; }
    }

    /// <summary>
    /// Progress notification delivered to the <c>onProgress</c> callback
    /// in Execute* service methods.
    /// </summary>
    public sealed class ProgressInfo
    {
        internal ProgressInfo(string message, int value, int maxValue)
        {
            Message = message;
            Value = value;
            MaxValue = maxValue;
        }

        /// <summary>Human-readable status message. May be null.</summary>
        public string Message { get; }

        /// <summary>Current progress value (0 to <see cref="MaxValue"/>).</summary>
        public int Value { get; }

        /// <summary>Total work units for this operation.</summary>
        public int MaxValue { get; }
    }
}
