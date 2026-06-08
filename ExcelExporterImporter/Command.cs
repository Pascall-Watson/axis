using System;
using System.Globalization;
using System.Reflection;
using System.Threading;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ExcelExporterImporter.Interop;
using ExcelExporterImporter.Support;
using log4net;

namespace ExcelExporterImporter
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        private static readonly ILog Logger =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public Result Execute(ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            SupportLog.EnsureConfigured();
            var originalCulture = Thread.CurrentThread.CurrentCulture;
            var originalUICulture = Thread.CurrentThread.CurrentUICulture;

            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

                var uiapp = commandData.Application;
                var uidoc = uiapp.ActiveUIDocument;
                var doc = uidoc.Document;

                if (doc == null)
                    return Result.Cancelled;

                SupportLog.Info(
                    "legacy-command-execute",
                    new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "document", doc.Title },
                        { "logFilePath", SupportLog.LogFilePath },
                    });

                ExcelExporterImporterInterop.ShowMainWindow(doc, uiapp.MainWindowHandle);

                return Result.Succeeded;
            }
            catch (Exception e)
            {
                SupportLog.Error(
                    "legacy-command-failed",
                    e,
                    new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "message", e.Message },
                    });
                Logger.Error(e.Message, e);
                return Result.Failed;
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = originalCulture;
                Thread.CurrentThread.CurrentUICulture = originalUICulture;
            }
        }
    }
}