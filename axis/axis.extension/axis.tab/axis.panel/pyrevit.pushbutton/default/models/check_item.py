# -*- coding: utf-8 -*-

import clr

clr.AddReference("WindowsBase")

from System.ComponentModel import INotifyPropertyChanged
from System.ComponentModel import PropertyChangedEventArgs


class CheckItem(INotifyPropertyChanged):
    def __init__(self, unique_id, label, is_checked=False, is_enabled=True):
        self._id = unique_id
        self._label = label
        self._is_checked = bool(is_checked)
        self._is_enabled = bool(is_enabled)
        self._property_changed_handlers = []

    @property
    def UniqueId(self):
        return self._id

    @property
    def Label(self):
        return self._label

    @property
    def IsEnabled(self):
        return self._is_enabled

    @IsEnabled.setter
    def IsEnabled(self, value):
        new_value = bool(value)
        if self._is_enabled == new_value:
            return
        self._is_enabled = new_value
        self._notify("IsEnabled")

    @property
    def IsChecked(self):
        return self._is_checked

    @IsChecked.setter
    def IsChecked(self, value):
        new_value = bool(value)
        if self._is_checked == new_value:
            return
        self._is_checked = new_value
        self._notify("IsChecked")

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
