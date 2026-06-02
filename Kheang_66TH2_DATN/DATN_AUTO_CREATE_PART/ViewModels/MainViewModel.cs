using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DATN_AUTO_CREATE_PART.Models;
using DATN_AUTO_CREATE_PART.Utils;
using System.Collections.ObjectModel;
using System.Linq;
using Tekla.Structures.Model.UI;
using TSM = Tekla.Structures.Model;

namespace DATN_AUTO_CREATE_PART.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        // --- Các danh sách (Collections) dùng để liên kết dữ liệu với giao diện (UI) ---
        // Sử dụng ObservableCollection giúp UI tự động cập nhật khi có sự thay đổi (thêm/xóa)
        [ObservableProperty]
        private ObservableCollection<BeamInfoCollection> _beamCollections = new ObservableCollection<BeamInfoCollection>();

        [ObservableProperty]
        private ObservableCollection<ColumnInfoCollection> _columnCollections = new ObservableCollection<ColumnInfoCollection>();

        [ObservableProperty]
        private ObservableCollection<FloorInfoCollection> _floorCollections = new ObservableCollection<FloorInfoCollection>();

        // Lưu trữ thông tin thiết lập cho Lưới trục (Grid)
        [ObservableProperty]
        private GridInfo _gridSettings = new GridInfo();

        // --- Bộ lọc Layer (Layer Filters) ---
        // Cho phép người dùng thiết lập các từ khóa để nhận diện cấu kiện theo layer trong AutoCAD (ngăn cách bởi dấu phẩy)
        [ObservableProperty]
        private string _beamLayerFilter = "beam, dam, frame";

        [ObservableProperty]
        private string _columnLayerFilter = "col, cot";


        [ObservableProperty]
        private string _floorLayerFilter = "slab, san, floor";

        [ObservableProperty]
        private string _gridLayerFilter = "grid, axis, truc";

        // Đối tượng Model chính để tương tác với Tekla Structures API
        private TSM.Model _model;

        // Hàm khởi tạo: Thiết lập kết nối model Tekla và xóa dữ liệu cũ trong các danh sách
        public MainViewModel()
        {
            _model = new TSM.Model();
            BeamCollections.Clear();
            ColumnCollections.Clear();
            FloorCollections.Clear();
        }

        // =====================================================
        // XỬ LÝ ĐIỂM GỐC (ORIGIN PICKING) - Tách biệt Logic
        // =====================================================

        // Biến lưu điểm gốc trong hệ tọa độ của CAD
        private XyzData _cadOrigin;
        // Biến lưu điểm gốc trong hệ tọa độ của Tekla
        private Tekla.Structures.Geometry3d.Point _teklaOrigin;

        /// <summary>
        /// Retrieves the origin point from AutoCAD (once per session).
        /// Yêu cầu lấy điểm gốc từ AutoCAD. Nếu đã có thì dùng lại, nếu chưa thì gọi API CAD để người dùng chọn.
        /// </summary>
        private XyzData RequireCadOrigin()
        {
            if (_cadOrigin != null) return _cadOrigin;

            _cadOrigin = AutoCadInterop.GetCadOrigin();
            if (_cadOrigin == null)
            {
                System.Windows.MessageBox.Show(
                    "CAD origin selection cancelled.",
                    "Cancelled",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }
            return _cadOrigin;
        }

        /// <summary>
        /// Retrieves the origin point from Tekla (once per session).
        /// Yêu cầu lấy điểm gốc từ Tekla. Nếu chưa có, sẽ kích hoạt công cụ Picker để người dùng click chọn trong Tekla.
        /// </summary>
        private Tekla.Structures.Geometry3d.Point RequireTeklaOrigin()
        {
            if (_teklaOrigin != null) return _teklaOrigin;

            try
            {
                var picker = new Picker();
                _teklaOrigin = picker.PickPoint("Pick origin point in Tekla");
                return _teklaOrigin;
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show(
                    "Tekla origin picking cancelled or failed: " + ex.Message,
                    "Cancelled",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return null;
            }
        }

        // Lệnh được gọi từ UI (Command) để ép chọn lại điểm gốc trong CAD
        [RelayCommand]
        private void SetCadOrigin()
        {
            var newOrigin = AutoCadInterop.GetCadOrigin();
            if (newOrigin != null)
            {
                _cadOrigin = newOrigin;
                System.Windows.MessageBox.Show("AutoCAD Origin updated successfully.", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        // Lệnh được gọi từ UI (Command) để ép chọn lại điểm gốc trong Tekla
        [RelayCommand]
        private void SetTeklaOrigin()
        {
            if (!EnsureTeklaConnection()) return;
            try
            {
                var picker = new Picker();
                var newOrigin = picker.PickPoint("Pick NEW origin point in Tekla");
                if (newOrigin != null)
                {
                    _teklaOrigin = newOrigin;
                    System.Windows.MessageBox.Show("Tekla Origin updated successfully.", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
            catch { }
        }

        /// <summary>
        /// Validates Tekla connection status.
        /// Hàm kiểm tra xem phần mềm Tekla có đang mở và có kết nối được Model hay không.
        /// </summary>
        private bool EnsureTeklaConnection()
        {
            if (!_model.GetConnectionStatus())
            {
                System.Windows.MessageBox.Show(
                    "Please open Tekla Structures and a Model before running this command.",
                    "Tekla Not Connected",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        // =====================================================
        // QUÉT DỮ LIỆU TỪ CAD - Mỗi thao tác quét đều yêu cầu phải có điểm gốc CAD
        // =====================================================

        // Lệnh quét Dầm từ CAD
        [RelayCommand]
        private void ScanCadBeams()
        {
            // Bước 1: Yêu cầu điểm gốc CAD
            var cadOrigin = RequireCadOrigin();
            if (cadOrigin == null) return;

            // Bước 2: Gọi hàm trích xuất Dầm từ AutoCAD, dùng bộ lọc Layer cấu hình sẵn
            AutoCadInterop.ExtractBeams(out var extractedBeams, cadOrigin, BeamLayerFilter);

            // Bước 3: Nhóm các dầm có cùng tên (text) lại với nhau
            var grouped = extractedBeams.GroupBy(x => x.Text);
            BeamCollections.Clear();

            // Duyệt qua từng nhóm Dầm và thêm vào bộ sưu tập hiển thị lên giao diện UI
            foreach (var group in grouped)
            {
                var col = new BeamInfoCollection
                {
                    Text = group.Key,
                    Number = group.Count()
                };

                foreach (var b in group)
                {
                    col.BeamInfos.Add(new BeamInfo(b.StartPoint, b.EndPoint, b.Text));
                }

                // Lọc bỏ các dầm bị trùng lặp tọa độ
                col.BeamInfos = col.BeamInfos.Distinct(new BeamInfo.BeamInfoComparerByPoint()).ToList();
                col.Number = col.BeamInfos.Count;

                // Cập nhật thông số Rộng (Width) và Cao (Height) cho nhóm dựa vào phần tử đầu tiên
                if (col.BeamInfos.Any())
                {
                    col.Width = col.BeamInfos.First().Width;
                    col.Height = col.BeamInfos.First().Height;
                }

                BeamCollections.Add(col);
            }
        }

        // Lệnh quét Cột từ CAD
        [RelayCommand]
        private void ScanCadColumns()
        {
            var cadOrigin = RequireCadOrigin();
            if (cadOrigin == null) return;

            // Trích xuất dữ liệu Cột từ AutoCAD
            AutoCadInterop.ExtractColumns(out var extractedColumns, cadOrigin, ColumnLayerFilter);

            var allColumnInfos = extractedColumns.Select(c => new ColumnInfo(c.Points, c.Mask)).ToList();

            // Nhóm Cột theo kích thước (Rộng x Cao)
            var grouped = allColumnInfos.GroupBy(c => new { c.Width, c.Height });

            ColumnCollections.Clear();

            foreach (var group in grouped)
            {
                var col = new ColumnInfoCollection
                {
                    Width = group.Key.Width,
                    Height = group.Key.Height,
                    Text = $"{group.Key.Width}x{group.Key.Height}",
                    Number = group.Count() // Số lượng cột trong nhóm
                };

                foreach (var c in group)
                {
                    col.ColumnInfos.Add(c);
                }

                // Đảm bảo không bị trùng lặp các cột (Dựa vào Distinct của ColumnInfo)
                col.ColumnInfos = col.ColumnInfos.Distinct().ToList();
                col.Number = col.ColumnInfos.Count;

                ColumnCollections.Add(col);
            }
        }

        // Lệnh quét Sàn từ CAD
        [RelayCommand]
        private void ScanCadFloors()
        {
            var cadOrigin = RequireCadOrigin();
            if (cadOrigin == null) return;

            // Trích xuất dữ liệu Sàn (thường là các đường Polyline kín) từ AutoCAD
            AutoCadInterop.ExtractFloors(out var extractedFloors, cadOrigin, FloorLayerFilter);

            FloorCollections.Clear();

            // Đưa dữ liệu sàn vừa quét được vào danh sách hiển thị
            foreach (var floor in extractedFloors)
            {
                var col = new FloorInfoCollection
                {
                    Area = floor.Area,
                    Number = 1
                };
                col.FloorPoints.Add(floor.Points);
                FloorCollections.Add(col);
            }
        }

        // Lệnh quét Lưới trục từ CAD
        [RelayCommand]
        private void ScanCadGrids()
        {
            var cadOrigin = RequireCadOrigin();
            if (cadOrigin == null) return;

            // Trích xuất khoảng cách và nhãn của lưới trục từ AutoCAD
            AutoCadInterop.ExtractGrids(out string cX, out string cY, out string lX, out string lY, cadOrigin, GridLayerFilter);
            if (cX != "0" || cY != "0")
            {
                GridSettings.CoordinateX = cX;
                GridSettings.CoordinateY = cY;
                GridSettings.LabelX = lX;
                GridSettings.LabelY = lY;
            }
        }

        // =====================================================
        // TẠO MÔ HÌNH BÊN TEKLA - Yêu cầu điểm gốc của cả CAD và Tekla
        // =====================================================

        // Lệnh tạo Lưới trục trong Tekla
        [RelayCommand]
        private void GenerateGridToTekla()
        {
            // Kiểm tra xem Tekla đã kết nối chưa
            if (!EnsureTeklaConnection()) return;

            // Lấy điểm gốc chèn vào Tekla
            var teklaOrigin = RequireTeklaOrigin();
            if (teklaOrigin == null) return;

            // Gọi API TeklaInterop để tạo lưới trục với các cài đặt (GridSettings)
            TeklaInterop.GenerateStandardGrid(teklaOrigin, GridSettings);
            System.Windows.MessageBox.Show(
                "Grid generated successfully!",
                "Success",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }

        // Lệnh tạo các cấu kiện (Dầm, Cột, Sàn) trong Tekla
        [RelayCommand]
        private void GenerateComponentsToTekla()
        {
            if (!EnsureTeklaConnection()) return;

            // Bắt buộc phải có tọa độ gốc của CAD để tính toán dịch chuyển tương đối
            if (_cadOrigin == null)
            {
                System.Windows.MessageBox.Show(
                    "Please scan at least one CAD part first to define the CAD origin.",
                    "CAD Origin Missing",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Lấy điểm gốc Tekla để chèn mô hình
            var teklaOrigin = RequireTeklaOrigin();
            if (teklaOrigin == null) return;

            // Lần lượt gọi hàm tạo Dầm, Cột, Sàn trong Tekla dựa trên dữ liệu đã quét và khoảng cách giữa 2 điểm gốc
            TeklaInterop.GenerateBeams(BeamCollections, _cadOrigin, teklaOrigin);
            TeklaInterop.GenerateColumns(ColumnCollections, _cadOrigin, teklaOrigin);
            TeklaInterop.GenerateFloors(FloorCollections, _cadOrigin, teklaOrigin);

            System.Windows.MessageBox.Show(
                "Components generated successfully in Tekla!",
                "Success",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
    }
}
