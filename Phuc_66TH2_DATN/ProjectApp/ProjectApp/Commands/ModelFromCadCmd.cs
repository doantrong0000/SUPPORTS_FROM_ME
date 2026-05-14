using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ProjectApp.ModelFromCad;
using ProjectApp.Utils;

namespace ProjectApp.Commands
{
    /// <summary>
    /// Command để mở tool Model From Cad (Tạo mô hình từ CAD)
    /// </summary>
    [Transaction(TransactionMode.Manual)]
   public class ModelFromCadCmd : IExternalCommand
    {
        /// <summary>
        /// Phương thức Execute chạy khi người dùng bấm nút trên Ribbon
        /// </summary>
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Khởi tạo biến môi trường AC (Active Context)
            AC.GetInformation(commandData, "DATN");

            // Khởi tạo và hiển thị giao diện ModelFromCadView
            var view = new ModelFromCadView(commandData.Application.ActiveUIDocument.Document);
            view.ShowDialog();

            return Result.Succeeded;
        }
    }
}
