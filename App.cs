using Autodesk.Revit.UI;
using System;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace MeasuringTools
{
    public class App : IExternalApplication
    {
        static void AddRibbonPanel(UIControlledApplication application)
        {
            RibbonPanel ribbonPanel = application.CreateRibbonPanel("Measure");

            string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;

            PushButtonData b1Data = new PushButtonData("Point to Point", "Point to Point", thisAssemblyPath, "MeasuringTools.MeasurePointToPoint");
            PushButton pb1 = ribbonPanel.AddItem(b1Data) as PushButton;
            pb1.ToolTip = "Measure distance between two points.";
            BitmapImage pb1Image = new BitmapImage(new Uri("pack://application:,,,/MeasuringTools;component/Resources/MeasurePt.png"));
            pb1.LargeImage = pb1Image;

            PushButtonData b2Data = new PushButtonData("Multiple Points", "Multiple Points", thisAssemblyPath, "MeasuringTools.MeasureMultiplePoints");
            PushButton pb2 = ribbonPanel.AddItem(b2Data) as PushButton;
            pb2.ToolTip = "Measure cumulative distance between points.";
            BitmapImage pb2Image = new BitmapImage(new Uri("pack://application:,,,/MeasuringTools;component/Resources/MeasurePts.png"));
            pb2.LargeImage = pb2Image;
        }
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
        public Result OnStartup(UIControlledApplication application)
        {
            AddRibbonPanel(application);
            return Result.Succeeded;
        }
    }
}