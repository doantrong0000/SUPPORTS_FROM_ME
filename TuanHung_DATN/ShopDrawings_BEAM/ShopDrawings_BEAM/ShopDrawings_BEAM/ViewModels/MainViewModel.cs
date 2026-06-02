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

        // --- Các thuộc tính được chọn (Selected Items) ---
        // Bản vẽ dọc
        public FamilySymbol SelectedTitleBlock { get; set; }
        public DimensionType SelectedDimType { get; set; }
        public View SelectedLongitudinalViewTemplate { get; set; }
        public FamilySymbol SelectedLongitudinalMainRebarTag { get; set; }
        public FamilySymbol SelectedLongitudinalStirrupTag { get; set; }

        // Mặt cắt
        public ViewFamilyType SelectedSectionViewType { get; set; }
        public View SelectedSectionViewTemplate { get; set; }
        public MultiReferenceAnnotationType SelectedCrossSectionMainRebarTag { get; set; }
        public FamilySymbol SelectedCrossSectionStirrupTag { get; set; }

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
            SelectedLongitudinalViewTemplate = ViewTemplates.FirstOrDefault(x => x.Name.ToLower().Contains("dọc") || x.Name.ToLower().Contains("doc") || x.Name.ToLower().Contains("dam")) ?? ViewTemplates.FirstOrDefault();
            SelectedSectionViewTemplate = ViewTemplates.FirstOrDefault(x => x.Name.ToLower().Contains("cắt") || x.Name.ToLower().Contains("cat") || x.Name.ToLower().Contains("ngang")) ?? ViewTemplates.FirstOrDefault();

            // Auto-select Section View Type
            SelectedSectionViewType = SectionViewTypes.FirstOrDefault(x => x.Name.ToLower().Contains("cắt") || x.Name.ToLower().Contains("section")) ?? SectionViewTypes.FirstOrDefault();

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
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
