using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using Autodesk.Revit.DB;
using ExcelExporterImporter.Annotations;
using ExcelExporterImporter.Common;
using ExcelExporterImporter.Interop;
using ExcelExporterImporter.Services;
using log4net;
using Ookii.Dialogs.Wpf;
using MessageBox = System.Windows.MessageBox;
using TaskDialog = Ookii.Dialogs.Wpf.TaskDialog;
using TaskDialogButton = Ookii.Dialogs.Wpf.TaskDialogButton;
using TaskDialogIcon = Ookii.Dialogs.Wpf.TaskDialogIcon;

namespace ExcelExporterImporter.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly BackendWorkflowService backendWorkflowService;
        private readonly DocumentInventoryService documentInventoryService;
        private readonly Document revitDocument;
        private readonly Window window;

        private BackgroundWorker backgroundWorker;
        private bool bCheckAllImport;
        private string buttonText;
        private CancellationTokenSource cancellationTokenSource;
        private bool checkExportSchedule;
        private bool checkExportStandard;
        private Dispatcher dispatcher;
        private bool enableButtons;
        private bool enableButtonsBasic;
        private ExportOptions exportOption = ExportOptions.SeparateTables;
        private ExportOptionsBasic exportOptionBasic = ExportOptionsBasic.SeparateTables;
        private string exportPrefix;
        private string exportPrefixBasic;
        private FileInfo fiExportFile;
        private string importFolder;
        private Progress progress;
        private int selectedTab;

        private string sFileExport;
        private string sIconButton;
        private string sShowExportButton;
        private string sShowImportButton;
        private string title;
        private bool useExportPrefix;
        private bool useExportPrefixBasic;

        /// <summary>
        /// </summary>
        /// <param name="window"></param>
        /// <param name="revitDocument"></param>
        public MainViewModel(Window window, Document revitDocument) : this()
        {
            documentInventoryService = new DocumentInventoryService();
            backendWorkflowService = new BackendWorkflowService();
            this.revitDocument = revitDocument;
            this.window = window;
            SelectedTab = 0;
            ShowExportButton = "Collapsed";
            ShowImportButton = "Visible";
            Command = new DelegateCommand<object>(OnSubmit, CanSubmit);
            Title = AddinInfo.AddinName + " " + Assembly.GetAssembly(GetType()).GetName().Version;
            EnableButtons = true;
            EnableButtonsBasic = true;
            ButtonText = Resources.TitleBtnExport;
            IconButton = Constants.IconExportButton;
            FillLists();
        }

        /// <summary>
        /// </summary>
        public MainViewModel()
        {
            SchedulesList = new ObservableCollection<CheckedListItem<string>>();
            SchedulesListBasic = new ObservableCollection<CheckedListItem<string>>();

            StandardsList = new ObservableCollection<CheckedListItem<string>>();
            ImportItemList = new ObservableCollection<CheckedListItem<string>>();
            NotImportItemList = new ObservableCollection<CheckedListItem<string>>();

            OrderedSchedulesForExport = CollectionViewSource.GetDefaultView(SchedulesList);
            OrderedSchedulesForExport.SortDescriptions.Add(new SortDescription("Item", ListSortDirection.Ascending));

            OrderedSchedulesForExportBasic = CollectionViewSource.GetDefaultView(SchedulesListBasic);
            OrderedSchedulesForExportBasic.SortDescriptions.Add(
                new SortDescription("Item", ListSortDirection.Ascending));

            OrderedItemsForImport = CollectionViewSource.GetDefaultView(ImportItemList);
            OrderedItemsForImport.SortDescriptions.Add(new SortDescription("Item", ListSortDirection.Ascending));

            OrderedItemsForNotImport = CollectionViewSource.GetDefaultView(NotImportItemList);
            OrderedItemsForNotImport.SortDescriptions.Add(new SortDescription("Item", ListSortDirection.Ascending));
        }

        public ICollectionView OrderedSchedulesForExport { get; set; }
        public ICollectionView OrderedSchedulesForExportBasic { get; set; }
        public ICollectionView OrderedItemsForImport { get; set; }
        public ICollectionView OrderedItemsForNotImport { get; set; }
        public ObservableCollection<CheckedListItem<string>> SchedulesList { get; set; }
        public ObservableCollection<CheckedListItem<string>> SchedulesListBasic { get; set; }
        public ObservableCollection<CheckedListItem<string>> StandardsList { get; set; }
        public ObservableCollection<CheckedListItem<string>> ImportItemList { get; set; }
        public ObservableCollection<CheckedListItem<string>> NotImportItemList { get; set; }

        public ICommand Command { get; }
        public Action CloseAction { get; set; }

        public string Title
        {
            get => title;
            set
            {
                if (value == title) return;
                title = value;
                OnPropertyChanged();
            }
        }

        public bool EnableButtons
        {
            get => enableButtons;
            set
            {
                if (value.Equals(enableButtons)) return;
                enableButtons = value;
                OnPropertyChanged();
            }
        }

        public bool EnableButtonsBasic
        {
            get => enableButtonsBasic;
            set
            {
                if (value.Equals(enableButtonsBasic)) return;
                enableButtonsBasic = value;
                OnPropertyChanged();
            }
        }

        public int SelectedTab
        {
            get => selectedTab;
            set
            {
                selectedTab = value;
                ButtonText = value == 1 ? Resources.TitleBtnImport : Resources.TitleBtnExport;
                IconButton = value == 1 ? Constants.IconImportButton : Constants.IconExportButton;
                OnPropertyChanged();
            }
        }

        public string ButtonText
        {
            get => buttonText;
            set
            {
                buttonText = value;
                OnPropertyChanged();
            }
        }

        public string IconButton
        {
            get => sIconButton;
            set
            {
                sIconButton = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Parameter which contains the path of the last directory used for an import.
        /// </summary>
        public string LastImportFolder
        {
            get
            {
                if (string.IsNullOrEmpty(Settings.Default.LastImportPath))
                {
                    if (string.IsNullOrEmpty(Settings.Default.LastExportPath))
                        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    return Settings.Default.LastExportPath;
                }

                return Settings.Default.LastImportPath;
            }
            set
            {
                if (value != Settings.Default.LastImportPath)
                {
                    Settings.Default.LastImportPath = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        ///     Parameter which contains the path of the last directory used for an export.
        /// </summary>
        public string ExportFolder
        {
            get
            {
                if (string.IsNullOrEmpty(Settings.Default.LastExportPath))
                {
                    if (string.IsNullOrEmpty(Settings.Default.LastImportPath))
                        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    return Settings.Default.LastImportPath;
                }

                return Settings.Default.LastExportPath;
            }
            set
            {
                if (value != Settings.Default.LastExportPath)
                {
                    Settings.Default.LastExportPath = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ExportFolderBasic
        {
            get => Settings.Default.LastExportPathBasic;
            set
            {
                if (value != Settings.Default.LastExportPathBasic)
                {
                    Settings.Default.LastExportPathBasic = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ImportFolder
        {
            get => importFolder;
            set
            {
                importFolder = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Property that contains the visibility value for the Export button
        /// </summary>
        public string ShowExportButton
        {
            get => sShowExportButton;
            set
            {
                sShowExportButton = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Property that contains the visibility value for the Import button
        /// </summary>
        public string ShowImportButton
        {
            get => sShowImportButton;
            set
            {
                sShowImportButton = value;
                OnPropertyChanged();
            }
        }

        public bool CheckAllImport
        {
            get => bCheckAllImport;
            set
            {
                bCheckAllImport = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Property to manage the value of the check box for all selected schedules
        /// </summary>
        public bool CheckExportSchedule
        {
            get => checkExportSchedule;
            set
            {
                checkExportSchedule = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Property to manage the value of the check box for all selected standard
        /// </summary>
        public bool CheckExportStandard
        {
            get => checkExportStandard;
            set
            {
                checkExportStandard = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Property to manage the value of the export mode uni
        /// </summary>
        public bool ExportModeUni
        {
            get => Settings.Default.LastExportModeUni;
            set
            {
                if (value) ExportModeBid = false;
                Settings.Default.LastExportModeUni = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Property to manage the value of the export mode bi
        /// </summary>
        public bool ExportModeBid
        {
            get
            {
                if (!Settings.Default.LastExportModeUni && !Settings.Default.LastExportModeBid) return true;
                return Settings.Default.LastExportModeBid;
            }
            set
            {
                if (value) ExportModeUni = false;
                Settings.Default.LastExportModeBid = value;
                OnPropertyChanged();
            }
        }

        public bool UseExportPrefix
        {
            get => useExportPrefix;
            set
            {
                useExportPrefix = value;
                OnPropertyChanged();
            }
        }

        public bool UseExportPrefixBasic
        {
            get => useExportPrefixBasic;
            set
            {
                useExportPrefixBasic = value;
                OnPropertyChanged();
            }
        }

        public string ExportPrefix
        {
            get => exportPrefix;
            set
            {
                exportPrefix = value;
                OnPropertyChanged();
            }
        }

        public string ExportPrefixBasic
        {
            get => exportPrefixBasic;
            set
            {
                exportPrefixBasic = value;
                OnPropertyChanged();
            }
        }

        public ExportOptions ExportOption
        {
            get => exportOption;
            set
            {
                exportOption = value;
                OnPropertyChanged();
            }
        }

        public ExportOptionsBasic ExportOptionBasic
        {
            get => exportOptionBasic;
            set
            {
                exportOptionBasic = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        ///     Method for filling the lists for basic export and advanced export
        /// </summary>
        private void FillLists()
        {
            foreach (var schedule in documentInventoryService.GetExportableSchedules(revitDocument))
            {
                SchedulesList.Add(new CheckedListItem<string>(schedule.UniqueId, schedule.Name, schedule));
                SchedulesListBasic.Add(new CheckedListItem<string>(schedule.UniqueId, schedule.Name, schedule));
            }

            StandardsList.Clear();
            foreach (var standard in documentInventoryService.GetExportableStandards())
            {
                StandardsList.Add(new CheckedListItem<string>(standard.GroupUniqueId, standard.DisplayName, standard));
            }
        }

        /// <summary>
        ///     Handle the Click event of the bind buttons
        /// </summary>
        /// <param name="arg"></param>
        private void OnSubmit(object arg)
        {
            switch (arg.ToString())
            {
                case "OK":
                    if (SelectedTab == 1)
                    {
                        Import();
                    }
                    else if (SelectedTab == 0)
                    {
                        if (ExportModeBid)
                            Export("Schedules");
                        else
                            Export("Basic");
                    }

                    break;
                case "Cancel":
                    CloseAction();
                    break;
                case "ExportSchedulesCheck":
                    if (CheckExportSchedule)
                        CheckAllItems(SchedulesList, true);
                    else
                        CheckAllItems(SchedulesList, false);
                    break;
                case "ExportStandardCheck":
                    if (CheckExportStandard)
                        CheckAllItems(StandardsList, true);
                    else
                        CheckAllItems(StandardsList, false);
                    break;
                case "ImportCheck":
                    if (CheckAllImport)
                        CheckAllItems(ImportItemList, true);
                    else
                        CheckAllItems(ImportItemList, false);
                    break;
                case "ShowExportTab":
                    SelectedTab = 0;
                    ShowExportButton = "Collapsed";
                    ShowImportButton = "Visible";
                    break;
                case "ShowImportTab":
                    SelectedTab = 1;
                    ShowExportButton = "Visible";
                    ShowImportButton = "Collapsed";
                    break;

                case "BrowseExportFolder":
                    var folderBrowserDialog = new VistaFolderBrowserDialog();
                    folderBrowserDialog.SelectedPath = ExportFolder;
                    var dialogResult = folderBrowserDialog.ShowDialog();
                    if (dialogResult.HasValue && dialogResult.Value) ExportFolder = folderBrowserDialog.SelectedPath;

                    break;
                case "BrowseImportFolder":
                    SelectImportFolder();
                    break;
            }
        }

        /// <summary>
        ///     Import method
        /// </summary>
        private void Import()
        {
            backgroundWorker = new BackgroundWorker();
            dispatcher = Dispatcher.CurrentDispatcher;

            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.ProgressChanged += Worker_ProgressChanged;
            backgroundWorker.RunWorkerCompleted += RunWorkerCompleted;

            progress = new Progress();
            progress.ProcessCanceled += OnProcessCanceled;
            EnableButtons = false;

            backgroundWorker.DoWork += ImportWorker;
            backgroundWorker.RunWorkerAsync();
        }

        /// <summary>
        /// </summary>
        /// <param name="sTypeExport">Schedules or Basic</param>
        private void Export(string sTypeExport)
        {
            if (string.IsNullOrEmpty(sTypeExport)) sTypeExport = "Schedules";
            var schedules = SchedulesList.Where(sl => sl.IsChecked).ToList();
            var standards = StandardsList.Where(s => s.IsChecked).ToList();

            var sRevitFilename = Path.GetFileNameWithoutExtension(revitDocument.PathName);
            if (string.IsNullOrEmpty(sRevitFilename))
            {
                if (string.IsNullOrEmpty(revitDocument.Title))
                    sRevitFilename = "Default";
                else
                    sRevitFilename = revitDocument.Title;
            }

            //Validate that he had a selection of facts at the list level
            if (!schedules.Any() && !standards.Any())
            {
                MessageBox.Show(window, Resources.NothingToExportMessage, Resources.NothingToExportTitle,
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            //If there are items in the schedules list
            if (schedules.Any())
            {
                var sFileName = sRevitFilename + "_Schedules.xlsx";
                var ExporttDlg = new SaveFileDialog
                {
                    Filter = @"Excel Files|*.xlsx;*.xls", CheckFileExists = false, RestoreDirectory = true,
                    InitialDirectory = ExportFolder, FileName = sFileName
                };
                if (ExporttDlg.ShowDialog() == DialogResult.OK)
                {
                    fiExportFile = new FileInfo(ExporttDlg.FileName);
                    ExportFolder = fiExportFile.DirectoryName;
                    var filesToOverwrite = backendWorkflowService.GetExistingFiles(new[] { fiExportFile.FullName }).ToList();
                    if (DeleteExistFile(filesToOverwrite))
                    {
                        sFileExport = sTypeExport;
                        ExecuteExport();
                    }
                }
                else
                {
                    return;
                }
            }

            //If there are checked items in the standard list
            if (standards.Any())
            {
                var sFileName = sRevitFilename + "_Standards.xlsx";
                var ExporttDlg = new SaveFileDialog
                {
                    Filter = @"Excel Files|*.xlsx;*.xls", CheckFileExists = false, RestoreDirectory = true,
                    InitialDirectory = ExportFolder, FileName = sFileName
                };
                if (ExporttDlg.ShowDialog() == DialogResult.OK)
                {
                    fiExportFile = new FileInfo(ExporttDlg.FileName);
                    ExportFolder = fiExportFile.DirectoryName;
                    var filesToOverwrite = backendWorkflowService.GetExistingFiles(new[] { fiExportFile.FullName }).ToList();
                    if (DeleteExistFile(filesToOverwrite))
                    {
                        sFileExport = "Standards";
                        ExecuteExport();
                    }
                }
                else
                {
                }
            }
        }

        /// <summary>
        ///     Initializes the progress bar and executes the method which performs the export
        /// </summary>
        private void ExecuteExport()
        {
            backgroundWorker = new BackgroundWorker();
            dispatcher = Dispatcher.CurrentDispatcher;

            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.ProgressChanged += Worker_ProgressChanged;

            backgroundWorker.RunWorkerCompleted += RunWorkerCompleted;
            progress = new Progress();
            progress.ProcessCanceled += OnProcessCanceled;
            EnableButtons = false;

            backgroundWorker.DoWork += ExportWorker; //On appel la fonction qui sert à l'exportation
            backgroundWorker.RunWorkerAsync();
        }

        /// <summary>
        ///     Erases existing files
        /// </summary>
        /// <param name="filesToOverwrite"></param>
        /// <returns></returns>
        private bool DeleteExistFile(List<FileInfo> filesToOverwrite)
        {
            if (filesToOverwrite.Any())
            {
                var taskDialog = new TaskDialog();
                taskDialog.ButtonStyle = TaskDialogButtonStyle.CommandLinks;
                taskDialog.WindowTitle = Resources.OverwriteTargetFiles;
                taskDialog.MainInstruction = Resources.TargetFileAlreadyExists;
                taskDialog.Content =
                    string.Format(Resources.TheTargetFolderAlreadyContainsFiles, filesToOverwrite.Count);
                taskDialog.ExpandedByDefault = false;
                taskDialog.ExpandFooterArea = false;
                taskDialog.Footer = string.Join(Environment.NewLine, filesToOverwrite.Select(f => f.Name));
                taskDialog.AllowDialogCancellation = true;

                var deleteFilesButton = new TaskDialogButton(ButtonType.Custom);
                deleteFilesButton.Text = Resources.OverwriteTheExistingFiles;

                var cancelButton = new TaskDialogButton(ButtonType.Custom);
                cancelButton.Text = Resources.CancelTheExportProcess;
                cancelButton.Default = true;

                taskDialog.Buttons.Add(deleteFilesButton);
                taskDialog.Buttons.Add(cancelButton);

                var taskDialogButton = taskDialog.ShowDialog(window);
                if (taskDialogButton == null || taskDialogButton == cancelButton)
                    //If the user to click cancel, we stop the export
                    return false;

                if (taskDialogButton == deleteFilesButton)
                    // We delete existing files
                    foreach (var fileInfo in filesToOverwrite)
                        try
                        {
                            fileInfo.Delete();
                        }
                        catch (Exception)
                        {
                            MessageBox.Show(string.Format(Resources.CouldNotDeleteFile, fileInfo.Name),
                                Resources.UnableToDeleteFile, MessageBoxButton.OK, MessageBoxImage.Warning);
                            return false;
                        }
            }

            return true;
        }

        /// <summary>
        ///     Worker progress changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
        }

        /// <summary>
        ///     Change value of chancellationTOkenSource
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void OnProcessCanceled(object sender, EventArgs eventArgs)
        {
            cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// </summary>
        private void SelectImportFolder()
        {
            var importDlg = new OpenFileDialog
            {
                Filter = @"Excel Files|*.xlsx;*.xls", CheckFileExists = true, RestoreDirectory = true,
                InitialDirectory = LastImportFolder
            };
            var bExcelFileValid = false;
            if (importDlg.ShowDialog(window) == DialogResult.OK)
            {
                ImportFolder = importDlg.FileName;

                var fileInfo = new FileInfo(ImportFolder);
                LastImportFolder = fileInfo.DirectoryName;
                if (fileInfo.IsFileLocked())
                {
                    MessageBox.Show(window, Resources.FileInUseMessage, Resources.FileInUseTitle, MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    ImportFolder = "";
                    return;
                }

                ImportItemList.Clear();
                NotImportItemList.Clear();
                CheckAllImport = false;

                var inspection = documentInventoryService.InspectImportWorkbook(revitDocument, ImportFolder);
                bExcelFileValid = inspection.IsValidWorkbook;

                foreach (var importableItem in inspection.ImportableItems)
                {
                    ImportItemList.Add(new CheckedListItem<string>(
                        importableItem.UniqueId,
                        importableItem.DisplayName,
                        importableItem));
                }

                foreach (var readOnlyItem in inspection.ReadOnlyItems)
                {
                    NotImportItemList.Add(new CheckedListItem<string>(
                        readOnlyItem.UniqueId,
                        readOnlyItem.DisplayName,
                        readOnlyItem));
                }

                if (ImportItemList.Count == 0)
                {
                    if (bExcelFileValid == false)
                        MessageBox.Show(window, Resources.InvalidExcelFileMessage, Resources.InvalidExcelFile,
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    else
                        MessageBox.Show(window, Resources.ImportUnavailableMsg, Resources.ImportUnavailable,
                            MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        ///     Select or deselect all items
        /// </summary>
        /// <param name="list"></param>
        /// <param name="check"></param>
        public void CheckAllItems(ObservableCollection<CheckedListItem<string>> list, bool check)
        {
            foreach (var checkedListItem in list) checkedListItem.IsChecked = check;
        }

        /// <summary>
        ///     Not used - This is part of ICommand implementation can be used to control
        ///     the buttons click behavior (Can fire the click event or not)
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private bool CanSubmit(object arg)
        {
            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExportWorker(object sender, DoWorkEventArgs e)
        {
            dispatcher.Invoke(() =>
            {
                OperationResult result = null;
                cancellationTokenSource = new CancellationTokenSource();
                var schedules = SchedulesList.Where(sl => sl.IsChecked).ToList();
                var standards = StandardsList.Where(s => s.IsChecked).ToList();

                try
                {
                    if (sFileExport == "Schedules")
                    {
                        var request = new ExportSchedulesRequest
                        {
                            OutputFilePath = fiExportFile.FullName,
                            UseBasicMode = false
                        };
                        foreach (var schedule in schedules)
                            request.ScheduleUniqueIds.Add(schedule.Id);

                        progress.Start(backendWorkflowService.EstimateScheduleExportProgressMaximum(request),
                            "Exporting Revit Schedules");
                        result = backendWorkflowService.ExecuteExportSchedules(
                            revitDocument,
                            request,
                            cancellationTokenSource.Token,
                            progress,
                            ExportOption == ExportOptions.SeparateFiles);
                    }
                    else if (sFileExport == "Standards")
                    {
                        var request = new ExportStandardsRequest
                        {
                            OutputFilePath = fiExportFile.FullName
                        };
                        foreach (var standard in standards)
                            request.StandardGroupUniqueIds.Add(standard.Id);

                        progress.Start(backendWorkflowService.EstimateStandardsExportProgressMaximum(request),
                            "Exporting Revit Schedules");
                        result = backendWorkflowService.ExecuteExportStandards(
                            revitDocument,
                            request,
                            cancellationTokenSource.Token,
                            progress);
                    }
                    else if (sFileExport == "Basic")
                    {
                        var request = new ExportSchedulesRequest
                        {
                            OutputFilePath = fiExportFile.FullName,
                            UseBasicMode = true
                        };
                        foreach (var schedule in schedules)
                            request.ScheduleUniqueIds.Add(schedule.Id);

                        progress.Start(backendWorkflowService.EstimateScheduleExportProgressMaximum(request),
                            "Exporting Revit Schedules");
                        result = backendWorkflowService.ExecuteExportSchedules(
                            revitDocument,
                            request,
                            cancellationTokenSource.Token,
                            progress);
                    }
                }
                catch (Exception exception)
                {
                    Logger.Error(exception.Message, exception);
                    MessageBox.Show(window, string.Format(Resources.ExportErrorMessage, exception.Message),
                        Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    progress.End();
                }

                if (result == null || cancellationTokenSource.IsCancellationRequested)
                    return;

                if (result.Errors.Any())
                {
                    ShowExportErrors(result.Errors);
                }
                else
                {
                    MessageBox.Show(window, Resources.ExportProcessCompleteMessage, Resources.ProcessCompleteTitle,
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            });
        }

        /// <summary>
        ///     Import worker
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImportWorker(object sender, DoWorkEventArgs e)
        {
            dispatcher.Invoke(() =>
            {
                try
                {
                    OperationResult result;
                    cancellationTokenSource = new CancellationTokenSource();
                    if (string.IsNullOrEmpty(ImportFolder))
                    {
                        MessageBox.Show(window, Resources.ImportNoFileSelected, Resources.ImportNoFileSelectedTitle,
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (!ImportItemList.Any(i => i.IsChecked))
                    {
                        MessageBox.Show(window, Resources.ImportNoSelection, Resources.ImportNoSelectionTitle,
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var request = new ImportRequest
                    {
                        WorkbookFilePath = ImportFolder
                    };

                    foreach (var importItem in ImportItemList.Where(i => i.IsChecked))
                    {
                        request.ItemUniqueIds.Add(importItem.Id);
                    }

                    progress.Start(backendWorkflowService.EstimateImportProgressMaximum(request),
                        Resources.ProgressImportingExcelData);
                    result = backendWorkflowService.ExecuteImport(
                        revitDocument,
                        request,
                        cancellationTokenSource.Token,
                        progress);

                    progress.End();

                    if (result.Errors.Any())
                        ShowImportErrors(result.Errors);
                    else
                        MessageBox.Show(window, Resources.ImportProcessCompleteMessage, Resources.ProcessCompleteTitle,
                            MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception exception)
                {
                    if (progress != null) progress.End();
                    Logger.Error(exception.Message, exception);
                    MessageBox.Show(window, string.Format(Resources.ImportErrorMessage, exception.Message),
                        Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
        }

        /// <summary>
        ///     Run worker completed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ImportFolder = string.Empty;
            ImportItemList.Clear();
            NotImportItemList.Clear();
            CheckAllImport = false;

            EnableButtons = true;
        }

        private void ShowExportErrors(IEnumerable<string> errors)
        {
            using (var td = new TaskDialog())
            {
                td.WindowTitle = Resources.CompletedWithErrors;
                td.Content = string.Format(Resources.ExportErrorMessage, Resources.CompletedWithErrors);
                td.MainIcon = TaskDialogIcon.Warning;
                td.MainInstruction = Resources.CompletedWithErrors;
                td.ExpandedInformation = string.Join(Environment.NewLine, errors);
                td.CollapsedControlText = Resources.ViewDetails;
                td.Buttons.Add(new TaskDialogButton(ButtonType.Ok));
                td.ShowDialog(window);
            }
        }

        private void ShowImportErrors(IEnumerable<string> errors)
        {
            using (var td = new TaskDialog())
            {
                td.WindowTitle = Resources.CompletedWithErrors;
                td.Content = Resources.TheImportProcessWasCompletedWithErrors;
                td.MainIcon = TaskDialogIcon.Warning;
                td.MainInstruction = Resources.SomeElementsCouldNotBeImported;
                td.ExpandedInformation = string.Join(Environment.NewLine, errors);
                td.CollapsedControlText = Resources.ViewDetails;
                td.Buttons.Add(new TaskDialogButton(ButtonType.Ok));
                td.ShowDialog(window);
            }
        }

        /// <summary>
        ///     Property changed
        /// </summary>
        /// <param name="propertyName"></param>
        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}