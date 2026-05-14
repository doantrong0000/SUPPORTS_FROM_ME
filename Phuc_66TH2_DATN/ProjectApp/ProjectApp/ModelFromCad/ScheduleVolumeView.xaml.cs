using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Autodesk.Revit.DB;

namespace ProjectApp.ModelFromCad
{
    /// <summary>
    /// Interaction logic for ScheduleVolumeView.xaml
    /// View để hiển thị bảng thống kê khối lượng
    /// </summary>
    public partial class ScheduleVolumeView : Window
    {
        // Danh sách lưu trữ thông tin thống kê để hiển thị lên DataGrid
        public List<ScheduleInfo> ScheduleInfos { get; set; } = [];

        /// <summary>
        /// Constructor: Khởi tạo view và tính toán khối lượng
        /// </summary>
        /// <param name="doc">Revit Document hiện tại</param>
        public ScheduleVolumeView(Document doc)
        {
            InitializeComponent();

            // --- 1. Tính tổng khối lượng CỘT (Structural Columns) ---
            var columns = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_StructuralColumns)
                .WhereElementIsNotElementType() // Lấy instance, không lấy Type
                .Cast<FamilyInstance>()
                .ToList();

            var sumVolCol = 0.0;

            foreach (var col in columns)
            {
                double v = GetElementVolumeFt3(col); // Hàm lấy volume (ft3)
                if (v > 0) sumVolCol += v;
            }

            ScheduleInfos.Add(new ScheduleInfo()
            {
                Name = "Tổng khối lượng cột",
                Volume = Math.Round(sumVolCol , 2) // Làm tròn 2 chữ số
            });


            // --- 2. Tính tổng khối lượng DẦM (Structural Framing) ---
            var beams = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_StructuralFraming)
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>()
                .ToList();

            var sumVolBeam = 0.0;
            foreach (var beam in beams)
            {
                double v = GetElementVolumeFt3(beam);
                if (v > 0) sumVolBeam += v;
            }

            ScheduleInfos.Add(new ScheduleInfo()
            {
                Name = "Tổng khối lượng dầm",
                Volume = Math.Round(sumVolBeam , 2) 
            });

            // --- 3. Tính tổng khối lượng SÀN (Floors) ---
            var floors = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Floors)
                .WhereElementIsNotElementType()
                .Cast<Floor>()
                .ToList();

            var sumVolFloor = 0.0;
            foreach (var floor in floors)
            {
                double v = GetElementVolumeFt3(floor);
                if (v > 0) sumVolFloor += v;
            }

            ScheduleInfos.Add(new ScheduleInfo()
            {
                Name = "Tổng khối lượng sàn",
                Volume = Math.Round(sumVolFloor , 2) 
            });

            // --- 4. Tổng hợp tất cả ---
            ScheduleInfos.Add(new ScheduleInfo()
            {
                Name = "Tổng khối lượng (cột + dầm + sàn)",
                Volume = Math.Round(sumVolCol , 2)  + Math.Round(sumVolBeam ,2) + Math.Round(sumVolFloor, 2)
            });

            // Gán dữ liệu vào DataGrid
            dgSchedule.ItemsSource = ScheduleInfos;
        }

        /// <summary>
        /// Lấy thể tích của một Element (đã quy đổi ra m3)
        /// </summary>
        /// <param name="e">Element cần tính</param>
        /// <returns>Thể tích (m3)</returns>
        private static double GetElementVolumeFt3(Element e)
        {
            // Thử lấy parameter volume có sẵn (Computed Volume)
            var p = e.get_Parameter(BuiltInParameter.HOST_VOLUME_COMPUTED);

            // Nếu không có, thử tìm parameter tên là "Volume"
            if (p == null)
                p = e.LookupParameter("Volume");

            // Kiểm tra tính hợp lệ
            if (p == null || p.StorageType != StorageType.Double || !p.HasValue)
                return 0.0;

            double v = p.AsDouble(); // Giá trị gốc trong Revit luôn là feet khối (ft3)
            
            if (double.IsNaN(v) || double.IsInfinity(v) || v < 0) return 0.0;

            // Chuyển đổi từ Internal Units (ft3) sang CubicMeters (m3)
            return UnitUtils.ConvertFromInternalUnits(v, UnitTypeId.CubicMeters) ;
        }

        /// <summary>
        /// Sự kiện click nút Close
        /// </summary>
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    /// <summary>
    /// Class chứa thông tin 1 dòng trong bảng thống kê
    /// </summary>
    public class ScheduleInfo
    {
        public string TypeName { get; set; }
        public string Name { get; set; }
        public double Volume { get; set; }
    }
}