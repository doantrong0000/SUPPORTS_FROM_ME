using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ProjectApp.ModelFromCad;
using ProjectApp.Utils;

namespace ProjectApp.Commands
{
    /// <summary>
    /// Command để mở tool Schedule Volume (Thống kê khối lượng)
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class ScheduleVolumeCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Khởi tạo biến môi trường AC
            AC.GetInformation(commandData, "Schedule Volume");

            // Khởi tạo và hiển thị giao diện ScheduleVolumeView
            var view = new ScheduleVolumeView(AC.Document);
            view.ShowDialog();

            return Result.Succeeded;
        }
    }
}
