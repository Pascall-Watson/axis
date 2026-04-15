using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Autodesk.Revit.DB;
using ExcelExporterImporter.Common;
using ExcelExporterImporter.Interop;
using ExcelExporterImporter.Support;
using OfficeOpenXml;

namespace ExcelExporterImporter.Services
{
    public class DocumentInventoryService
    {
        public IReadOnlyList<ScheduleInfo> GetExportableSchedules(Document document)
        {
            if (document == null) throw new ArgumentNullException("document");

            var result = new List<ScheduleInfo>();
            foreach (ViewSchedule schedule in new FilteredElementCollector(document).OfClass(typeof(ViewSchedule)))
            {
                if (schedule.IsTitleblockRevisionSchedule)
                    continue;

                result.Add(new ScheduleInfo(
                    schedule.Id.ToString(),
                    schedule.UniqueId,
                    schedule.Name,
                    isTitleblockRevisionSchedule: false,
                    isMaterialTakeoff: schedule.Definition.IsMaterialTakeoff));
            }

            return result;
        }

        public IReadOnlyList<StandardInfo> GetExportableStandards()
        {
            return new List<StandardInfo>
            {
                new StandardInfo(
                    Constants.StandardsGroupLineStylesUniqueId,
                    Constants.StandardsLineStyles,
                    isReadOnly: false,
                    new List<string> { Constants.StandardsGroupItemLineStylesUniqueId }),

                new StandardInfo(
                    Constants.StandardsGroupObjectStylesUniqueId,
                    Constants.StandardsObjectStyles,
                    isReadOnly: false,
                    new List<string>
                    {
                        Constants.StandardsGroupItemAnnotationObjectsUniqueId,
                        Constants.StandardsGroupItemModelObjectsUniqueId,
                        Constants.StandardsGroupItemAnalyticalModelObjectsUniqueId
                    }),

                new StandardInfo(
                    Constants.StandardsGroupFamilyListingUniqueId,
                    Constants.StandardsFamilyListing,
                    isReadOnly: true,
                    new List<string> { Constants.StandardsGroupItemFamilyListingUniqueId }),

                new StandardInfo(
                    Constants.StandardsGroupSharedParametersUniqueId,
                    Constants.StandardsSharedParametersSettings,
                    isReadOnly: true,
                    new List<string>
                    {
                        Constants.StandardsGroupItemProjectSharedParametersSettingsUniqueId
                    }),

                new StandardInfo(
                    Constants.StandardsGroupProjectParametersUniqueId,
                    Constants.StandardsProjectParametersSettings,
                    isReadOnly: true,
                    new List<string>
                    {
                        Constants.StandardsGroupItemProjectParametersSettingsUniqueId
                    }),

                new StandardInfo(
                    Constants.StandardsGroupProjectInformationUniqueId,
                    Constants.StandardsProjectInformation,
                    isReadOnly: false,
                    new List<string>
                    {
                        Constants.StandardsGroupItemProjectInformationUniqueId
                    })
            };
        }

        public ImportWorkbookInspection InspectImportWorkbook(Document document, string filePath)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException("filePath");

            var importableItems = new List<ImportWorkbookItem>();
            var readOnlyItems = new List<ImportWorkbookItem>();
            var isValid = false;

