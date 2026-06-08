# -*- coding: utf-8 -*-

import clr

clr.AddReference("PresentationFramework")
clr.AddReference("WindowsBase")

from System.Collections.ObjectModel import ObservableCollection
from System.ComponentModel import INotifyPropertyChanged
from System.ComponentModel import PropertyChangedEventArgs
from System.Windows.Input import ICommand
from System.Windows.Input import CommandManager

from default.models.check_item import CheckItem


class RelayCommand(ICommand):
    def __init__(self, execute, can_execute=None):
        self._execute = execute
        self._can_execute = can_execute or (lambda _: True)

    def Execute(self, parameter):
        self._execute(parameter)

    def CanExecute(self, parameter):
        return self._can_execute(parameter)

    def add_CanExecuteChanged(self, handler):
        CommandManager.RequerySuggested += handler

    def remove_CanExecuteChanged(self, handler):
        CommandManager.RequerySuggested -= handler


class MainViewModel(INotifyPropertyChanged):
    def __init__(self, title):
        self._property_changed_handlers = []
        self._title = title
        self._selected_tab = 0
        self._selected_export_mode = "Worksheets"
        self._export_path = ""
        self._workbook_path = ""
        self._check_export_schedules_all = False
        self._check_import_all = False
        self._status_message = "Ready."
        self._action_handler = None

        self.ExportSchedules = ObservableCollection[CheckItem]()
        self.ImportItems = ObservableCollection[CheckItem]()
        self.ExportModes = ObservableCollection[str]()
        self.ExportModes.Add("Worksheets")
        self.ExportModes.Add("Workbooks")

        self.RunCommand = RelayCommand(lambda _: self._request_action("Run"))
        self.CancelCommand = RelayCommand(lambda _: self._request_action("Cancel"))
        self.BrowseExportPathCommand = RelayCommand(lambda _: self._request_action("BrowseExportPath"))
        self.BrowseWorkbookPathCommand = RelayCommand(lambda _: self._request_action("BrowseWorkbookPath"))

    def set_action_handler(self, handler):
        self._action_handler = handler

    def _request_action(self, action_name):
        if self._action_handler is not None:
            self._action_handler(action_name, self)

    @property
    def Title(self):
        return self._title

    @property
    def SelectedTab(self):
        return self._selected_tab

    @SelectedTab.setter
    def SelectedTab(self, value):
        if self._selected_tab == value:
            return
        self._selected_tab = value
        self._notify("SelectedTab")
        self._notify("RunButtonText")

    @property
    def RunButtonText(self):
        if self._selected_tab == 1:
            return "Import"
        return "Export"

    @property
    def StatusMessage(self):
        return self._status_message

    @StatusMessage.setter
    def StatusMessage(self, value):
        next_value = value or ""
        if self._status_message == next_value:
            return
        self._status_message = next_value
        self._notify("StatusMessage")

    @property
    def SelectedExportMode(self):
        return self._selected_export_mode

    @SelectedExportMode.setter
    def SelectedExportMode(self, value):
        next_value = value or "Worksheets"
        if self._selected_export_mode == next_value:
            return
        self._selected_export_mode = next_value
        self._notify("SelectedExportMode")

    @property
    def ExportPath(self):
        return self._export_path

    @ExportPath.setter
    def ExportPath(self, value):
        next_value = value or ""
        if self._export_path == next_value:
            return
        self._export_path = next_value
        self._notify("ExportPath")

    @property
    def WorkbookPath(self):
        return self._workbook_path

    @WorkbookPath.setter
    def WorkbookPath(self, value):
        next_value = value or ""
        if self._workbook_path == next_value:
            return
        self._workbook_path = next_value
        self._notify("WorkbookPath")

    @property
    def CheckExportSchedulesAll(self):
        return self._check_export_schedules_all

    @CheckExportSchedulesAll.setter
    def CheckExportSchedulesAll(self, value):
        new_value = bool(value)
        if self._check_export_schedules_all == new_value:
            return
        self._check_export_schedules_all = new_value
        for item in self.ExportSchedules:
            if item.IsEnabled:
                item.IsChecked = new_value
        self._notify("CheckExportSchedulesAll")

    @property
    def CheckImportAll(self):
        return self._check_import_all

    @CheckImportAll.setter
    def CheckImportAll(self, value):
        new_value = bool(value)
        if self._check_import_all == new_value:
            return
        self._check_import_all = new_value
        for item in self.ImportItems:
            if item.IsEnabled:
                item.IsChecked = new_value
        self._notify("CheckImportAll")

    def set_export_schedules(self, rows):
        self.ExportSchedules.Clear()
        for label, unique_id in rows:
            self.ExportSchedules.Add(CheckItem(unique_id, label, False, True))

        self._check_export_schedules_all = False
        self._notify("CheckExportSchedulesAll")

    def set_import_data(self, importable_rows, read_only_rows):
        self.ImportItems.Clear()
        for label, unique_id in importable_rows:
            self.ImportItems.Add(CheckItem(unique_id, label, False, True))

        for index, label in enumerate(read_only_rows):
            self.ImportItems.Add(CheckItem("readonly-{0}".format(index), label, False, False))

        self._check_import_all = False
        self._notify("CheckImportAll")

    def get_selected_schedule_ids(self):
        return [item.UniqueId for item in self.ExportSchedules if item.IsChecked]

    def get_selected_schedules(self):
        return [item for item in self.ExportSchedules if item.IsChecked and item.IsEnabled]

    def get_primary_selected_schedule_label(self):
        for item in self.ExportSchedules:
            if item.IsChecked:
                return item.Label
        return "Schedule"

    def get_selected_import_ids(self):
        return [item.UniqueId for item in self.ImportItems if item.IsChecked and item.IsEnabled]

    def _notify(self, property_name):
        args = PropertyChangedEventArgs(property_name)
        for handler in list(self._property_changed_handlers):
            handler(self, args)

    def add_PropertyChanged(self, handler):
        if handler not in self._property_changed_handlers:
            self._property_changed_handlers.append(handler)

    def remove_PropertyChanged(self, handler):
        if handler in self._property_changed_handlers:
            self._property_changed_handlers.remove(handler)
