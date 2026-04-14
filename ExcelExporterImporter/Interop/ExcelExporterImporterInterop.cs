using System;
using System.Globalization;
using System.Threading;
using System.Windows.Interop;
using Autodesk.Revit.DB;
using ExcelExporterImporter.Views;

namespace ExcelExporterImporter.Interop
{
    public static class ExcelExporterImporterInterop
    {
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
    }
}