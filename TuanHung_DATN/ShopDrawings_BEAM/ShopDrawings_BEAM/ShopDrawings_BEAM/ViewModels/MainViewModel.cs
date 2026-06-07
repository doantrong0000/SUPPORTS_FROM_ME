using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Autodesk.Revit.DB;

namespace ShopDrawings_BEAM.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private Document _doc;
        public Document Doc => _doc;

        // --- Danh sách (Collections) dùng cho ItemsSource ---
        public ObservableCollection<View> ViewTemplates { get; set; }
        public ObservableCollection<FamilySymbol> RebarTags { get; set; }
        public ObservableCollection<MultiReferenceAnnotationType> MultiRebarTags { get; set; }
        public ObservableCollection<DimensionType> DimensionTypes { get; set; }
        public ObservableCollection<FamilySymbol> TitleBlocks { get; set; }
        public ObservableCollection<ViewFamilyType> SectionViewTypes { get; set; }
        public ObservableCollection<ViewFamilyType> DetailViewTypes { get; set; }
        public ObservableCollection<ElementType> ViewportTypes { get; set; }

        // --- Các thuộc tính được chọn (Selected Items) ---
        // Bản vẽ dọc
        public FamilySymbol SelectedTitleBlock { get; set; }
        public DimensionType SelectedDimType { get; set; }
        public View SelectedLongitudinalViewTemplate { get; set; }
        public FamilySymbol SelectedLongitudinalMainRebarTag { get; set; }
        public FamilySymbol SelectedLongitudinalStirrupTag { get; set; }

        // Mặt cắt
        public ViewFamilyType SelectedSectionViewType { get; set; }
        public ViewFamilyType SelectedDetailViewType { get; set; }
        public View SelectedSectionViewTemplate { get; set; }
        public MultiReferenceAnnotationType SelectedCrossSectionMainRebarTag { get; set; }
        public FamilySymbol SelectedCrossSectionStirrupTag { get; set; }
        public ElementType SelectedLongitudinalViewportType { get; set; }
        public ElementType SelectedCrossSectionViewportType { get; set; }

        // Mở rộng
        public ObservableCollection<string> ViewScales { get; set; }
        public string SelectedViewScale { get; set; }
        
        // Tiến trình
        private int _progressValue;
        public int ProgressValue
        {
            get => _progressValue;
            set { _progressValue = value; OnPropertyChanged(); }
        }

        // --- Hỗ trợ Wizard & Thông tin Dầm ---
        private int _currentStep = 1;
        public int CurrentStep
        {
            get => _currentStep;
            set 
            { 
                _currentStep = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(IsStep1));
                OnPropertyChanged(nameof(IsStep2));
            }
        }

        public bool IsStep1 => CurrentStep == 1;
        public bool IsStep2 => CurrentStep == 2;

        private bool _hasSelectedBeam;
        public bool HasSelectedBeam
        {
            get => _hasSelectedBeam;
            set { _hasSelectedBeam = value; OnPropertyChanged(); }
        }

        private string _beamName = "Chưa chọn dầm";
        public string BeamName
        {
            get => _beamName;
            set { _beamName = value; OnPropertyChanged(); }
        }

        private string _beamSize = "-";
        public string BeamSize
        {
            get => _beamSize;
            set { _beamSize = value; OnPropertyChanged(); }
        }

        private string _beamLength = "-";
        public string BeamLength
        {
            get => _beamLength;
            set { _beamLength = value; OnPropertyChanged(); }
        }

        private string _beamLevel = "-";
        public string BeamLevel
        {
            get => _beamLevel;
            set { _beamLevel = value; OnPropertyChanged(); }
        }

        // --- Đối tượng bản vẽ & dầm đã chọn để vẽ ---
        public System.Collections.Generic.List<Element> SelectedBeams { get; set; } = new System.Collections.Generic.List<Element>();
        public ViewSheet CreatedSheet { get; set; }
        public ViewSection CreatedLongitudinalView { get; set; }
        public System.Collections.Generic.List<ViewSection> CreatedCrossSections { get; set; } = new System.Collections.Generic.List<ViewSection>();

        public MainViewModel(Document doc)
        {
            _doc = doc;
            LoadData();
        }

        private void LoadData()
        {
            // View Templates
            var viewTemplates = new FilteredElementCollector(_doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v => v.IsTemplate)
                .ToList();
            ViewTemplates = new ObservableCollection<View>(viewTemplates);

            // Tag thép (Tag thép dọc mặt cắt dọc, Tag thép đai mặt cắt dọc/ngang)
            var rebarTags = new FilteredElementCollector(_doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_RebarTags)
                .Cast<FamilySymbol>()
                .ToList();
            RebarTags = new ObservableCollection<FamilySymbol>(rebarTags);

            // Tag thép dọc mặt cắt (MultiRebar Annotation)
            var multiRebarTags = new FilteredElementCollector(_doc)
                .OfClass(typeof(MultiReferenceAnnotationType))
                .Cast<MultiReferenceAnnotationType>()
                .ToList();
            MultiRebarTags = new ObservableCollection<MultiReferenceAnnotationType>(multiRebarTags);

            // Dimension Types
            var dimTypes = new FilteredElementCollector(_doc)
                .OfClass(typeof(DimensionType))
                .Cast<DimensionType>()
                .ToList();
            DimensionTypes = new ObservableCollection<DimensionType>(dimTypes);

            // Title Blocks
            var titleBlocks = new FilteredElementCollector(_doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .Cast<FamilySymbol>()
                .ToList();
            TitleBlocks = new ObservableCollection<FamilySymbol>(titleBlocks);

        // Detail Items (Cho thép chủ, thép đai trên mặt cắt) - Bỏ qua do sử dụng mặc định

            // Section View Types
            var sectionViewTypes = new FilteredElementCollector(_doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .Where(v => v.ViewFamily == ViewFamily.Section)
                .ToList();
            SectionViewTypes = new ObservableCollection<ViewFamilyType>(sectionViewTypes);

            // Detail View Types (for cross section detail views)
            var detailViewTypes = new FilteredElementCollector(_doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .Where(v => v.ViewFamily == ViewFamily.Detail)
                .ToList();
            DetailViewTypes = new ObservableCollection<ViewFamilyType>(detailViewTypes);

            // Viewport Types
            var viewportTypes = new FilteredElementCollector(_doc)
                .OfClass(typeof(ElementType))
                .Cast<ElementType>()
                .Where(x => x.FamilyName != null && x.FamilyName.Equals("Viewport", System.StringComparison.OrdinalIgnoreCase))
                .ToList();
            ViewportTypes = new ObservableCollection<ElementType>(viewportTypes);

            ViewScales = new ObservableCollection<string> { "1:20", "1:25", "1:50", "1:100" };

            AutoSelectInitialValues();
        }

        private void AutoSelectInitialValues()
        {
            // Auto-select Khung tên
            SelectedTitleBlock = TitleBlocks.FirstOrDefault(x => x.Name.ToLower().Contains("khung") || x.Name.ToLower().Contains("a3")) ?? TitleBlocks.FirstOrDefault();

            // Auto-select Dim Type
            SelectedDimType = DimensionTypes.FirstOrDefault(x => x.Name.ToLower().Contains("dim") || x.Name.ToLower().Contains("100")) ?? DimensionTypes.FirstOrDefault();

            // Auto-select View Templates
            SelectedLongitudinalViewTemplate = ViewTemplates.FirstOrDefault(x => x.Name.ToLower().Contains("beam") && (x.Name.ToLower().Contains("dọc") || x.Name.ToLower().Contains("doc")))
                ?? ViewTemplates.FirstOrDefault(x => x.Name.ToLower().Contains("beam"))
                ?? ViewTemplates.FirstOrDefault(x => x.Name.ToLower().Contains("dọc") || x.Name.ToLower().Contains("doc") || x.Name.ToLower().Contains("dam"))
                ?? ViewTemplates.FirstOrDefault();

            SelectedSectionViewTemplate = ViewTemplates.FirstOrDefault(x => x.Name.ToLower().Contains("beam") && (x.Name.ToLower().Contains("cắt") || x.Name.ToLower().Contains("cat") || x.Name.ToLower().Contains("ngang")))
                ?? ViewTemplates.FirstOrDefault(x => x.Name.ToLower().Contains("beam"))
                ?? ViewTemplates.FirstOrDefault(x => x.Name.ToLower().Contains("cắt") || x.Name.ToLower().Contains("cat") || x.Name.ToLower().Contains("ngang"))
                ?? ViewTemplates.FirstOrDefault();

            // Auto-select Section View Type
            SelectedSectionViewType = SectionViewTypes.FirstOrDefault(x => x.Name.ToLower().Contains("cắt") || x.Name.ToLower().Contains("section")) ?? SectionViewTypes.FirstOrDefault();

            // Auto-select Detail View Type
            SelectedDetailViewType = DetailViewTypes.FirstOrDefault(x => x.Name.ToLower().Contains("detail") || x.Name.ToLower().Contains("chi tiết") || x.Name.ToLower().Contains("chi tiet")) ?? DetailViewTypes.FirstOrDefault();

            // Auto-select Viewport Type
            SelectedLongitudinalViewportType = ViewportTypes.FirstOrDefault(x => x.Name.ToLower().Contains("show") || x.Name.ToLower().Contains("tiêu đề") || x.Name.ToLower().Contains("tieu de") || x.Name.ToLower().Contains("title"))
                ?? ViewportTypes.FirstOrDefault();

            SelectedCrossSectionViewportType = ViewportTypes.FirstOrDefault(x => x.Name.ToLower().Contains("no title") || x.Name.ToLower().Contains("không tiêu đề") || x.Name.ToLower().Contains("khong tieu de"))
                ?? ViewportTypes.FirstOrDefault();

            // Auto-select Tags (Mặt cắt dọc)
            SelectedLongitudinalMainRebarTag = RebarTags.FirstOrDefault(x => x.Name.ToLower().Contains("chủ") || x.Name.ToLower().Contains("chu") || x.Name.ToLower().Contains("dọc") || x.Name.ToLower().Contains("doc")) ?? RebarTags.FirstOrDefault();
            SelectedLongitudinalStirrupTag = RebarTags.FirstOrDefault(x => x.Name.ToLower().Contains("đai") || x.Name.ToLower().Contains("dai")) ?? RebarTags.FirstOrDefault();

            // Auto-select Tags (Mặt cắt ngang)
            SelectedCrossSectionMainRebarTag = MultiRebarTags.FirstOrDefault(x => x.Name.ToLower().Contains("thép") || x.Name.ToLower().Contains("thep") || x.Name.ToLower().Contains("multi")) ?? MultiRebarTags.FirstOrDefault();
            SelectedCrossSectionStirrupTag = RebarTags.FirstOrDefault(x => x.Name.ToLower().Contains("đai") || x.Name.ToLower().Contains("dai")) ?? RebarTags.FirstOrDefault();

            SelectedViewScale = "1:50";
            ProgressValue = 0;
            CurrentStep = 1;
            HasSelectedBeam = false;

            LoadSettings();
        }

        public void SetSelectedBeams(System.Collections.Generic.List<Element> beams)
        {
            if (beams == null || beams.Count == 0) return;

            SelectedBeams = beams;

            if (beams.Count == 1)
            {
                SetSelectedBeam(beams[0]);
                return;
            }

            HasSelectedBeam = true;

            // Tên dầm tổng hợp
            if (beams.Count <= 3)
            {
                BeamName = string.Join(", ", beams.Select(b => b.Name));
            }
            else
            {
                BeamName = $"{beams[0].Name} và {beams.Count - 1} dầm khác";
            }

            // Lấy Level của dầm đầu tiên làm chuẩn
            var levelId = beams[0].LevelId;
            if (levelId != ElementId.InvalidElementId)
            {
                var lvl = _doc.GetElement(levelId);
                BeamLevel = lvl?.Name ?? "-";
            }
            else
            {
                var lvlParam = beams[0].get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM);
                BeamLevel = lvlParam?.AsValueString() ?? "-";
            }

            // Tính tổng Chiều dài
            double totalLengthVal = 0;
            foreach (var b in beams)
            {
                var lenParam = b.get_Parameter(BuiltInParameter.STRUCTURAL_FRAME_CUT_LENGTH);
                if (lenParam != null)
                {
                    totalLengthVal += lenParam.AsDouble();
                }
            }

            if (totalLengthVal > 0)
            {
                // Revit lưu trữ internal bằng feet, đổi sang mm để hiển thị trực quan
                double mmVal = totalLengthVal * 304.8;
                BeamLength = $"{Math.Round(mmVal)} mm (Tổng cộng)";
            }
            else
            {
                BeamLength = "-";
            }

            // Lấy Tiết diện (B x H) của dầm đầu tiên làm đại diện
            var typeId = beams[0].GetTypeId();
            if (typeId != ElementId.InvalidElementId)
            {
                var typeElem = _doc.GetElement(typeId);
                if (typeElem != null)
                {
                    var bParam = typeElem.LookupParameter("b") ?? typeElem.LookupParameter("Width") ?? typeElem.LookupParameter("B");
                    var hParam = typeElem.LookupParameter("h") ?? typeElem.LookupParameter("Height") ?? typeElem.LookupParameter("H");
                    if (bParam != null && hParam != null)
                    {
                        BeamSize = $"{bParam.AsValueString()} x {hParam.AsValueString()}";
                    }
                    else
                    {
                        BeamSize = typeElem.Name;
                    }
                }
            }
            SelectTemplateBasedOnDirection();
        }

        public void SetSelectedBeam(Element beam)
        {
            if (beam == null) return;

            SelectedBeams = new System.Collections.Generic.List<Element> { beam };

            BeamName = beam.Name;
            HasSelectedBeam = true;

            // Lấy Level
            var levelId = beam.LevelId;
            if (levelId != ElementId.InvalidElementId)
            {
                var lvl = _doc.GetElement(levelId);
                BeamLevel = lvl?.Name ?? "-";
            }
            else
            {
                var lvlParam = beam.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM);
                BeamLevel = lvlParam?.AsValueString() ?? "-";
            }

            // Lấy Chiều dài
            var lenParam = beam.get_Parameter(BuiltInParameter.STRUCTURAL_FRAME_CUT_LENGTH);
            BeamLength = lenParam != null ? lenParam.AsValueString() : "-";

            // Lấy Tiết diện (B x H)
            var typeId = beam.GetTypeId();
            if (typeId != ElementId.InvalidElementId)
            {
                var typeElem = _doc.GetElement(typeId);
                if (typeElem != null)
                {
                    var bParam = typeElem.LookupParameter("b") ?? typeElem.LookupParameter("Width") ?? typeElem.LookupParameter("B");
                    var hParam = typeElem.LookupParameter("h") ?? typeElem.LookupParameter("Height") ?? typeElem.LookupParameter("H");
                    if (bParam != null && hParam != null)
                    {
                        BeamSize = $"{bParam.AsValueString()} x {hParam.AsValueString()}";
                    }
                    else
                    {
                        BeamSize = typeElem.Name;
                    }
                }
            }
            SelectTemplateBasedOnDirection();
        }

        private void SelectTemplateBasedOnDirection()
        {
            if (SelectedBeams == null || SelectedBeams.Count == 0) return;

            Element beam = SelectedBeams[0];
            LocationCurve locCurve = beam.Location as LocationCurve;
            if (locCurve == null) return;

            Line line = locCurve.Curve as Line;
            if (line == null) return;

            XYZ dir = line.Direction.Normalize();

            // Nếu |X| > |Y|, dầm chủ yếu chạy theo trục X -> Chọn Beam Px
            // Ngược lại nếu |Y| >= |X|, dầm chạy theo trục Y -> Chọn Beam Py
            bool isMostlyX = Math.Abs(dir.X) > Math.Abs(dir.Y);
            string targetTemplateName = isMostlyX ? "beam px" : "beam py";

            var template = ViewTemplates.FirstOrDefault(t => t.Name.ToLower().Contains(targetTemplateName));
            if (template != null)
            {
                SelectedLongitudinalViewTemplate = template;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // --- JSON Settings Save/Load ---
        private string GetSettingsFilePath()
        {
            string appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
            string folder = System.IO.Path.Combine(appData, "ShopDrawings_BEAM");
            if (!System.IO.Directory.Exists(folder))
            {
                System.IO.Directory.CreateDirectory(folder);
            }
            return System.IO.Path.Combine(folder, "settings.json");
        }

        public void SaveSettings()
        {
            try
            {
                var settings = new UserSettings
                {
                    SelectedTitleBlockName = SelectedTitleBlock?.Name,
                    SelectedDimTypeName = SelectedDimType?.Name,
                    SelectedLongitudinalViewTemplateName = SelectedLongitudinalViewTemplate?.Name,
                    SelectedLongitudinalMainRebarTagName = SelectedLongitudinalMainRebarTag?.Name,
                    SelectedLongitudinalStirrupTagName = SelectedLongitudinalStirrupTag?.Name,
                    SelectedSectionViewTypeName = SelectedSectionViewType?.Name,
                    SelectedDetailViewTypeName = SelectedDetailViewType?.Name,
                    SelectedSectionViewTemplateName = SelectedSectionViewTemplate?.Name,
                    SelectedCrossSectionMainRebarTagName = SelectedCrossSectionMainRebarTag?.Name,
                    SelectedCrossSectionStirrupTagName = SelectedCrossSectionStirrupTag?.Name,
                    SelectedLongitudinalViewportTypeName = SelectedLongitudinalViewportType?.Name,
                    SelectedCrossSectionViewportTypeName = SelectedCrossSectionViewportType?.Name,
                    SelectedViewScale = SelectedViewScale
                };

                string json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(GetSettingsFilePath(), json);
            }
            catch
            {
                // Silently ignore settings save errors
            }
        }

        public void LoadSettings()
        {
            try
            {
                string path = GetSettingsFilePath();
                if (!System.IO.File.Exists(path)) return;

                string json = System.IO.File.ReadAllText(path);
                var settings = System.Text.Json.JsonSerializer.Deserialize<UserSettings>(json);
                if (settings == null) return;

                if (!string.IsNullOrEmpty(settings.SelectedTitleBlockName))
                {
                    var match = TitleBlocks.FirstOrDefault(x => x.Name == settings.SelectedTitleBlockName);
                    if (match != null) SelectedTitleBlock = match;
                }
                if (!string.IsNullOrEmpty(settings.SelectedDimTypeName))
                {
                    var match = DimensionTypes.FirstOrDefault(x => x.Name == settings.SelectedDimTypeName);
                    if (match != null) SelectedDimType = match;
                }
                if (!string.IsNullOrEmpty(settings.SelectedLongitudinalViewTemplateName))
                {
                    var match = ViewTemplates.FirstOrDefault(x => x.Name == settings.SelectedLongitudinalViewTemplateName);
                    if (match != null) SelectedLongitudinalViewTemplate = match;
                }
                if (!string.IsNullOrEmpty(settings.SelectedLongitudinalMainRebarTagName))
                {
                    var match = RebarTags.FirstOrDefault(x => x.Name == settings.SelectedLongitudinalMainRebarTagName);
                    if (match != null) SelectedLongitudinalMainRebarTag = match;
                }
                if (!string.IsNullOrEmpty(settings.SelectedLongitudinalStirrupTagName))
                {
                    var match = RebarTags.FirstOrDefault(x => x.Name == settings.SelectedLongitudinalStirrupTagName);
                    if (match != null) SelectedLongitudinalStirrupTag = match;
                }
                if (!string.IsNullOrEmpty(settings.SelectedSectionViewTypeName))
                {
                    var match = SectionViewTypes.FirstOrDefault(x => x.Name == settings.SelectedSectionViewTypeName);
                    if (match != null) SelectedSectionViewType = match;
                }
                if (!string.IsNullOrEmpty(settings.SelectedDetailViewTypeName))
                {
                    var match = DetailViewTypes.FirstOrDefault(x => x.Name == settings.SelectedDetailViewTypeName);
                    if (match != null) SelectedDetailViewType = match;
                }
                if (!string.IsNullOrEmpty(settings.SelectedSectionViewTemplateName))
                {
                    var match = ViewTemplates.FirstOrDefault(x => x.Name == settings.SelectedSectionViewTemplateName);
                    if (match != null) SelectedSectionViewTemplate = match;
                }
                if (!string.IsNullOrEmpty(settings.SelectedCrossSectionMainRebarTagName))
                {
                    var match = MultiRebarTags.FirstOrDefault(x => x.Name == settings.SelectedCrossSectionMainRebarTagName);
                    if (match != null) SelectedCrossSectionMainRebarTag = match;
                }
                if (!string.IsNullOrEmpty(settings.SelectedCrossSectionStirrupTagName))
                {
                    var match = RebarTags.FirstOrDefault(x => x.Name == settings.SelectedCrossSectionStirrupTagName);
                    if (match != null) SelectedCrossSectionStirrupTag = match;
                }
                if (!string.IsNullOrEmpty(settings.SelectedLongitudinalViewportTypeName))
                {
                    var match = ViewportTypes.FirstOrDefault(x => x.Name == settings.SelectedLongitudinalViewportTypeName);
                    if (match != null) SelectedLongitudinalViewportType = match;
                }
                if (!string.IsNullOrEmpty(settings.SelectedCrossSectionViewportTypeName))
                {
                    var match = ViewportTypes.FirstOrDefault(x => x.Name == settings.SelectedCrossSectionViewportTypeName);
                    if (match != null) SelectedCrossSectionViewportType = match;
                }
                if (!string.IsNullOrEmpty(settings.SelectedViewScale))
                {
                    var match = ViewScales.FirstOrDefault(x => x == settings.SelectedViewScale);
                    if (match != null) SelectedViewScale = match;
                }
            }
            catch
            {
                // Silently ignore settings load errors
            }
        }
    }

    public class UserSettings
    {
        public string SelectedTitleBlockName { get; set; }
        public string SelectedDimTypeName { get; set; }
        public string SelectedLongitudinalViewTemplateName { get; set; }
        public string SelectedLongitudinalMainRebarTagName { get; set; }
        public string SelectedLongitudinalStirrupTagName { get; set; }
        public string SelectedSectionViewTypeName { get; set; }
        public string SelectedDetailViewTypeName { get; set; }
        public string SelectedSectionViewTemplateName { get; set; }
        public string SelectedCrossSectionMainRebarTagName { get; set; }
        public string SelectedCrossSectionStirrupTagName { get; set; }
        public string SelectedLongitudinalViewportTypeName { get; set; }
        public string SelectedCrossSectionViewportTypeName { get; set; }
        public string SelectedViewScale { get; set; }
    }
}
