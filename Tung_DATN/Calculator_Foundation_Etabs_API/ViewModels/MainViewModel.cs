using Calculator_Foundation_Etabs_API.Models;
using ETABSv1;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Calculator_Foundation_Etabs_API.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private string _selectedCombo;
        public string SelectedCombo
        {
            get => _selectedCombo;
            set { if (SetProperty(ref _selectedCombo, value)) SyncEtabs(null); }
        }

        public ObservableCollection<FoundationModel> Foundations { get; } = new ObservableCollection<FoundationModel>();
        public ObservableCollection<string> LoadCombos { get; } = new ObservableCollection<string>();

        public IEnumerable<ConcreteGrade> ConcreteGrades => Enum.GetValues(typeof(ConcreteGrade)).Cast<ConcreteGrade>();
        public IEnumerable<RebarGrade> RebarGrades => Enum.GetValues(typeof(RebarGrade)).Cast<RebarGrade>();

        private ConcreteGrade _selectedConcrete = ConcreteGrade.B20;
        public ConcreteGrade SelectedConcrete { get => _selectedConcrete; set { SetProperty(ref _selectedConcrete, value); } }

        private RebarGrade _selectedRebar = RebarGrade.CB400V;
        public RebarGrade SelectedRebar { get => _selectedRebar; set { SetProperty(ref _selectedRebar, value); } }

        public ICommand SyncEtabsCommand { get; }
        public ICommand RefreshCombosCommand { get; }
        public ICommand CalculateCommand { get; }

        public MainViewModel()
        {
            SyncEtabsCommand = new RelayCommand(SyncEtabs);
            RefreshCombosCommand = new RelayCommand(RefreshCombos);
            CalculateCommand = new RelayCommand((obj) => CalculateAll());
        }

        private void RefreshCombos(object obj)
        {
            try
            {
                var combos = EtabsHelper.GetLoadCombinations();
                LoadCombos.Clear();
                foreach (var c in combos) LoadCombos.Add(c);
                if (LoadCombos.Any()) SelectedCombo = LoadCombos.First();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối ETABS: " + ex.Message);
            }
        }

        private void SyncEtabs(object obj)
        {
            if (string.IsNullOrEmpty(SelectedCombo)) return;

            var foundationList = EtabsHelper.GetBaseLevelFoundations(SelectedCombo);

            if (foundationList != null && foundationList.Any())
            {
                Foundations.Clear();
                foreach (var f in foundationList)
                {
                    f.SelectedSteel = SelectedRebar.ToString();
                    Foundations.Add(f);
                }

                if (obj != null)
                    MessageBox.Show($"Đã đồng bộ {foundationList.Count} dải móng từ ETABS thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CalculateAll()
        {
            if (!Foundations.Any()) return;
            double rs = SelectedRebar == RebarGrade.CB400V ? 350 : 260;

            foreach (var f in Foundations)
            {
                // Gọi hàm tính thép tại đây, truyền f (Model chứa các giá trị MTotal đã được tự động cộng)
                // FoundationCalculator.RunCalculation(f, rs);
            }
        }
    }

    public class EtabsHelper
    {
        // ==========================================
        // 1. CÁC CLASS & STRUCT CHỨA DỮ LIỆU
        // ==========================================
        private class FrameData
        {
            public string Name { get; set; }
            public double X1 { get; set; }
            public double Y1 { get; set; }
            public double Z1 { get; set; }
            public double X2 { get; set; }
            public double Y2 { get; set; }
            public double Z2 { get; set; }
            public bool IsHorizontal => Math.Abs(X1 - X2) > Math.Abs(Y1 - Y2);
            public double MidX => (X1 + X2) / 2.0;
            public double MidY => (Y1 + Y2) / 2.0;
            public double Length => Math.Max(0.01, Math.Sqrt(Math.Pow(X2 - X1, 2) + Math.Pow(Y2 - Y1, 2)));
        }

        private class SlabData
        {
            public string Name { get; set; }
            public double Thickness { get; set; }
            public double MinX { get; set; }
            public double MaxX { get; set; }
            public double MinY { get; set; }
            public double MaxY { get; set; }
            public List<Point2D> Points { get; set; } = new List<Point2D>();
        }

        private struct Point2D { public double X; public double Y; }

        private class StripForcePoint
        {
            public string StripName { get; set; }
            public double Station { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double M3 { get; set; }
        }

        private struct InternalForces
        {
            public double Start;
            public double Mid;
            public double End;
            public double MaxShear;
        }

        // ==========================================
        // 2. CÁC HÀM THUẬT TOÁN HÌNH HỌC (GEOMETRY)
        // ==========================================
        private static bool IsPointInPolygon(double x, double y, List<Point2D> polygon)
        {
            bool isInside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                if (((polygon[i].Y > y) != (polygon[j].Y > y)) &&
                    (x < (polygon[j].X - polygon[i].X) * (y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) + polygon[i].X))
                {
                    isInside = !isInside;
                }
            }
            return isInside;
        }

        private static bool IsPointOnSegment(double px, double py, double x1, double y1, double x2, double y2, double tolerance = 0.2)
        {
            double apx = px - x1, apy = py - y1, abx = x2 - x1, aby = y2 - y1;
            double ab2 = abx * abx + aby * aby;
            if (ab2 == 0) return false;

            double t = (apx * abx + apy * aby) / ab2;
            if (t < -0.1 || t > 1.1) return false;

            t = Math.Max(0, Math.Min(1, t));
            double cx = x1 + t * abx, cy = y1 + t * aby;
            double dx = px - cx, dy = py - cy;
            return (dx * dx + dy * dy) <= (tolerance * tolerance);
        }

        // ==========================================
        // 3. CÁC HÀM TƯƠNG TÁC ETABS (API EXTRACTION)
        // ==========================================

        public static List<string> GetLoadCombinations()
        {
            var list = new List<string>();
            try
            {
                cOAPI myETABSObject = (cOAPI)System.Runtime.InteropServices.Marshal.GetActiveObject("CSI.ETABS.API.ETABSObject");
                cSapModel mySapModel = myETABSObject.SapModel;
                int numberNames = 0; string[] myName = null;
                if (mySapModel.RespCombo.GetNameList(ref numberNames, ref myName) == 0 && numberNames > 0)
                    list.AddRange(myName);
            }
            catch (Exception) { }
            return list;
        }

        private static List<FrameData> GetBaseFrames(cSapModel mySapModel, out double baseZ)
        {
            var allFrames = new List<FrameData>();
            baseZ = double.MaxValue;
            int numFrames = 0; string[] frameNames = null;

            if (mySapModel.FrameObj.GetNameList(ref numFrames, ref frameNames) != 0 || numFrames == 0) return allFrames;

            for (int i = 0; i < numFrames; i++)
            {
                string fName = frameNames[i], pt1 = "", pt2 = "";
                mySapModel.FrameObj.GetPoints(fName, ref pt1, ref pt2);
                double x1 = 0, y1 = 0, z1 = 0, x2 = 0, y2 = 0, z2 = 0;
                mySapModel.PointObj.GetCoordCartesian(pt1, ref x1, ref y1, ref z1);
                mySapModel.PointObj.GetCoordCartesian(pt2, ref x2, ref y2, ref z2);

                allFrames.Add(new FrameData { Name = fName, X1 = x1, Y1 = y1, Z1 = z1, X2 = x2, Y2 = y2, Z2 = z2 });
                if (z1 < baseZ) baseZ = z1;
                if (z2 < baseZ) baseZ = z2;
            }

            // TẠO BIẾN TRUNG GIAN TẠI ĐÂY
            double currentBaseZ = baseZ;

            // Dùng biến trung gian trong Lambda expression
            allFrames = allFrames.Where(f => Math.Abs(f.Z1 - currentBaseZ) < 0.001 && Math.Abs(f.Z2 - currentBaseZ) < 0.001).ToList();

            return allFrames;
        }

        private static List<SlabData> GetBaseSlabs(cSapModel mySapModel, double baseZ)
        {
            var baseSlabs = new List<SlabData>();
            int numAreas = 0; string[] areaNames = null;
            if (mySapModel.AreaObj.GetNameList(ref numAreas, ref areaNames) != 0 || numAreas == 0) return baseSlabs;

            for (int i = 0; i < numAreas; i++)
            {
                string aName = areaNames[i];
                int numPts = 0; string[] ptNames = null;
                if (mySapModel.AreaObj.GetPoints(aName, ref numPts, ref ptNames) == 0 && numPts > 2)
                {
                    double px = 0, py = 0, pz = 0;
                    mySapModel.PointObj.GetCoordCartesian(ptNames[0], ref px, ref py, ref pz);
                    if (Math.Abs(pz - baseZ) < 0.1) // Nếu Sàn nằm ở Base
                    {
                        var slab = new SlabData { Name = aName, MinX = double.MaxValue, MinY = double.MaxValue, MaxX = double.MinValue, MaxY = double.MinValue };
                        for (int j = 0; j < numPts; j++)
                        {
                            double x = 0, y = 0, z = 0;
                            mySapModel.PointObj.GetCoordCartesian(ptNames[j], ref x, ref y, ref z);
                            slab.Points.Add(new Point2D { X = x, Y = y });
                            if (x < slab.MinX) slab.MinX = x; if (x > slab.MaxX) slab.MaxX = x;
                            if (y < slab.MinY) slab.MinY = y; if (y > slab.MaxY) slab.MaxY = y;
                        }

                        string propAreaName = "";
                        mySapModel.AreaObj.GetProperty(aName, ref propAreaName);
                        eSlabType slabType = eSlabType.Drop; eShellType shellType = eShellType.ShellThin;
                        string matProp = "", notes = "", guid = ""; int color = 0; double thickness = 0;
                        mySapModel.PropArea.GetSlab(propAreaName, ref slabType, ref shellType, ref matProp, ref thickness, ref color, ref notes, ref guid);

                        slab.Thickness = thickness;
                        baseSlabs.Add(slab);
                    }
                }
            }
            return baseSlabs;
        }

        private static List<StripForcePoint> GetAllStripForces(cSapModel mySapModel, string loadCombo)
        {
            var stripForces = new List<StripForcePoint>();
            mySapModel.Results.Setup.DeselectAllCasesAndCombosForOutput();
            mySapModel.Results.Setup.SetComboSelectedForOutput(loadCombo, true);

            string[] emptyCases = new string[0];
            mySapModel.DatabaseTables.SetLoadCasesSelectedForDisplay(ref emptyCases);
            string[] combos = new string[] { loadCombo };
            mySapModel.DatabaseTables.SetLoadCombinationsSelectedForDisplay(ref combos);

            int tableVersion = 0; string[] fieldKeyList = null; string[] fieldsKeysIncluded = null;
            int numberRecords = 0; string[] tableData = null;

            if (mySapModel.DatabaseTables.GetTableForDisplayArray("Strip Forces", ref fieldKeyList, "", ref tableVersion, ref fieldsKeysIncluded, ref numberRecords, ref tableData) == 0 && numberRecords > 0)
            {
                int numCols = fieldsKeysIncluded.Length;
                int idxStrip = Array.FindIndex(fieldsKeysIncluded, col => col.Trim().Equals("Strip", StringComparison.OrdinalIgnoreCase) || col.Trim().Equals("Strip Name", StringComparison.OrdinalIgnoreCase));
                int idxStation = Array.FindIndex(fieldsKeysIncluded, col => col.Trim().Equals("Station", StringComparison.OrdinalIgnoreCase));
                int idxM3 = Array.FindIndex(fieldsKeysIncluded, col => col.Trim().Equals("M3", StringComparison.OrdinalIgnoreCase) || col.Trim().Equals("Moment", StringComparison.OrdinalIgnoreCase));
                int idxX = Array.FindIndex(fieldsKeysIncluded, col => col.Trim().Equals("X", StringComparison.OrdinalIgnoreCase) || col.Trim().StartsWith("X ", StringComparison.OrdinalIgnoreCase));
                int idxY = Array.FindIndex(fieldsKeysIncluded, col => col.Trim().Equals("Y", StringComparison.OrdinalIgnoreCase) || col.Trim().StartsWith("Y ", StringComparison.OrdinalIgnoreCase));

                if (idxStrip != -1 && idxStation != -1 && idxM3 != -1 && idxX != -1 && idxY != -1)
                {
                    for (int i = 0; i < numberRecords; i++)
                    {
                        double.TryParse(tableData[i * numCols + idxStation], NumberStyles.Any, CultureInfo.InvariantCulture, out double st);
                        double.TryParse(tableData[i * numCols + idxX], NumberStyles.Any, CultureInfo.InvariantCulture, out double x);
                        double.TryParse(tableData[i * numCols + idxY], NumberStyles.Any, CultureInfo.InvariantCulture, out double y);
                        double.TryParse(tableData[i * numCols + idxM3], NumberStyles.Any, CultureInfo.InvariantCulture, out double m3);

                        stripForces.Add(new StripForcePoint
                        {
                            StripName = tableData[i * numCols + idxStrip].Trim(),
                            Station = st,
                            X = x,
                            Y = y,
                            M3 = m3
                        });
                    }
                }
            }
            return stripForces;
        }

        private static void GetFrameDimensions(cSapModel mySapModel, FrameData frame, List<SlabData> slabs, out double B, out double H)
        {
            B = 0; H = 0;
            // Kích thước từ tiết diện Dầm (Dự phòng)
            string propName = "", sAuto = "";
            mySapModel.FrameObj.GetSection(frame.Name, ref propName, ref sAuto);
            string fileName = "", matProp = "", notes = "", guid = ""; int color = 0; double t3 = 0, t2 = 0;
            if (mySapModel.PropFrame.GetRectangle(propName, ref fileName, ref matProp, ref t3, ref t2, ref color, ref notes, ref guid) == 0)
            { B = t2; H = t3; }

            // Ghi đè bằng kích thước Sàn (Nếu dầm nằm trong sàn)
            SlabData containingSlab = slabs.FirstOrDefault(s => IsPointInPolygon(frame.MidX, frame.MidY, s.Points));
            if (containingSlab != null)
            {
                H = containingSlab.Thickness;
                B = frame.IsHorizontal ? (containingSlab.MaxY - containingSlab.MinY) : (containingSlab.MaxX - containingSlab.MinX);
            }
        }

        private static InternalForces ExtractFrameForces(cSapModel mySapModel, string frameName)
        {
            var res = new InternalForces();
            int nResStrip = 0; string[] oResStrip = null, eResStrip = null, lcResStrip = null, stResStrip = null;
            double[] osResStrip = null, esResStrip = null, snResStrip = null, pResStrip = null, v2ResStrip = null, v3ResStrip = null, tResStrip = null, m2ResStrip = null, m3ResStrip = null;

            if (mySapModel.Results.FrameForce(frameName, eItemTypeElm.ObjectElm, ref nResStrip, ref oResStrip, ref osResStrip, ref eResStrip, ref esResStrip, ref lcResStrip, ref stResStrip, ref snResStrip, ref pResStrip, ref v2ResStrip, ref v3ResStrip, ref tResStrip, ref m2ResStrip, ref m3ResStrip) == 0 && nResStrip > 0)
            {
                res.Start = m3ResStrip[0] > 0 ? m3ResStrip[0] : 0;
                res.End = m3ResStrip[nResStrip - 1] > 0 ? m3ResStrip[nResStrip - 1] : 0;

                for (int k = 0; k < nResStrip; k++)
                {
                    if (Math.Abs(v2ResStrip[k]) > res.MaxShear) res.MaxShear = Math.Abs(v2ResStrip[k]);
                    if (k > 0 && k < nResStrip - 1 && m3ResStrip[k] < -res.Mid) res.Mid = Math.Abs(m3ResStrip[k]);
                }
            }
            return res;
        }

        private static InternalForces ExtractStripForces(List<StripForcePoint> allStripForces, FrameData frame)
        {
            var res = new InternalForces();
            var overlappingPoints = allStripForces.Where(s => IsPointOnSegment(s.X, s.Y, frame.X1, frame.Y1, frame.X2, frame.Y2)).ToList();

            if (overlappingPoints.Any())
            {
                var targetStripGroup = overlappingPoints.GroupBy(s => s.StripName).OrderByDescending(g => g.Count()).First();
                var stripData = targetStripGroup.OrderBy(s => s.Station).ToList();

                res.Start = Math.Abs(stripData.First().M3);
                res.End = Math.Abs(stripData.Last().M3);

                if (stripData.Count > 2)
                {
                    for (int k = 1; k < stripData.Count - 1; k++)
                    {
                        if (Math.Abs(stripData[k].M3) > res.Mid) res.Mid = Math.Abs(stripData[k].M3);
                    }
                }
            }
            return res;
        }

        // ==========================================
        // 4. HÀM CHÍNH (ĐÃ ĐƯỢC LÀM SẠCH VÀ LẮP RÁP)
        // ==========================================
        public static List<FoundationModel> GetBaseLevelFoundations(string loadCombo)
        {
            var resultList = new List<FoundationModel>();
            try
            {
                cOAPI myETABSObject = (cOAPI)System.Runtime.InteropServices.Marshal.GetActiveObject("CSI.ETABS.API.ETABSObject");
                cSapModel mySapModel = myETABSObject.SapModel;
                mySapModel.SetPresentUnits(eUnits.kN_m_C);

                // 1. Tải danh sách Dầm
                var baseFrames = GetBaseFrames(mySapModel, out double baseZ);
                if (!baseFrames.Any()) return resultList;

                // 2. Tải danh sách Sàn & Strip Forces
                var baseSlabs = GetBaseSlabs(mySapModel, baseZ);
                var allStripForces = GetAllStripForces(mySapModel, loadCombo);

                // 3. Xử lý từng Dầm
                foreach (var frame in baseFrames)
                {
                    // Lấy hình học
                    GetFrameDimensions(mySapModel, frame, baseSlabs, out double B, out double H);
                    var foundation = new FoundationModel { Name = $"Móng {frame.Name}", Length = frame.Length, B = B, Bd = B, H = H, Hd = H };

                    // Lấy Nội lực Dầm
                    var frameForces = ExtractFrameForces(mySapModel, frame.Name);
                    foundation.MBot_Start = frameForces.Start;
                    foundation.MTop_Mid = frameForces.Mid;
                    foundation.MBot_End = frameForces.End;
                    foundation.Q_Max = frameForces.MaxShear;

                    // Lấy Nội lực Strip
                    var stripForces = ExtractStripForces(allStripForces, frame);
                    foundation.MStrip_Start = stripForces.Start;
                    foundation.MStrip_Mid = stripForces.Mid;
                    foundation.MStrip_End = stripForces.End;

                    resultList.Add(foundation);
                }
            }
            catch (Exception) { }
            return resultList;
        }
    }
}