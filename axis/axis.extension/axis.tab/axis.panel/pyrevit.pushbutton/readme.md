# pyrevit workflow

WPF/MVVM pyRevit UI for export and import workflows backed by the C# interop service.

## What this button does

- Export selected schedules to Excel.
- Import workbook items into the active Revit model.

## Architecture

This button follows Tier 3 structure:

- `script.py` is a thin dispatcher.
- `default/entry.py` wires UI, ViewModel, and services.
- `default/viewmodels/` stores MVVM state and commands.
- `default/views/` stores XAML only.
- `default/services/` stores backend workflow operations.
- `default/models/` stores UI data models.
