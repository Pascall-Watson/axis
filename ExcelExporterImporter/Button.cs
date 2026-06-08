using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using ExcelExporterImporter.Support;
using log4net;

namespace ExcelExporterImporter
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class Button : IExternalApplication
    {
        private const string TabLabel = "BIM One";

        private static readonly ILog Logger =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                SupportLog.EnsureConfigured();
                var assemblieFolder = Path.GetDirectoryName(Assembly.GetAssembly(GetType()).Location);
                var commandPath = Assembly.GetAssembly(GetType()).Location;

                var toolsPanel = GetOrCreateRibbonPanel(application);

                PushButton pushButton = toolsPanel.AddItem(new PushButtonData(
                    AddinInfo.ButtonName,
                    AddinInfo.ButtonText,
                    commandPath,
                    "ExcelExporterImporter.Command")) as PushButton;

                var buttonImage = Path.Combine(assemblieFolder, @"Resources\button.png");
                if (!File.Exists(buttonImage))
                    buttonImage = Path.Combine(Directory.GetParent(assemblieFolder).FullName, @"Resources\button.png");

                pushButton.LargeImage = new BitmapImage(new Uri(buttonImage));
                pushButton.ToolTip = AddinInfo.AddinDescription;

                SupportLog.Info(
                    "addin-startup",
                    new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "assemblyPath", commandPath },
                        { "logFilePath", SupportLog.LogFilePath },
                    });
            }
            catch (Exception e)
            {
                SupportLog.Error(
                    "addin-startup-failed",
                    e,
                    new System.Collections.Generic.Dictionary<string, object>());
                Logger.Error(e.Message, e);
                return Result.Failed;
            }


            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private RibbonPanel GetOrCreateRibbonPanel(UIControlledApplication application)
        {
            var ribbonPanel = application.GetRibbonPanels(Tab.AddIns).Find(x => x.Name == TabLabel);
            if (ribbonPanel == null)
                ribbonPanel = application.CreateRibbonPanel(Tab.AddIns, TabLabel);

            return ribbonPanel;
        }
    }
}