            var schedulesInModel = BuildScheduleLookup(document);
            var knownStandards = BuildKnownStandardsLookup();
            var readonlyStandardIds = BuildReadOnlyStandardIds();

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                foreach (var worksheet in package.Workbook.Worksheets)
                {
                    var uniqueId = Convert.ToString(worksheet.Cells[1, 1].Value);

                    if (Constants.LegendUniqueId == uniqueId)
                    {
                        isValid = true;
                    }
                    else if (schedulesInModel.ContainsKey(uniqueId))
                    {
                        isValid = true;
                        var schedule = schedulesInModel[uniqueId];
                        if (schedule.IsTitleblockRevisionSchedule || schedule.Definition.IsMaterialTakeoff)
                        {
                            readOnlyItems.Add(new ImportWorkbookItem(
                                uniqueId,
                                schedule.Name,
                                canImport: false,
                                reason: "Titleblock revision schedules and material takeoffs cannot be imported."));
                        }
                        else
                        {
                            importableItems.Add(new ImportWorkbookItem(uniqueId, schedule.Name, canImport: true));
                        }
                    }
                    else if (knownStandards.ContainsKey(uniqueId))
                    {
                        isValid = true;
                        var displayName = knownStandards[uniqueId];
                        if (readonlyStandardIds.Contains(uniqueId))
                        {
                            readOnlyItems.Add(new ImportWorkbookItem(
                                uniqueId,
                                displayName,
                                canImport: false,
                                reason: "This standard type is export-only and cannot be imported."));
                        }
                        else
                        {
                            importableItems.Add(new ImportWorkbookItem(uniqueId, displayName, canImport: true));
                        }
                    }
                    else
                    {
                        readOnlyItems.Add(new ImportWorkbookItem(
                            worksheet.Name,
                            worksheet.Name,
                            canImport: false,
                            reason: "Sheet was not recognised as a known schedule or standard."));
                    }
                }
            }

