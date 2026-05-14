using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using ProjectApp.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
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
using Line = Autodesk.Revit.DB.Line;

namespace ProjectApp.ModelFromCad
{
    /// <summary>
    /// Interaction logic for ModelFromCadView.xaml
    /// </summary>
    public partial class ModelFromCadView : Window
    {
        #region Fields & Properties (Các biến và thuộc tính)

        private readonly Document document;
        
        // Tọa độ gốc từ CAD
        public XyzData CadBeamOrigin;
        
        // Điểm gốc để đặt mô hình trong Revit
        private XYZ _origin;

        // Các danh sách lưu trữ dữ liệu tạm thời
        private ObservableCollection<BeamInfoCollection> _beamInfoCollections = new();
        private readonly List<ColumnInfoCollection> columnInfoCollections = new();
        private readonly List<CadBeams> _cadBeams = new();
        public List<CadRectangle> cadRectangles = new();
        public List<List<XyzData>> ListPoint = new(); // Danh sách các điểm tạo sàn
        public List<FloorInfoCollection> floorInfoCollections = new();

        // Properties binding ra View (DataGrid)
        public ObservableCollection<FloorInfoCollection> FloorInfoCollections { get; set; } = new();
        
        public ObservableCollection<BeamInfoCollection> BeamInfoCollections
        {
            get => _beamInfoCollections;
            set { _beamInfoCollections = value; }
        }

        public ObservableCollection<ColumnInfoCollection> ColumnInfoCollections { get; set; } = new();
        
        // Danh sách các Family Type hiện có để hiển thị trên DataGrid
        public ObservableCollection<FamilySymbol> CurrentFamilyTypes { get; } = new();

        #endregion

        #region Constructor & Initialization (Khởi tạo)

        public ModelFromCadView(Document document)
        {
            InitializeComponent();
            this.document = document;
            LoadColumnFamilySymbols(); // Load dữ liệu ban đầu cho combobox
        }

        /// <summary>
        /// Load các Family Symbol cột, dầm, sàn, level vào ComboBox khi mở window
        /// </summary>
        private void LoadColumnFamilySymbols()
        {
            // 1. Lấy các Column Types trong dự án (trừ vật liệu thép)
            var columnFamilySymbols = new FilteredElementCollector(AC.Document)
                .OfCategory(BuiltInCategory.OST_StructuralColumns)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .Select(x => x.Family)
                .Where(x => x.StructuralMaterialType != StructuralMaterialType.Steel)
                .GroupBy(x => x.Id).Select(x => x.First()) // Fix cho .NET 4.8 thay vì DistinctBy
                .OrderBy(x => x.Name)
                .ToList();

            cboColumnFamilyType.ItemsSource = columnFamilySymbols;

            // Chọn item đầu tiên nếu có
            if (columnFamilySymbols.Any())
            {
                cboColumnFamilyType.SelectedIndex = 0;
            }

            // 2. Lấy tất cả các Level
            var levels = new FilteredElementCollector(document)
                .OfCategory(BuiltInCategory.OST_Levels)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(x => x.Elevation)
                .ToList();

            // Gán source cho các combobox Level của Cột
            cbBaseLevel.ItemsSource = levels;
            cbBaseLevel.SelectedItem = levels.FirstOrDefault();
            cbTopLevel.ItemsSource = levels;
            cbTopLevel.SelectedItem = levels.Skip(1).FirstOrDefault();

            // Load dữ liệu mặc định cho Dầm và Sàn
            GetDataDefaultBeam();
            GetDataDefaultFloor();
        }

        #endregion

        #region UI Event Handlers (Sự kiện nút bấm)

        /// <summary>
        /// Nút Close: Đóng cửa sổ
        /// </summary>
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Nút Create: Thực hiện tạo mô hình Revit dựa trên tab đang chọn
        /// </summary>
        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            switch (TabControl.SelectedIndex)
            {
                case 1: // Tab Beam
                    ModelBeams();
                    break;
                case 0: // Tab Column
                    ModelColumn();
                    break;
                default: // Tab Floor (hoặc khác)
                    ModelFloor();
                    break;
            }
        }

        /// <summary>
        /// Nút GetData: Lấy dữ liệu từ file AutoCAD đang mở
        /// </summary>
        private void BtnGetData_Click(object sender, RoutedEventArgs e)
        {
            switch (TabControl.SelectedIndex)
            {
                case 1: // Tab Beam
                    SelectBeam();
                    break;
                case 0: // Tab Column
                    SelectFormCadColumn();
                    break;
                default: // Tab Floor
                    SelectFloorFromCad();
                    break;
            }
        }

