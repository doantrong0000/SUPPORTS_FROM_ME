using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Nice3point.Revit.Toolkit.External;
using ShopDrawings_BEAM.Views;
using ShopDrawings_BEAM.ViewModels;

namespace ShopDrawings_BEAM.Commands
{
    public class BeamSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem.Category != null && elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralFraming;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }

    /// <summary>
    ///     External command entry point
    /// </summary>
    [UsedImplicitly]
    [Transaction(TransactionMode.Manual)]
    public class DrawingCommand : ExternalCommand
    {
        public override void Execute()
        {
            // 1. Quét chọn nhiều dầm từ Revit trước khi mở giao diện
            IList<Reference> references = null;
            try
            {
                references = UiDocument.Selection.PickObjects(
                    ObjectType.Element, 
                    new BeamSelectionFilter(), 
                    "Vui lòng quét chọn các dầm liên tục, thẳng hàng..."
                );
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                // Người dùng hủy (ESC)
                return;
            }

            if (references == null || references.Count == 0) return;

            // 2. Chuyển đổi tham chiếu thành Element dầm
            List<Element> beams = references
                .Select(r => Document.GetElement(r))
                .Where(e => e != null)
                .ToList();

            // 3. Kiểm tra xem các dầm được chọn có thẳng hàng (collinear) hay không
            if (!AreBeamsCollinear(beams))
            {
                TaskDialog.Show("Cảnh báo", "Các dầm được chọn không thẳng hàng dọc! Vui lòng quét chọn các cấu kiện dầm nằm thẳng hàng với nhau trên cùng một trục/lưới để triển khai Shop Drawing.");
                return;
            }

            // 4. Khởi tạo và hiển thị Giao diện Wizard 1 lần duy nhất
            var viewModel = new MainViewModel(Document);
            viewModel.SetSelectedBeams(beams);
            viewModel.ProgressValue = 20; // Đã chọn dầm xong -> tiến trình đạt 20%

            var view = new CreateDrawingView
            {
                DataContext = viewModel
            };

            view.ShowDialog();
        }

        /// <summary>
        /// Kiểm tra xem danh sách dầm được chọn có thẳng hàng (collinear) hay không
        /// </summary>
        private bool AreBeamsCollinear(List<Element> beams)
        {
            if (beams.Count <= 1) return true;

            // Lấy Line và hướng của dầm đầu tiên làm chuẩn
            Line firstLine = GetBeamLine(beams[0]);
            if (firstLine == null) return false;

            XYZ p0 = firstLine.GetEndPoint(0);
            XYZ dir0 = firstLine.Direction.Normalize();

            // Dung sai khoảng cách (0.1 feet ~ 3cm) chấp nhận dung sai khi vẽ mô hình hơi lệch trục
            double toleranceFeet = 0.1; 

            for (int i = 1; i < beams.Count; i++)
            {
                Line currentLine = GetBeamLine(beams[i]);
                if (currentLine == null) return false;

                XYZ pStart = currentLine.GetEndPoint(0);
                XYZ pEnd = currentLine.GetEndPoint(1);
                XYZ currentDir = currentLine.Direction.Normalize();

                // 1. Kiểm tra hướng dầm: Phải song song với hướng dầm chuẩn (tích vô hướng tuyệt đối xấp xỉ 1)
                double dot = Math.Abs(dir0.DotProduct(currentDir));
                if (Math.Abs(dot - 1.0) > 0.005) // Sai lệch hướng tối đa cho phép
                {
                    return false;
                }

                // 2. Kiểm tra điểm mút của dầm hiện tại có nằm trên đường thẳng chuẩn của dầm 0 hay không
                // Khoảng cách từ điểm Q đến đường thẳng qua P0 có hướng dir0: |(Q - P0) x dir0|
                double distStart = (pStart - p0).CrossProduct(dir0).GetLength();
                double distEnd = (pEnd - p0).CrossProduct(dir0).GetLength();

                if (distStart > toleranceFeet || distEnd > toleranceFeet)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Lấy Line định vị của Dầm
        /// </summary>
        private Line GetBeamLine(Element beam)
        {
            LocationCurve locCurve = beam.Location as LocationCurve;
            if (locCurve == null) return null;
            return locCurve.Curve as Line;
        }
    }
}