            return new ImportWorkbookInspection(isValid, importableItems, readOnlyItems);
        }

        internal Dictionary<string, ViewSchedule> BuildScheduleLookup(Document document)
        {
            var result = new Dictionary<string, ViewSchedule>();
            foreach (ViewSchedule schedule in new FilteredElementCollector(document).OfClass(typeof(ViewSchedule)))
            {
                result[schedule.UniqueId] = schedule;
            }

            return result;
        }

        internal Dictionary<string, string> BuildKnownStandardsLookup()
        {
            return new Dictionary<string, string>
            {
                { Constants.StandardsGroupItemLineStylesUniqueId, Constants.StandardsLineStyles },
                { Constants.StandardsGroupItemAnnotationObjectsUniqueId, Constants.StandardsAnnotationObjects },
                { Constants.StandardsGroupItemModelObjectsUniqueId, Constants.StandardsModelObjects },
                {
                    Constants.StandardsGroupItemAnalyticalModelObjectsUniqueId,
                    Constants.StandardsAnalyticalModelObjects
                },
                { Constants.StandardsGroupItemSheetListingUniqueId, Constants.StandardsSheetListing },
                { Constants.StandardsGroupItemViewListingUniqueId, Constants.StandardsViewListing },
                { Constants.StandardsGroupItemProjectInformationUniqueId, Constants.StandardsProjectInformation },
                { Constants.StandardsGroupItemMaterialsUniqueId, Constants.StandardsMaterials },
                {
                    Constants.StandardsGroupItemProjectParametersSettingsUniqueId,
                    Constants.StandardsProjectParameters
                },
                {
                    Constants.StandardsGroupItemProjectSharedParametersSettingsUniqueId,
                    Constants.StandardsProjectSharedParameters
                },
                { Constants.StandardsGroupItemFamilyListingUniqueId, Constants.StandardsFamilyListing }
            };
        }

        internal HashSet<string> BuildReadOnlyStandardIds()
        {
            return new HashSet<string>
            {
                Constants.StandardsGroupItemFamilyListingUniqueId,
                Constants.StandardsGroupItemProjectSharedParametersSettingsUniqueId,
                Constants.StandardsGroupItemProjectParametersSettingsUniqueId
            };
        }
    }

    public class BackendWorkflowService
    {
        private const int ImportWorksheetOverheadUnits = 10;
        private const int ScheduleImportFirstDataRow = 5;
        private const int StandardImportFirstDataRow = 4;

        private readonly DocumentInventoryService inventoryService = new DocumentInventoryService();

        public IReadOnlyList<FileInfo> GetExistingFiles(IEnumerable<string> filePaths)
        {
            if (filePaths == null)
                return new List<FileInfo>();

            return filePaths
                .Where(path => !string.IsNullOrEmpty(path))
                .Select(path => new FileInfo(path))
                .Where(fileInfo => fileInfo.Exists)
                .GroupBy(fileInfo => fileInfo.FullName, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();
        }

        public ParametersSettings LoadParametersSettings()
        {
            ParametersSettings settings = null;
            try
            {
                var assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var settingsPath = Path.Combine(assemblyFolder ?? string.Empty, "ParametersSettings.xml");
                ParametersSettings.LoadFromFile(settingsPath, out settings);
            }
            catch (Exception exception)
            {
                SupportLog.Warn(
                    "parameters-settings-load-failed",
                    new Dictionary<string, object>
                    {
                        { "settingsFile", "ParametersSettings.xml" },
                        { "message", exception.Message },
                    });
                settings = null;
            }

            return settings ?? new ParametersSettings();
        }

        public int EstimateScheduleExportProgressMaximum(ExportSchedulesRequest request)
        {
            return Math.Max((request?.ScheduleUniqueIds?.Count ?? 0) * 10, 1);
        }

        public int EstimateStandardsExportProgressMaximum(ExportStandardsRequest request)
        {
            return Math.Max((request?.StandardGroupUniqueIds?.Count ?? 0) * 15, 1);
        }

        public int EstimateImportProgressMaximum(ImportRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.WorkbookFilePath) || request.ItemUniqueIds == null)
                return 1;

            using (var package = new ExcelPackage(new FileInfo(request.WorkbookFilePath)))
            {
                var selectedIds = new HashSet<string>(request.ItemUniqueIds);
                var worksheets = package.Workbook.Worksheets
                    .Where(worksheet => selectedIds.Contains(Convert.ToString(worksheet.Cells[1, 1].Value)))
                    .ToList();

                var rowCounts = worksheets.Sum(GetImportWorkUnits);
                return Math.Max(worksheets.Count * ImportWorksheetOverheadUnits + rowCounts, 1);
            }
        }

        public OperationResult ExecuteExportSchedules(
            Document document,
            ExportSchedulesRequest request,
            CancellationToken cancellationToken,
            Progress progress,
            bool addLegendPerSchedule = false)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (request == null) throw new ArgumentNullException("request");
            if (string.IsNullOrEmpty(request.OutputFilePath))
                return Fail("OutputFilePath must be set.", request.ScheduleUniqueIds == null ? 0 : request.ScheduleUniqueIds.Count);
            if (request.ScheduleUniqueIds == null || request.ScheduleUniqueIds.Count == 0)
                return Fail("No schedules selected for export.", 0);

            EnsureParentDirectoryExists(request.OutputFilePath);

            var schedulesInModel = inventoryService.BuildScheduleLookup(document);
            var parametersSettings = request.UseBasicMode ? null : LoadParametersSettings();
            var errors = new List<string>();
            var warnings = new List<string>();
            var requestedCount = request.ScheduleUniqueIds.Count;
            var succeededCount = 0;
            var skippedCount = 0;

            using (var package = new ExcelPackage(new FileInfo(request.OutputFilePath)))
            {
                var worksheetNames = new Hashtable();
                var scheduleExporter = new ScheduleExporter(cancellationToken);

                foreach (var uniqueId in request.ScheduleUniqueIds)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return OperationResult.Cancelled;

                    if (!schedulesInModel.ContainsKey(uniqueId))
                    {
                        errors.Add("Schedule not found in model: " + uniqueId);
                        skippedCount++;
                        continue;
                    }

                    var schedule = schedulesInModel[uniqueId];
                    var worksheetName = SanitizeWorksheetName(schedule.Name, worksheetNames);
                    worksheetNames[worksheetName] = true;

                    Increment(progress, 5);
                    SetStatus(progress, string.Format(Resources.ExportProgressExporting, schedule.Name));

                    try
                    {
                        var worksheet = package.Workbook.Worksheets.Add(worksheetName);
                        if (request.UseBasicMode)
                            scheduleExporter.ExportViewScheduleBasic(schedule, worksheet);
                        else
                            scheduleExporter.ExportViewSchedule(document, schedule, worksheet, parametersSettings);

                        if (!request.UseBasicMode && addLegendPerSchedule)
                        {
                            var legendWorksheet = package.Workbook.Worksheets.Add(Resources.clLegend);
                            ColorLegend.Add(legendWorksheet);
                        }

                        succeededCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add("Error exporting '" + schedule.Name + "': " + ex.Message);
                        SupportLog.Error(
                            "schedule-export-item-failed",
                            ex,
                            new Dictionary<string, object>
                            {
                                { "scheduleName", schedule.Name },
                                { "scheduleUniqueId", uniqueId },
                                { "targetPath", request.OutputFilePath },
                            });
                    }

                    Increment(progress, 5);
                }

                if (!request.UseBasicMode && !addLegendPerSchedule && !cancellationToken.IsCancellationRequested)
                {
                    var legendWorksheet = package.Workbook.Worksheets.Add(Resources.clLegend);
                    ColorLegend.Add(legendWorksheet);
                }

                package.Save();
            }

            if (request.UseBasicMode)
                warnings.Add("Basic mode export omits color coding and legend sheets by design.");

            return CompleteResult(requestedCount, succeededCount, skippedCount, errors, warnings);
        }

        public OperationResult ExecuteExportStandards(
            Document document,
            ExportStandardsRequest request,
            CancellationToken cancellationToken,
            Progress progress)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (request == null) throw new ArgumentNullException("request");
            if (string.IsNullOrEmpty(request.OutputFilePath))
                return Fail("OutputFilePath must be set.", request.StandardGroupUniqueIds == null ? 0 : request.StandardGroupUniqueIds.Count);
            if (request.StandardGroupUniqueIds == null || request.StandardGroupUniqueIds.Count == 0)
                return Fail("No standard groups selected for export.", 0);

            EnsureParentDirectoryExists(request.OutputFilePath);

            var parametersSettings = LoadParametersSettings();
            var errors = new List<string>();
            var warnings = new List<string>();
            var requestedCount = request.StandardGroupUniqueIds.Count;
            var succeededCount = 0;
            var standardsExporter = new StandardsExporter(cancellationToken);

            using (var package = new ExcelPackage(new FileInfo(request.OutputFilePath)))
            {
                foreach (var standardGroupId in request.StandardGroupUniqueIds)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return OperationResult.Cancelled;

                    Increment(progress, 5);
                    SetStatus(progress, string.Format(Resources.Exporting, standardGroupId));

                    try
                    {
                        standardsExporter.ExportStandard(standardGroupId, document, package.Workbook, parametersSettings);
                        succeededCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add("Error exporting standard '" + standardGroupId + "': " + ex.Message);
                        SupportLog.Error(
                            "standards-export-item-failed",
                            ex,
                            new Dictionary<string, object>
                            {
                                { "standardGroupId", standardGroupId },
                                { "targetPath", request.OutputFilePath },
                            });
                    }

                    Increment(progress, 10);
                }

                package.Save();
            }

            if (request.StandardGroupUniqueIds.Count != succeededCount && errors.Count == 0)
                warnings.Add("One or more standards groups were skipped without a reported backend error.");

            return CompleteResult(requestedCount, succeededCount, 0, errors, warnings);
        }

        public OperationResult ExecuteImport(
            Document document,
            ImportRequest request,
            CancellationToken cancellationToken,
            Progress progress)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (request == null) throw new ArgumentNullException("request");
            if (string.IsNullOrEmpty(request.WorkbookFilePath))
                return Fail("WorkbookFilePath must be set.", request.ItemUniqueIds == null ? 0 : request.ItemUniqueIds.Count);
            if (request.ItemUniqueIds == null || request.ItemUniqueIds.Count == 0)
                return Fail("No items selected for import.", 0);

            var workbookFile = new FileInfo(request.WorkbookFilePath);
            if (!workbookFile.Exists)
                return Fail("The workbook file does not exist: " + request.WorkbookFilePath, request.ItemUniqueIds.Count);
            if (workbookFile.IsFileLocked())
                return Fail("The workbook file is locked or in use: " + request.WorkbookFilePath, request.ItemUniqueIds.Count);

            var parametersSettings = LoadParametersSettings();
            var schedulesInModel = inventoryService.BuildScheduleLookup(document);
            var knownStandards = inventoryService.BuildKnownStandardsLookup();
            var requestedIds = new HashSet<string>(request.ItemUniqueIds);
            var errors = new List<string>();
            var warnings = new List<string>();
            var succeededCount = 0;
            var skippedCount = 0;

            using (var package = new ExcelPackage(workbookFile))
            {
                var selectedWorksheets = package.Workbook.Worksheets
                    .Where(worksheet => requestedIds.Contains(Convert.ToString(worksheet.Cells[1, 1].Value)))
                    .ToList();

                if (selectedWorksheets.Count == 0)
                    return Fail("None of the selected workbook items were found in the workbook.", request.ItemUniqueIds.Count);

                var discoveredIds = new HashSet<string>(selectedWorksheets.Select(worksheet => Convert.ToString(worksheet.Cells[1, 1].Value)));
                foreach (var missingId in requestedIds.Where(id => !discoveredIds.Contains(id)))
                {
                    warnings.Add("Selected workbook item was not found and was skipped: " + missingId);
                    skippedCount++;
                }

                var scheduleImporter = new ScheduleImporter(cancellationToken);
                var standardsImporter = new StandardsImporter(cancellationToken);

                foreach (var worksheet in selectedWorksheets)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return OperationResult.Cancelled;

                    var uniqueId = Convert.ToString(worksheet.Cells[1, 1].Value);

                    if (schedulesInModel.ContainsKey(uniqueId))
                    {
                        var schedule = schedulesInModel[uniqueId];
                        Increment(progress, 5);
                        try
                        {
                            scheduleImporter.ImportViewSchedule(document, worksheet, schedule, progress, parametersSettings);
                            succeededCount++;
                        }
                        catch (Exception ex)
                        {
                            errors.Add(string.Format(Resources.Schedule2, schedule.Name, ex.Message));
                            SupportLog.Error(
                                "schedule-import-item-failed",
                                ex,
                                new Dictionary<string, object>
                                {
                                    { "scheduleName", schedule.Name },
                                    { "scheduleUniqueId", uniqueId },
                                    { "workbookPath", request.WorkbookFilePath },
                                });
                        }

                        Increment(progress, 5);
                    }
                    else if (knownStandards.ContainsKey(uniqueId))
                    {
                        Increment(progress, 5);
                        try
                        {
                            ImportStandard(uniqueId, standardsImporter, document, worksheet, progress, parametersSettings);
                            succeededCount++;
                        }
                        catch (Exception ex)
                        {
                            errors.Add(string.Format(Resources.Standard, knownStandards[uniqueId], ex.Message));
                            SupportLog.Error(
                                "standard-import-item-failed",
                                ex,
                                new Dictionary<string, object>
                                {
                                    { "standardUniqueId", uniqueId },
                                    { "standardName", knownStandards[uniqueId] },
                                    { "workbookPath", request.WorkbookFilePath },
                                });
                        }

                        Increment(progress, 5);
                    }
                    else
                    {
                        skippedCount++;
                        errors.Add("Unrecognised sheet in workbook: " + worksheet.Name);
                    }
                }
            }

            return CompleteResult(request.ItemUniqueIds.Count, succeededCount, skippedCount, errors, warnings);
        }

        internal Progress CreateCallbackProgress(Action<ProgressInfo> callback, int maxValue)
        {
            if (callback == null)
                return null;

            return new CallbackProgress(callback, maxValue);
        }

        private int GetImportWorkUnits(ExcelWorksheet worksheet)
        {
            if (worksheet == null || worksheet.Dimension == null)
                return 0;

            var uniqueId = Convert.ToString(worksheet.Cells[1, 1].Value);
            var firstDataRow = inventoryService.BuildKnownStandardsLookup().ContainsKey(uniqueId)
                ? StandardImportFirstDataRow
                : ScheduleImportFirstDataRow;

            if (worksheet.Dimension.Rows < firstDataRow)
                return 0;

            var count = 0;
            for (var rowIndex = firstDataRow; rowIndex <= worksheet.Dimension.Rows; rowIndex++)
            {
                if (!string.IsNullOrEmpty(Convert.ToString(worksheet.Cells[rowIndex, 1].Value)))
                    count++;
            }

            return count;
        }

        private static OperationResult Fail(string message, int requestedCount)
        {
            return new OperationResult(
                false,
                new List<string> { message },
                null,
                new OperationSummary(requestedCount, 0, 1, 0));
        }

        private static OperationResult CompleteResult(
            int requestedCount,
            int succeededCount,
            int skippedCount,
            IList<string> errors,
            IList<string> warnings)
        {
            var failedCount = errors == null ? 0 : errors.Count;
            return new OperationResult(
                failedCount == 0,
                errors,
                warnings,
                new OperationSummary(requestedCount, succeededCount, failedCount, skippedCount));
        }

        private static void EnsureParentDirectoryExists(string filePath)
        {
            var directoryPath = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
        }

        private static void Increment(Progress progress, int value)
        {
            if (progress == null)
                return;

            progress.Increment(value);
        }

        private static void SetStatus(Progress progress, string status)
        {
            if (progress == null)
                return;

            progress.SetStatus(status);
        }

        private static string SanitizeWorksheetName(string rawName, Hashtable usedNames)
        {
            var name = Regex.Replace(rawName, ":|\\?|/|\\\\|\\[|\\]|\\*", " ");
            name = name.Length > 31 ? name.Substring(0, 28) + "001" : name;

            var suffixNumber = 2;
            while (usedNames[name] != null)
            {
                var suffix = suffixNumber++.ToString().PadLeft(3, '0');
                name = name.Substring(0, Math.Min(name.Length, 28)) + suffix;
            }

            return name;
        }

        private static void ImportStandard(
            string uniqueId,
            StandardsImporter standardsImporter,
            Document document,
            ExcelWorksheet worksheet,
            Progress progress,
            ParametersSettings parametersSettings)
        {
            switch (uniqueId)
            {
                case Constants.StandardsGroupItemLineStylesUniqueId:
                    standardsImporter.ImportLineStyles(document, worksheet, progress);
                    break;
                case Constants.StandardsGroupItemAnnotationObjectsUniqueId:
                    standardsImporter.ImportAnnotationObjects(document, worksheet, progress);
                    break;
                case Constants.StandardsGroupItemModelObjectsUniqueId:
                    standardsImporter.ImportModelObjects(document, worksheet, progress);
                    break;
                case Constants.StandardsGroupItemAnalyticalModelObjectsUniqueId:
                    standardsImporter.ImportAnalyticalModelObjects(document, worksheet, progress);
                    break;
                case Constants.StandardsGroupItemProjectInformationUniqueId:
                    standardsImporter.ImportProjectInformation(document, worksheet, progress, parametersSettings);
                    break;
                case Constants.StandardsGroupItemFamilyListingUniqueId:
                case Constants.StandardsGroupItemProjectSharedParametersSettingsUniqueId:
                case Constants.StandardsGroupItemProjectParametersSettingsUniqueId:
                    break;
                default:
                    throw new ArgumentOutOfRangeException("uniqueId", uniqueId + " " + Resources.IsNotKnownStandardGuid);
            }
        }

        private sealed class CallbackProgress : Progress
        {
            private readonly Action<ProgressInfo> callback;
            private readonly int maxValue;
            private int currentValue;

            public CallbackProgress(Action<ProgressInfo> callback, int maxValue)
            {
                this.callback = callback;
                this.maxValue = maxValue > 0 ? maxValue : 1;
            }

            public override void Increment(int value)
            {
                currentValue = Math.Min(currentValue + value, maxValue);
                Report(null);
            }

            public override void SetStatus(string status)
            {
                Report(status);
            }

            private void Report(string status)
            {
                callback(new ProgressInfo(status, currentValue, maxValue));
            }
        }
    }
}