        // Sự kiện khi chọn Family Type cho Dầm -> Update ComboBox kích thước và danh sách Type trong Grid
        private void CbFamilyBeamTypes_OnSelected(object sender, RoutedEventArgs e)
        {
            var familySelected = CbFamilyBeamTypes.SelectedItem as Family;
            if (familySelected == null) return;

            var first = document.GetElement(familySelected.GetFamilySymbolIds().FirstOrDefault()) as FamilySymbol;
            if (first != null)
            {
                var data = first.GetOrderedParameters()
                    .Where(x => x.StorageType == StorageType.Double).Select(x => x.Definition.Name)
                    .ToList();
                cbBeamWidth.ItemsSource = data;
                cbBeamWidth.SelectedItem = data.FirstOrDefault();

                cbBeamHeight.ItemsSource = data;
                cbBeamHeight.SelectedItem = data.Skip(1).FirstOrDefault();
            }

            // Update list types for DataGrid
            CurrentFamilyTypes.Clear();
            var symbols = familySelected.GetFamilySymbolIds()
                .Select(x => document.GetElement(x))
                .Cast<FamilySymbol>()
                .OrderBy(x => x.Name)
                .ToList();
            foreach (var s in symbols) CurrentFamilyTypes.Add(s);
        }

        // Sự kiện khi chọn Type cụ thể ở ComboBox dưới cùng -> Update Width/Height mặc định
        private void CbBeamType_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedType = cbBeamType.SelectedItem as FamilySymbol;
            if (selectedType != null)
            {
                var wParamName = cbBeamWidth.SelectedItem as string;
                var hParamName = cbBeamHeight.SelectedItem as string;

                if (!string.IsNullOrEmpty(wParamName) && !string.IsNullOrEmpty(hParamName))
                {
                    // Lấy dimensions từ Type được chọn
                    var wParam = selectedType.LookupParameter(wParamName);
                    var hParam = selectedType.LookupParameter(hParamName);
                    
                    // Update vào combobox kích thước để người dùng thấy (optional)
                    // ... (Logic này có thể thêm nếu cần, nhưng hiện tại chỉ cần lấy giá trị khi Model)
                }
            }
        }

        // Sự kiện khi chọn Family Type cho Cột -> Update ComboBox kích thước
        private void CboColumnFamilyType_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var familySelected = cboColumnFamilyType.SelectedItem as Family;
            if (familySelected == null) return;

            var first = document.GetElement(familySelected.GetFamilySymbolIds().FirstOrDefault()) as FamilySymbol;
            if (first != null)
            {
                var data = first.GetOrderedParameters()
                    .Where(x => x.StorageType == StorageType.Double).Select(x => x.Definition.Name)
                    .ToList();
                cbWidthColumn.ItemsSource = data;
                cbWidthColumn.SelectedItem = data.FirstOrDefault();

                cbHeightCol.ItemsSource = data;
                cbHeightCol.SelectedItem = data.Skip(1).FirstOrDefault();
            }
        }

        #endregion

        #region Beams (Xử lý Dầm)

        // Lấy dữ liệu dầm từ AutoCAD
        public void SelectBeam()
        {
            Hide(); // Ẩn form Revit để thao tác CAD

            // Kết nối với AutoCAD đang chạy
            dynamic a = null;
            try 
            {
                // Thử lấy Active Object một cách an toàn hơn
                a = Marshal.GetActiveObject("AutoCAD.Application");
            }
            catch 
            {
                try 
                {
                    // Fallback cho cú pháp cũ nếu có vấn đề
                    a = ComRunningObject.GetActiveObjectByProgId("AutoCAD.Application");
                }
                catch
                {
                    MessageBox.Show("Cannot find running AutoCAD. Please open AutoCAD first.");
                    ShowDialog();
                    return;
                }
            }

            if (a != null)
            {
                a.Visible = true;
                a.WindowState = 3; // Maximized
                try { a.ActiveDocument.Activate(); } catch { }

                dynamic doc = a.ActiveDocument;
                string[] arrPoint = null;

                try
                {
                    // Yêu cầu người dùng chọn điểm gốc trên CAD
                    var pointCad = doc.Utility.GetPoint(Type.Missing, "Select point: ");
                    arrPoint = ((IEnumerable)pointCad).Cast<object>().Select(x => x.ToString()).ToArray();
                }
                catch (Exception) { /* Bỏ qua nếu lỗi click */ }

                if (arrPoint != null)
                {
                    double[] arrPoint1 = new double[3];
                    int i = 0;
                    foreach (var item in arrPoint)
                    {
                        arrPoint1[i] = Convert.ToDouble(item);
                        i++;
                    }

                    // Lưu tọa độ gốc CAD
                    CadBeamOrigin = new XyzData(arrPoint1[0], arrPoint1[1], arrPoint1[2]);
                    
                    // Tạo selection set mới
                    var newset = doc.SelectionSets.Add(Guid.NewGuid().ToString());
                    newset.SelectOnScreen();

                    List<dynamic> listText = new List<dynamic>();
                    List<dynamic> listLine = new List<dynamic>();

                    // Phân loại đối tượng được chọn
                    foreach (dynamic s in newset)
                    {
                        if (s.EntityName == "AcDbLine") listLine.Add(s);
                        if (s.EntityName == "AcDbText") listText.Add(s);
                    }

                    List<TextData> listpoint = new List<TextData>();
                    if (listText.Count > 0)
                    {
                        foreach (var text in listText)
                        {
                            string[] arrtextpoint = ((IEnumerable)text.InsertionPoint).Cast<object>().Select(x => x.ToString()).ToArray();
                            double[] arrtextpoint1 = new double[3];
                            int k = 0;
                            foreach (var item in arrtextpoint)
                            {
                                arrtextpoint1[k] = Convert.ToDouble(item);
                                k++;
                            }

                            listpoint.Add(new TextData()
                            {
                                point = new XYZ(arrtextpoint1[0], arrtextpoint1[1], arrtextpoint1[2]),
                                text = text.TextString
                            });
                        }
                    }

                    // Xử lý Line (Dầm) và gán Text tương ứng
                    if (listLine.Count > 0)
                    {
                        foreach (var line in listLine)
                        {
                            dynamic startpointarr = line.StartPoint;
                            dynamic endpointarr = line.EndPoint;

                            var startpoint = new XyzData((double)startpointarr[0], (double)startpointarr[1], 0);
                            var endpoint = new XyzData((double)endpointarr[0], (double)endpointarr[1], 0);

                            // Tìm text gần nhất (nếu có)
                            string beamText = "Default";
                            if (listpoint != null && listpoint.Count > 0)
                            {
                                TextData textData = listpoint.MinBy2(x => x.point.ToXyzfit().DistanceTo(startpoint.ToXyz()));
                                if (textData != null)
                                {
                                    beamText = textData.text;
                                }
                            }

                            _cadBeams.Add(new CadBeams()
                            {
                                StartPoint = startpoint,
                                EndPoint = endpoint,
                                Text = beamText
                            });
                        }
                    }

                    GetBeamInfoCollection(); // Gom nhóm và xử lý dữ liệu dầm
                    dgBeam.ItemsSource = BeamInfoCollections;
                }
            }

            ShowDialog();
        }

        // Xử lý và gom nhóm dầm
        public void GetBeamInfoCollection()
        {
            _beamInfoCollections.Clear();
            var dic = new Dictionary<string, List<BeamInfo>>();

            if (_cadBeams != null)
            {
                foreach (var cadbeam in _cadBeams)
                {
                    var text = cadbeam.Text;
                    var beamInfo = new BeamInfo(cadbeam.StartPoint.ToXyz(), cadbeam.EndPoint.ToXyz(), cadbeam.Text);
                    
                    if (dic.ContainsKey(text))
                        dic[text].Add(beamInfo);
                    else
                        dic.Add(text, new List<BeamInfo> { beamInfo });
                }

                foreach (var pair in dic)
                {
                    var collection = new BeamInfoCollection
                    {
                        Text = pair.Key,
                        Width = pair.Value.Select(x => x.Width).FirstOrDefault(), // Lấy kích thước mặc định từ tên
                        Height = pair.Value.Select(x => x.Height).FirstOrDefault(),
                        Number = pair.Value.Count
                    };
                    
                    // Lọc trùng lặp
                    var b = pair.Value.ToList().Distinct(new BeamInfo.BeamInfoComparerByPoint()).ToList();
                    collection.BeamInfos = b;
                    _beamInfoCollections.Add(collection);
                }
            }
            BeamInfoCollections = new ObservableCollection<BeamInfoCollection>(_beamInfoCollections);
        }

        // Tạo mô hình Dầm trong Revit
        public void ModelBeams()
        {
            Hide();
            var selectedLevel = cbBeamLevels.SelectedItem as Level;
            
            // Lấy Global Type nếu có
            var globalSelectedType = cbBeamType.SelectedItem as FamilySymbol;
            
            // Chọn điểm đặt trong Revit
            try
            {
                _origin = AC.Selection.PickPoint();
            }
            catch (Exception) { /* Handle cancel */ }

            if (_origin != null)
            {
                _origin = new XYZ(_origin.X, _origin.Y, 0);
                var max = BeamInfoCollections.Select(x => x.BeamInfos.Count).Sum();
                var progressView = new progressbar();
                progressView.Show();

                using var tg = new TransactionGroup(AC.Document, "Model Beams");
                tg.Start();

                foreach (var beamInfoCollection in BeamInfoCollections)
                {
                    if (progressView.Flag == false) break;

                    var height = Convert.ToInt32(beamInfoCollection.Height);
                    var width = Convert.ToInt32(beamInfoCollection.Width);
                    var baseOffset = double.Parse(txtBeamOffset.Text);

                    // Logic: Nếu người dùng chọn Type chung ở dưới (globalSelectedType) -> Dùng nó cho TẤT CẢ các dầm
                    // Nếu không -> Dùng logic cũ (tự tìm theo kích thước width/height từng dòng)
                    ElementType elementType = globalSelectedType;
                    
                    if (elementType == null)
                    {
                        elementType = GetElementType(width, height);
                    }
                    beamInfoCollection.ElementType = elementType;

                    if (elementType == null) continue;

                    DeleteWarningSuper waringsuper = new DeleteWarningSuper();

                    foreach (var beamInfo in beamInfoCollection.BeamInfos)
                    {
                        using var tx = new Transaction(AC.Document, "Modeling Beam From Cad");
                        tx.Start();

                        // Xử lý cảnh báo (warning)
                        FailureHandlingOptions failOpt = tx.GetFailureHandlingOptions();
                        failOpt.SetFailuresPreprocessor(waringsuper);
                        tx.SetFailureHandlingOptions(failOpt);

                        var fs = beamInfoCollection.ElementType as FamilySymbol;
                        if (!fs.IsActive) fs.Activate();

                        // Tính toán tọa độ dầm dựa trên điểm gốc CAD và điểm chọn Revit
                        var p1 = beamInfo.StartPoint.Add(_origin - CadBeamOrigin.ToXyz());
                        var p2 = beamInfo.EndPoint.Add(_origin - CadBeamOrigin.ToXyz());
                        var line = Line.CreateBound(p1, p2);
                        
                        var a = line.GetEndPoint(0).EditZ(selectedLevel.Elevation);
                        var b = line.GetEndPoint(1).EditZ(selectedLevel.Elevation);
                        var l = Line.CreateBound(a, b);

                        try
                        {
                            var beam = AC.Document.Create.NewFamilyInstance(l, fs, selectedLevel, StructuralType.Beam);

                            // Set offset
                            var startOffsetParam = beam.LookupParameter("Start Level Offset");
                            if (startOffsetParam is { IsReadOnly: false }) startOffsetParam.Set(baseOffset.MmToFoot());

                            var endOffsetParam = beam.LookupParameter("End Level Offset");
                            if (endOffsetParam is { IsReadOnly: false }) endOffsetParam.Set(baseOffset.MmToFoot());
                        }
                        catch { }

                        AC.Document.Regenerate();
                        progressView.Create(max, "BeamModel");
                        tx.Commit();
                    }
                }
                tg.Assimilate();
                progressView.Close();
            }
            ShowDialog();
        }

        // Lấy dữ liệu mặc định ban đầu cho Dầm
        private void GetDataDefaultBeam()
        {
            var families = new FilteredElementCollector(document)
                .OfCategory(BuiltInCategory.OST_StructuralFraming)
                .OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>()
                .Select(x => x.Family)
                .Where(x => x.StructuralMaterialType != StructuralMaterialType.Steel)
                .GroupBy(x => x.Id).Select(x => x.First()) // Fix DistinctBy
                .OrderBy(x => x.Name).ToList();

            CbFamilyBeamTypes.ItemsSource = families;
            CbFamilyBeamTypes.SelectedItem = families.FirstOrDefault();

            var levels = new FilteredElementCollector(document).OfClass(typeof(Level)).Cast<Level>()
                .OrderBy(x => x.Elevation).ToList();

            cbBeamLevels.ItemsSource = levels;
            cbBeamLevels.SelectedItem = levels.FirstOrDefault();
        }

        #endregion

        #region Columns (Xử lý Cột)

        // Lấy dữ liệu Cột từ AutoCAD
        public void SelectFormCadColumn()
        {
            Hide();
            dynamic a = ComRunningObject.GetActiveObjectByProgId("AutoCaD.Application");
            a.Visible = true;
            a.WindowState = 3;
            a.ActiveDocument.Activate();

            dynamic doc = a.Documents.Application.ActiveDocument;
            string[] arr = null;
            try
            {
                var pointCad = doc.Utility.GetPoint(Type.Missing, "Select point: ");
                arr = ((IEnumerable)pointCad).Cast<object>().Select(x => x.ToString()).ToArray();
            }
            catch (Exception) { }

            if (arr != null)
            {
                double[] arr1 = new double[3];
                int i = 0;
                foreach (var item in arr)
                {
                    arr1[i] = Convert.ToDouble(item);
                    i++;
                }

                CadBeamOrigin = new XyzData(arr1[0], arr1[1], arr1[2]);
                var newset = doc.SelectionSets.Add(Guid.NewGuid().ToString());
                newset.SelectOnScreen();

                List<dynamic> listText = new List<dynamic>();
                List<dynamic> listPolyline = new List<dynamic>();

                foreach (dynamic s in newset)
                {
                    if (s.EntityName == "AcDbPolyline") listPolyline.Add(s);
                    if (s.EntityName == "AcDbText") listText.Add(s);
                }

                List<TextData> listpoint = new List<TextData>();
                if (listText.Count > 0)
                {
                    foreach (var text in listText)
                    {
                        string[] arrtextpoint = ((IEnumerable)text.InsertionPoint).Cast<object>().Select(x => x.ToString()).ToArray();
                        double[] arrtextpoint1 = new double[3];
                        int k = 0;
                        foreach (var item in arrtextpoint)
                        {
                            arrtextpoint1[k] = Convert.ToDouble(item);
                            k++;
                        }
                        listpoint.Add(new TextData()
                        {
                            point = new XYZ(arrtextpoint1[0], arrtextpoint1[1], arrtextpoint1[2]),
                            text = text.TextString
                        });
                    }
                }
                else listpoint = null;

                // Xử lý Polyline (Cột)
                foreach (var polyline in listPolyline)
                {
                    dynamic c = polyline.Coordinates;
                    if (c.Length == 8) // Hình chữ nhật 4 điểm
                    {
                        // Lấy 4 đỉnh
                        dynamic p1Arr = polyline.Coordinate[0];
                        dynamic p2Arr = polyline.Coordinate[1];
                        dynamic p3Arr = polyline.Coordinate[2];
                        dynamic p4Arr = polyline.Coordinate[3];

                        var point1 = new XyzData((double)p1Arr[0], (double)p1Arr[1], 0);
                        var point2 = new XyzData((double)p2Arr[0], (double)p2Arr[1], 0);
                        var point3 = new XyzData((double)p3Arr[0], (double)p3Arr[1], 0);
                        var point4 = new XyzData((double)p4Arr[0], (double)p4Arr[1], 0);

                        string mask = "";
                        if (listText.Count > 0 && listpoint != null)
                        {
                            // Tìm text gần nhất
                            TextData textData = listpoint.MinBy2(x => x.point.ToXyzfit().DistanceTo(point1.ToXyz()));
                            mask = textData.text;
                        }

                        cadRectangles.Add(new CadRectangle()
                        {
                            P1 = point1, P2 = point2, P3 = point3, P4 = point4,
                            Mask = mask
                        });
                    }
                }
                GetColumnInfoCollections();
            }
            ShowDialog();
        }

        // Xử lý và gom nhóm cột
        public void GetColumnInfoCollections()
        {
            columnInfoCollections.Clear();
            var dic = new Dictionary<ColumnInfo, List<ColumnInfo>>();

            if (cadRectangles != null)
            {
                foreach (var cadRectangle in cadRectangles)
                {
                    var points = cadRectangle.Points.Select(x => x.ToXyz()).ToList();
                    if (points.Count == 4)
                    {
                        var columnInfo = new ColumnInfo(points, cadRectangle.Mask);
                        if (dic.ContainsKey(columnInfo))
                            dic[columnInfo].Add(columnInfo);
                        else
                            dic.Add(columnInfo, new List<ColumnInfo> { columnInfo });
                    }
                }

                foreach (var pair in dic)
                {
                    // Kiểm tra tỷ lệ cạnh để loại bỏ các hình quá dẹt (không phải cột)
                    if ((pair.Key.Height / pair.Key.Width) < 5)
                    {
                        var collection = new ColumnInfoCollection
                        {
                            Width = pair.Key.Width,
                            Height = pair.Key.Height,
                            Number = pair.Value.Count,
                            Text = pair.Key.Text
                        };
                        collection.ColumnInfos = pair.Value.ToList();
                        columnInfoCollections.Add(collection);
                    }
                }
            }
            ColumnInfoCollections = new ObservableCollection<ColumnInfoCollection>(columnInfoCollections);
            dgColumn.ItemsSource = ColumnInfoCollections;
        }

        // Tạo mô hình Cột trong Revit
        public void ModelColumn()
        {
            Hide();
            try
            {
                _origin = AC.Selection.PickPoint();
            }
            catch (Exception) { }

            if (_origin != null)
            {
                try
                {
                    _origin = new XYZ(_origin.X, _origin.Y, 0);
                    var max = ColumnInfoCollections.Select(x => x.ColumnInfos.Count).Sum();
                    var progressView = new progressbar();
                    progressView.Show();

                    using (var tg = new TransactionGroup(AC.Document, "Model Columns"))
                    {
                        tg.Start();
                        DeleteWarningSuper waringsuper = new DeleteWarningSuper();

                        foreach (var columnInfoCollection in ColumnInfoCollections)
                        {
                            if (progressView.Flag == false) break;

                            var width = Convert.ToInt32(columnInfoCollection.Width);
                            var height = Convert.ToInt32(columnInfoCollection.Height);

                            // Lấy hoặc tạo Type cột mới
                            var elementType = columnInfoCollection.ElementType = GetColElementType(width, height);
                            if (elementType == null) continue;

                            foreach (var columnInfo in columnInfoCollection.ColumnInfos)
                            {
                                using var tx = new Transaction(AC.Document, "Modeling Column From Cad");
                                tx.Start();
                                FailureHandlingOptions failOpt = tx.GetFailureHandlingOptions();
                                failOpt.SetFailuresPreprocessor(waringsuper);
                                tx.SetFailureHandlingOptions(failOpt);

                                // Tính tâm đặt cột
                                var center = columnInfo.Center.Add(_origin - CadBeamOrigin.ToXyz());
                                var fs = columnInfoCollection.ElementType as FamilySymbol;
                                if (!fs.IsActive) fs.Activate();

                                try
                                {
                                    var selectedBaseLevel = cbBaseLevel.SelectedItem as Level;
                                    var selectedTopLevel = cbTopLevel.SelectedItem as Level;
                                    var topOffset = double.Parse(txtTopOffset.Text);
                                    var baseOffset = double.Parse(txtBaseOffset.Text);

                                    // Tạo cột
                                    var column = AC.Document.Create.NewFamilyInstance(center, fs, selectedBaseLevel, StructuralType.Column);

                                    tx.Commit();
                                    tx.Start();

                                    // Set parameters (Level, Offset)
                                    var pTopLevel = column.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
                                    if (pTopLevel != null && !pTopLevel.IsReadOnly) pTopLevel.Set(selectedTopLevel.Id);

                                    SetColumnOffset(column, "Top Offset", BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM, topOffset);
                                    SetColumnOffset(column, "Base Offset", BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM, baseOffset);

                                    // Xoay cột theo hướng CAD
                                    var rotateAxis = center.CreateLineByPointAndDirection(XYZ.BasisZ);
                                    ElementTransformUtils.RotateElement(AC.Document, column.Id, rotateAxis, columnInfo.Rotation);
                                    
                                    progressView.Create(max, "ColumnModel");
                                }
                                catch { }
                                tx.Commit();
                            }
                        }
                        tg.Assimilate();
                        progressView.Close();
                    }
                }
                catch { }
            }
        }

        private void SetColumnOffset(FamilyInstance column, string paramName, BuiltInParameter bip, double value)
        {
            var param = column.LookupParameter(paramName) 
                        ?? column.get_Parameter(bip) 
                        ?? column.get_Parameter(bip == BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM ? BuiltInParameter.SCHEDULE_TOP_LEVEL_OFFSET_PARAM : BuiltInParameter.SCHEDULE_BASE_LEVEL_OFFSET_PARAM);

            if (param != null && !param.IsReadOnly)
            {
                param.Set(value.MmToFoot());
            }
        }

        // Tìm hoặc tạo Type cột mới
        private ElementType GetColElementType(double width, double height)
        {
            ElementType elementType = null;

            using (var tx = new Transaction(AC.Document, "Duplicate Type"))
            {
                tx.Start();
                AC.Document.Regenerate();

                var selectedItem = cboColumnFamilyType.SelectedItem as Family;
                var columnTypes = selectedItem.GetFamilySymbolIds().Select(x => document.GetElement(x)).Cast<FamilySymbol>().ToList();

                // 1. Tìm type có sẵn khớp kích thước
                foreach (var familySymbol in columnTypes)
                {
                    var bParam = familySymbol.LookupParameter(cbWidthColumn.SelectedItem as string);
                    var hParam = familySymbol.LookupParameter(cbHeightCol.SelectedItem as string);
                    var bInMM = Convert.ToInt32(bParam.AsDouble().FootToMm());
                    var hInMM = Convert.ToInt32(hParam.AsDouble().FootToMm());

                    if (width == bInMM && height == hInMM)
                    {
                        elementType = familySymbol;
                        break;
                    }
                }

                // 2. Nếu không có, Duplicate tạo mới
                if (elementType == null)
                {
                    var type = columnTypes.FirstOrDefault();
                    var newTypeName = "Column" + "_" + width + "x" + height;

                    if (columnTypes.Select(x => x.Name).Contains(newTypeName))
                        newTypeName += " Ignore existed name";

                    while (true)
                    {
                        try
                        {
                            elementType = type?.Duplicate(newTypeName);
                            break;
                        }
                        catch { newTypeName += "."; }
                    }

                    if (elementType != null)
                    {
                        elementType.LookupParameter(cbWidthColumn.SelectedItem as string).Set(width.MmToFoot());
                        elementType.LookupParameter(cbHeightCol.SelectedItem as string).Set(height.MmToFoot());
                    }
                }
                tx.Commit();
            }
            return elementType;
        }

        #endregion

        #region Floors (Xử lý Sàn)

        // Lấy dữ liệu Sàn từ CAD
        public void SelectFloorFromCad()
        {
            Hide();
            dynamic a = ComRunningObject.GetActiveObjectByProgId("AutoCaD.Application");
            a.Visible = true;
            a.WindowState = 3;
            a.ActiveDocument.Activate();

            dynamic doc = a.Documents.Application.ActiveDocument;
            string[] arrPoint = null;
            try
            {
                var pointCad = doc.Utility.GetPoint(Type.Missing, "Select point: ");
                arrPoint = ((IEnumerable)pointCad).Cast<object>().Select(x => x.ToString()).ToArray();
            }
            catch (Exception) { }

            if (arrPoint != null)
            {
                double[] arrPoint1 = new double[3];
                int i = 0;
                foreach (var item in arrPoint)
                {
                    arrPoint1[i] = Convert.ToDouble(item);
                    i++;
                }

                CadBeamOrigin = new XyzData(arrPoint1[0], arrPoint1[1], arrPoint1[2]);
                var newset = doc.SelectionSets.Add(Guid.NewGuid().ToString());
                newset.SelectOnScreen();

                List<dynamic> listPolylines = new List<dynamic>();
                foreach (dynamic s in newset)
                {
                    if (s.EntityName == "AcDbPolyline") listPolylines.Add(s);
                }

                foreach (var polyline in listPolylines)
                {
                    dynamic c = polyline.Coordinates;
                    var ct = Enumerable.Count(c) / 2;
                    var slabPoints = new List<XyzData>();
                    
                    // Lấy các điểm của polyline
                    for (int j = 0; j < ct; j++)
                    {
                        dynamic pointarr = polyline.Coordinate[j];
                        var point = new XyzData((double)pointarr[0], (double)pointarr[1], 0);
                        slabPoints.Add(point);
                    }

                    // Lọc bỏ các điểm quá gần nhau (trùng lặp)
                    for (int item = 0; item < slabPoints.Count; item++)
                    {
                        for (int item1 = 1; item1 < slabPoints.Count; item1++)
                        {
                            if (item < item1)
                            {
                                XYZ displacement = slabPoints[item].ToXyz().Subtract(slabPoints[item1].ToXyz());
                                if (displacement.GetLength() < 0.08)
                                {
                                    slabPoints.RemoveAt(item1);
                                }
                            }
                        }
                    }

                    ListPoint.Add(slabPoints);

                    var collection = new FloorInfoCollection
                    {
                        Area = Math.Round(polyline.Area / 1000000, 1) // Diện tích
                    };
                    floorInfoCollections.Add(collection);
                }

                FloorInfoCollections = new ObservableCollection<FloorInfoCollection>(floorInfoCollections);
                dgFloor.ItemsSource = FloorInfoCollections;
            }
            ShowDialog();
        }

        // Tạo mô hình Sàn trong Revit
        public void ModelFloor()
        {
            Hide();
            try
            {
                _origin = AC.Selection.PickPoint();
            }
            catch (Exception) { }

            if (_origin != null)
            {
                var max = FloorInfoCollections.Count;
                _origin = new XYZ(_origin.X, _origin.Y, 0);
                var progressView = new progressbar();
                progressView.Show();

                using var tg = new TransactionGroup(AC.Document, "Model Floor");
                tg.Start();

                DeleteWarningSuper waringsuper = new DeleteWarningSuper();

                foreach (var listpoint in ListPoint)
                {
                    if (progressView.Flag == false) break;

                    using (var tx = new Transaction(AC.Document, "Modeling Floor From Cad"))
                    {
                        tx.Start();
                        FailureHandlingOptions failOpt = tx.GetFailureHandlingOptions();
                        failOpt.SetFailuresPreprocessor(waringsuper);
                        tx.SetFailureHandlingOptions(failOpt);

                        CurveArray curvearr = new CurveArray();
                        var selectedLevel = cbFloorLevel.SelectedItem as Level;

                        // Tạo các Curve biên dạng sàn
                        for (int k = 0; k < listpoint.Count - 1; k++)
                        {
                            var p1 = listpoint[k].ToXyz().Add(_origin - CadBeamOrigin.ToXyz()).EditZ(selectedLevel.Elevation);
                            var p2 = listpoint[k + 1].ToXyz().Add(_origin - CadBeamOrigin.ToXyz()).EditZ(selectedLevel.Elevation);
                            curvearr.Append(Line.CreateBound(p1, p2));
                        }

                        // Đóng kín biên dạng
                        var pe = listpoint[listpoint.Count - 1].ToXyz().Add(_origin - CadBeamOrigin.ToXyz()).EditZ(selectedLevel.Elevation);
                        var pt = listpoint[0].ToXyz().Add(_origin - CadBeamOrigin.ToXyz()).EditZ(selectedLevel.Elevation);
                        if (pt.DistanceTo(pe) > 0.08)
                        {
                            curvearr.Append(Line.CreateBound(pe, pt));
                        }

                        try
                        {
                            var floorType = cbFloorType.SelectedItem as FloorType;
                            var cl = new CurveLoop();
                            curvearr.ToCurves().ForEach(x => cl.Append(x));
                            
                            // Tạo sàn
                            Floor floor = Floor.Create(AC.Document, new List<CurveLoop>() { cl }, floorType.Id, selectedLevel.Id);
                            
                            // Set offset
                            var offsetParam = floor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM);
                            offsetParam.Set(double.Parse(txtFloorOffset.Text).MmToFoot());
                        }
                        catch { }

                        progressView.Create(max, "FloorModel");
                        tx.Commit();
                    }
                }
                tg.Assimilate();
                progressView.Close();
            }
        }

        // Load dữ liệu mặc định cho Sàn
        private void GetDataDefaultFloor()
        {
            var families = new FilteredElementCollector(AC.Document)
                .OfCategory(BuiltInCategory.OST_Floors)
                .OfClass(typeof(FloorType)).Cast<FloorType>().ToList();

            cbFloorType.ItemsSource = families;
            cbFloorType.SelectedItem = families.FirstOrDefault();

            var levels = new FilteredElementCollector(document).OfClass(typeof(Level)).Cast<Level>()
                .OrderBy(x => x.Elevation).ToList();

            cbFloorLevel.ItemsSource = levels;
            cbFloorLevel.SelectedItem = levels.FirstOrDefault();
        }

        #endregion

        #region Helper Methods (Phương thức hỗ trợ chung)

        // Tìm hoặc tạo Type dầm (tương tự như cột)
        private ElementType GetElementType(double width, double heigt)
        {
            ElementType elementType = null;

            using var tx = new Transaction(AC.Document, "Duplicate Type");
            tx.Start();
            AC.Document.Regenerate();

            var selectedItem = CbFamilyBeamTypes.SelectedItem as Family;
            var beamTypes = selectedItem.GetFamilySymbolIds().Select(x => document.GetElement(x)).Cast<FamilySymbol>().ToList();

            foreach (var familySymbol in beamTypes)
            {
                var bParameter = familySymbol.LookupParameter(cbBeamWidth.SelectedItem as string);
                var binMm = Convert.ToInt32(bParameter.AsDouble().FootToMm());
                var hParameter = familySymbol.LookupParameter(cbBeamHeight.SelectedItem as string);
                var hinMm = Convert.ToInt32(hParameter.AsDouble().FootToMm());

                if (heigt == hinMm && width == binMm)
                {
                    elementType = familySymbol;
                    break; // Tìm thấy thì dừng
                }
            }

            if (elementType == null)
            {
                var type = beamTypes.FirstOrDefault();
                var newTypeName = "Beams" + "" + width + "x" + heigt;
                var i = 1;

                if (beamTypes.Select(x => x.Name).Contains(newTypeName))
                    newTypeName = $"{newTypeName} (1)";

                while (true)
                {
                    try
                    {
                        elementType = type?.Duplicate(newTypeName);
                        break;
                    }
                    catch
                    {
                        i++;
                        newTypeName = $"{newTypeName} ({i})";
                    }
                }

                if (elementType != null)
                {
                    elementType.LookupParameter(cbBeamWidth.SelectedItem as string).Set(width.MmToFoot());
                    elementType.LookupParameter(cbBeamHeight.SelectedItem as string).Set(Utils.Utils.MmToFoot(heigt));
                }
            }
            tx.Commit();
            return elementType;
        }

        #endregion
    }
}