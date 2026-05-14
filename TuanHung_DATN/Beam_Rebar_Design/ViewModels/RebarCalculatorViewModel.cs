using Beam_Rebar_Design.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace Beam_Rebar_Design.ViewModels
{
    public class RebarCalculatorViewModel : INotifyPropertyChanged
    {
        private readonly EtabsConnector _etabs = new EtabsConnector();

        private ObservableCollection<BeamModel> _beamList = new ObservableCollection<BeamModel>();
        public ObservableCollection<BeamModel> BeamList
        {
            get => _beamList;
            set { _beamList = value; OnPropertyChanged(nameof(BeamList)); }
        }

        private ObservableCollection<string> _outputCases = new ObservableCollection<string>();
        public ObservableCollection<string> OutputCases
        {
            get => _outputCases;
            set { _outputCases = value; OnPropertyChanged(nameof(OutputCases)); }
        }

        private string _selectedOutputCase;
        public string SelectedOutputCase
        {
            get => _selectedOutputCase;
            set { _selectedOutputCase = value; OnPropertyChanged(nameof(SelectedOutputCase)); }
        }

        private string _connectionStatus = "Chưa kết nối";
        public string ConnectionStatus
        {
            get => _connectionStatus;
            set { _connectionStatus = value; OnPropertyChanged(nameof(ConnectionStatus)); }
        }

        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set { _isConnected = value; OnPropertyChanged(nameof(IsConnected)); }
        }

        public List<ConcreteModel> ConcreteList { get; set; } = new List<ConcreteModel>();
        public List<RebarModel> RebarList { get; set; } = new List<RebarModel>();

        public List<string> StirrupDiameterList { get; set; } = new List<string>
        { "Ø6", "Ø8", "Ø10", "Ø12" };

        private string _selectedStirrupType = "Ø8";
        public string SelectedStirrupType
        {
            get => _selectedStirrupType;
            set { _selectedStirrupType = value; OnPropertyChanged(nameof(SelectedStirrupType)); UpdateAllStirrupCalculations(); }
        }

        private RebarModel _selectedStirrupMaterial;
        public RebarModel SelectedStirrupMaterial
        {
            get => _selectedStirrupMaterial;
            set { _selectedStirrupMaterial = value; OnPropertyChanged(nameof(SelectedStirrupMaterial)); UpdateAllStirrupCalculations(); }
        }

        private ConcreteModel _selectedConcrete;
        public ConcreteModel SelectedConcrete
        {
            get => _selectedConcrete;
            set { _selectedConcrete = value; OnPropertyChanged(nameof(SelectedConcrete)); }
        }

        private RebarModel _selectedRebarLongitudinal;
        public RebarModel SelectedRebarLongitudinal
        {
            get => _selectedRebarLongitudinal;
            set
            {
                _selectedRebarLongitudinal = value;
                OnPropertyChanged(nameof(SelectedRebarLongitudinal));
                OnPropertyChanged(nameof(KsiRDisplay));
                OnPropertyChanged(nameof(AlphaRDisplay));
            }
        }

        private double _protectiveLayer = 25;
        public double ProtectiveLayer
        {
            get => _protectiveLayer;
            set { _protectiveLayer = value; OnPropertyChanged(nameof(ProtectiveLayer)); }
        }

        private double _defaultFloorThickness = 120;
        public double DefaultFloorThickness
        {
            get => _defaultFloorThickness;
            set { _defaultFloorThickness = value; OnPropertyChanged(nameof(DefaultFloorThickness)); }
        }

        private double _defaultWallHeight = 3000;
        public double DefaultWallHeight
        {
            get => _defaultWallHeight;
            set { _defaultWallHeight = value; OnPropertyChanged(nameof(DefaultWallHeight)); }
        }

        public string KsiRDisplay => SelectedRebarLongitudinal != null ? $"{SelectedRebarLongitudinal.KsiR:F3}" : "0.615";
        public string AlphaRDisplay => SelectedRebarLongitudinal != null ? $"{SelectedRebarLongitudinal.AlphaR:F3}" : "0.426";
        public double AlphaR => SelectedRebarLongitudinal?.AlphaR ?? 0.426;

        private List<DetailedFrameForceData> _cachedForceData;

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public RebarCalculatorViewModel()
        {
            LoadMaterialData();
            if (ConcreteList.Count > 2) SelectedConcrete = ConcreteList[2];
            if (RebarList.Count > 1) SelectedRebarLongitudinal = RebarList[1];
            if (RebarList.Count > 0) SelectedStirrupMaterial = RebarList[0];
        }

        private void LoadMaterialData()
        {
            ConcreteList.Add(new ConcreteModel { Name = "B20", Rb = 11.5, Rbt = 0.9 });
            ConcreteList.Add(new ConcreteModel { Name = "B25", Rb = 14.5, Rbt = 1.05 });
            ConcreteList.Add(new ConcreteModel { Name = "B30", Rb = 17, Rbt = 1.15 });
            ConcreteList.Add(new ConcreteModel { Name = "B35", Rb = 19.5, Rbt = 1.3 });
            ConcreteList.Add(new ConcreteModel { Name = "B40", Rb = 22, Rbt = 1.4 });
            ConcreteList.Add(new ConcreteModel { Name = "B45", Rb = 25, Rbt = 1.5 });
            ConcreteList.Add(new ConcreteModel { Name = "B50", Rb = 27.5, Rbt = 1.6 });

            RebarList.Add(new RebarModel { Name = "CB240-T", Rs = 210, Rsw = 170, KsiR = 0.615, AlphaR = 0.426 });
            RebarList.Add(new RebarModel { Name = "CB300-T", Rs = 260, Rsw = 210, KsiR = 0.583, AlphaR = 0.413 });
            RebarList.Add(new RebarModel { Name = "CB300-V", Rs = 260, Rsw = 210, KsiR = 0.583, AlphaR = 0.413 });
            RebarList.Add(new RebarModel { Name = "CB400-V", Rs = 350, Rsw = 280, KsiR = 0.533, AlphaR = 0.391 });
            RebarList.Add(new RebarModel { Name = "CB500-V", Rs = 435, Rsw = 300, KsiR = 0.493, AlphaR = 0.372 });
        }

        #region ETABS Connection

        public bool ConnectToEtabs()
        {
            bool success = _etabs.Connect();
            IsConnected = success;
            ConnectionStatus = success ? $"✓ Đã kết nối: {_etabs.GetModelName()}" : "✗ Không kết nối được ETABS";
            return success;
        }

        public bool LoadBeamsFromEtabs()
        {
            if (!_etabs.IsConnected) return false;
            try
            {
                BeamList.Clear();
                var beams = _etabs.GetAllBeams();
                foreach (var info in beams.OrderBy(b => b.Label))
                {
                    BeamList.Add(new BeamModel
                    {
                        Name = info.FrameName,
                        Story = info.Story,
                        SectionName = info.SectionName,
                        B = info.B,
                        H = info.H,
                        Ltt = info.Length,
                        SpanLeft = 0,
                        SpanRight = 0,
                        FloorThicknessLeft = _defaultFloorThickness,
                        FloorThicknessRight = _defaultFloorThickness,
                        WallHeight = _defaultWallHeight,
                        Station1 = 0.0, Station2 = 0.5, Station3 = 1.0,
                    });
                }
                return BeamList.Count > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi load dầm từ ETABS: {ex.Message}");
                return false;
            }
        }

        public bool LoadOutputCases()
        {
            if (!_etabs.IsConnected) return false;
            try
            {
                OutputCases.Clear();
                var cases = _etabs.GetAllLoadCasesAndCombos();
                foreach (var c in cases) OutputCases.Add(c);
                return OutputCases.Count > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi load output cases: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Load Forces from ETABS

        public bool LoadForcesFromEtabs()
        {
            if (!_etabs.IsConnected || string.IsNullOrEmpty(SelectedOutputCase)) return false;
            try
            {
                var frameNames = BeamList.Select(b => b.Name).ToList();
                var forceDataList = _etabs.GetFrameForces(SelectedOutputCase, frameNames);
                if (!forceDataList.Any()) return false;

                _cachedForceData = forceDataList;
                UpdateBeamForces(forceDataList);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi load nội lực: {ex.Message}");
                return false;
            }
        }

        private void UpdateBeamForces(List<DetailedFrameForceData> forceDataList)
        {
            foreach (var beamModel in BeamList)
            {
                var frameForces = forceDataList.Where(f => f.FrameName == beamModel.Name).ToList();
                if (frameForces.Any())
                {
                    bool hasCombinationMaxMin = frameForces.Any(f =>
                        f.StepType != null &&
                        (f.StepType.Equals("Max", StringComparison.OrdinalIgnoreCase) ||
                         f.StepType.Equals("Min", StringComparison.OrdinalIgnoreCase)));

                    if (hasCombinationMaxMin)
                        UpdateBeamForcesFromComboMaxMin(beamModel, frameForces);
                    else
                        UpdateBeamForcesFromSimpleCase(beamModel, frameForces);
                }
                else
                {
                    ResetBeamForces(beamModel);
                }
            }
            OnPropertyChanged(nameof(BeamList));
        }

        private void UpdateBeamForcesFromComboMaxMin(BeamModel beam, List<DetailedFrameForceData> frameForces)
        {
            var minRecords = frameForces.Where(f => f.StepType != null && f.StepType.Equals("Min", StringComparison.OrdinalIgnoreCase)).ToList();
            var maxRecords = frameForces.Where(f => f.StepType != null && f.StepType.Equals("Max", StringComparison.OrdinalIgnoreCase)).ToList();

            DetailedFrameForceData forceAtStart = null, forceAtEnd = null;

            if (minRecords.Any())
            {
                var sortedMin = minRecords.OrderBy(f => f.Station).ToList();
                forceAtStart = sortedMin.First();
                beam.Momen1 = GetMoment(forceAtStart);
                beam.Shear1 = GetShear(forceAtStart);
                beam.Station1 = forceAtStart.StationRatio;

                forceAtEnd = sortedMin.Last();
                beam.Momen3 = GetMoment(forceAtEnd);
                beam.Shear3 = GetShear(forceAtEnd);
                beam.Station3 = forceAtEnd.StationRatio;
            }

            if (maxRecords.Any() && forceAtStart != null && forceAtEnd != null)
            {
                double stationRange = forceAtEnd.Station - forceAtStart.Station;
                double lowerBound = forceAtStart.Station + (stationRange * 0.3);
                double upperBound = forceAtStart.Station + (stationRange * 0.7);
                double midStation = (forceAtStart.Station + forceAtEnd.Station) / 2.0;

                var middleRecords = maxRecords.Where(f => f.Station >= lowerBound && f.Station <= upperBound).ToList();
                var forceAtMiddle = middleRecords.Any()
                    ? middleRecords.OrderByDescending(f => Math.Abs(GetMoment(f))).FirstOrDefault()
                    : maxRecords.OrderBy(f => Math.Abs(f.Station - midStation)).FirstOrDefault();

                if (forceAtMiddle != null)
                {
                    beam.Momen2 = GetMoment(forceAtMiddle);
                    beam.Shear2 = GetShear(forceAtMiddle);
                    beam.Station2 = forceAtMiddle.StationRatio;
                }
            }
            else
            {
                beam.Momen2 = 0; beam.Shear2 = 0; beam.Station2 = 0.5;
            }
        }

        private void UpdateBeamForcesFromSimpleCase(BeamModel beam, List<DetailedFrameForceData> frameForces)
        {
            var sortedForces = frameForces.OrderBy(f => f.Station).ToList();

            var forceAtStart = sortedForces.First();
            beam.Momen1 = GetMoment(forceAtStart);
            beam.Shear1 = GetShear(forceAtStart);
            beam.Station1 = forceAtStart.StationRatio;

            var forceAtEnd = sortedForces.Last();
            beam.Momen3 = GetMoment(forceAtEnd);
            beam.Shear3 = GetShear(forceAtEnd);
            beam.Station3 = forceAtEnd.StationRatio;

            double midStation = (forceAtStart.Station + forceAtEnd.Station) / 2.0;
            double stationRange = forceAtEnd.Station - forceAtStart.Station;
            double lowerBound = forceAtStart.Station + (stationRange * 0.3);
            double upperBound = forceAtStart.Station + (stationRange * 0.7);

            var middleRecords = sortedForces.Where(f => f.Station >= lowerBound && f.Station <= upperBound).ToList();
            var forceAtMiddle = middleRecords.Any()
                ? middleRecords.OrderByDescending(f => Math.Abs(GetMoment(f))).FirstOrDefault()
                : sortedForces.OrderBy(f => Math.Abs(f.Station - midStation)).FirstOrDefault();

            if (forceAtMiddle != null)
            {
                beam.Momen2 = GetMoment(forceAtMiddle);
                beam.Shear2 = GetShear(forceAtMiddle);
                beam.Station2 = forceAtMiddle.StationRatio;
            }
        }

        private void ResetBeamForces(BeamModel beam)
        {
            beam.Momen1 = 0; beam.Shear1 = 0; beam.Station1 = 0.0;
            beam.Momen2 = 0; beam.Shear2 = 0; beam.Station2 = 0.5;
            beam.Momen3 = 0; beam.Shear3 = 0; beam.Station3 = 1.0;
        }

        private double GetMoment(DetailedFrameForceData f)
        {
            return Math.Abs(f.Moment3) >= Math.Abs(f.Moment2) ? f.Moment3 : f.Moment2;
        }

        private double GetShear(DetailedFrameForceData f)
        {
            return Math.Abs(f.Shear2) >= Math.Abs(f.Shear3) ? f.Shear2 : f.Shear3;
        }

        #endregion

        #region Rebar Calculation (giữ nguyên logic tính toán)

        public int CalculateRebarForAllBeams()
        {
            if (SelectedConcrete == null || SelectedRebarLongitudinal == null) return 0;

            int calculatedCount = 0;
            foreach (var beam in BeamList)
            {
                if (beam.Momen1 == 0 && beam.Momen2 == 0 && beam.Momen3 == 0) continue;
                try
                {
                    CalculateRebarForBeam(beam);
                    calculatedCount++;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Lỗi tính thép dầm {beam.Name}: {ex.Message}");
                }
            }

            UpdateAllStirrupCalculations();
            OnPropertyChanged(nameof(BeamList));
            return calculatedCount;
        }

        private void CalculateRebarForBeam(BeamModel beam)
        {
            double Rb = SelectedConcrete.Rb;
            double Rs = SelectedRebarLongitudinal.Rs;
            double a = ProtectiveLayer;
            double b = beam.B;
            double h = beam.H;
            double h0 = h - a - 10;

            beam.AsRequiredTrai = CalculateRebarAreaValue(beam.Momen1, b, h0, Rb, Rs);
            beam.AsRequiredDuoi = CalculateMidRebarAreaValue(beam, beam.Momen2, h0, Rb, Rs);
            beam.AsRequiredPhai = CalculateRebarAreaValue(beam.Momen3, b, h0, Rb, Rs);
        }

        private double CalculateRebarAreaValue(double M, double b, double h0, double Rb, double Rs)
        {
            M = Math.Abs(M);
            if (M <= 0 || b <= 0 || h0 <= 0) return 0;

            try
            {
                double M_Nmm = M * 1_000_000;
                double alpha_m = M_Nmm / (Rb * b * h0 * h0);
                if (alpha_m > AlphaR) return -1;

                double ksi = 0.5 * (1 + Math.Sqrt(1 - 2 * alpha_m));
                double As = M_Nmm / (Rs * ksi * h0);
                double As_min = 0.0005 * b * h0;
                return Math.Max(As, As_min);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi tính thép: {ex.Message}");
                return 0;
            }
        }

        private double CalculateMidRebarAreaValue(BeamModel beam, double M, double h0, double Rb, double Rs)
        {
            M = Math.Abs(M);
            if (M <= 0 || h0 <= 0) return 0;

            double S1 = Math.Abs(0.5 * (beam.SpanLeft - beam.B));
            double S2 = Math.Abs(0.5 * (beam.SpanRight - beam.B));
            double S3 = Math.Abs(beam.Ltt / 6);
            double Sc = Math.Min(S1, Math.Min(S2, S3));
            double b = beam.B + 2 * Sc;

            try
            {
                double M_Nmm = M * 1_000_000;
                double hf = beam.FloorThicknessLeft > 0 ? beam.FloorThicknessLeft : beam.FloorThicknessRight;
                if (hf <= 0) hf = _defaultFloorThickness;

                double Mf = Rb * b * hf * (h0 - 0.5 * hf);

                if (M_Nmm <= Mf)
                {
                    double alpha_m = M_Nmm / (Rb * b * h0 * h0);
                    if (alpha_m > AlphaR) return -1;
                    double ksi = 0.5 * (1 + Math.Sqrt(1 - 2 * alpha_m));
                    double As = M_Nmm / (Rs * ksi * h0);
                    double As_min = 0.0005 * b * h0;
                    return Math.Max(As, As_min);
                }
                else
                {
                    double alpha_m = M_Nmm / (Rb * b * hf * h0);
                    if (alpha_m > AlphaR) return -1;
                    double ksi = 0.5 * (1 + Math.Sqrt(1 - 2 * alpha_m));
                    double As = M_Nmm / (Rs * ksi * h0);
                    double As_min = 0.0005 * b * h0;
                    return Math.Max(As, As_min);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi tính thép: {ex.Message}");
                return 0;
            }
        }

        #endregion

        #region Stirrup Calculation (giữ nguyên logic)

        public static double CalculateShear(double Ltt, double B, double H, double Cover, double stirrupArea, double SpanLeft, double SpanRight, double WallHeight, double hf, double V, double Rb, double Rbt, double Rsw)
        {
            V = Math.Abs(V * 1000);
            double spacing = 0;
            var h0 = H - Cover - 10;
            var Gk2 = B * H / 1.1 * 25 / 1000000;
            var Gt2 = 5.8 * WallHeight / Ltt;
            var cq = Math.Min(2.4 * h0, 0.25 * Ltt);

            var hmax = Math.Max(SpanLeft, SpanRight);
            var S = ((Ltt + Ltt - hmax) * hmax) / 2;
            if (Ltt > 2 * hmax) S = Ltt * hmax / 2;
            if (Ltt < hmax) S = Ltt * hmax / 4;
            if (Ltt < hmax / 2) S = 0;

            var san = (hf * 25 * S) * 1.1 / Ltt;
            var gs = 2 * S * cq / (2 * hmax) / 1000000;
            var g = Gk2 + Gt2 + gs;
            var p = (2600 * S / 2) / Ltt * cq / (hmax * 2);
            if (Ltt < hmax / 2) p = 0;
            var q1 = g + 0.5 * p / 1000000;

            if (V < 0.3 * Rb * B * h0 && V < 2.5 * Rbt * B * h0)
            {
                var Mb = 1.5 * Rbt * B * h0 * h0;
                var Qbmin = 0.5 * Rbt * B * h0;

                if (V > 2 * Math.Sqrt(Mb * q1))
                {
                    var qrw1 = (V * V - 4 * Mb * q1) / (3 * Mb);
                    var qrw2 = (V - 2 * Math.Sqrt(Mb * q1)) / (1.5 * h0);
                    var qrw3 = (V - Qbmin - 3 * q1 * h0) / (1.5 * h0);
                    var qrw = Math.Max(qrw1, Math.Max(qrw2, qrw3));
                    var qswmin = 0.25 * Rbt * B;
                    var qsw = Math.Max(qrw, qswmin);
                    var stt = Rsw * stirrupArea * 2 / qsw;
                    var smax = Rbt * B * h0 * h0 / V;
                    var sct = Math.Min(0.5 * h0, 300);
                    spacing = Math.Min(stt, Math.Min(sct, smax));
                }
                else
                {
                    var smax = Rbt * B * h0 * h0 / V;
                    var sct = Math.Min(0.5 * h0, 300);
                    spacing = Math.Min(smax, Math.Min(sct, smax));
                }
            }
            return spacing;
        }

        private void UpdateAllStirrupCalculations()
        {
            foreach (var beam in BeamList)
            {
                if (beam.Shear1 != 0 || beam.Shear2 != 0 || beam.Shear3 != 0)
                {
                    var Rb = SelectedConcrete?.Rb ?? 17.0;
                    var Rbt = SelectedConcrete?.Rbt ?? 1.15;
                    var RsStirrup = SelectedStirrupMaterial?.Rsw ?? 170.0;
                    double hf = beam.FloorThicknessLeft > 0 ? beam.FloorThicknessLeft : (beam.FloorThicknessRight > 0 ? beam.FloorThicknessRight : _defaultFloorThickness);
                    double stirrupArea = GetStirrupAreaByDiameter(SelectedStirrupType);

                    beam.StirrupSpacingV1 = CalculateShear(beam.Ltt, beam.B, beam.H, ProtectiveLayer, stirrupArea, beam.SpanLeft, beam.SpanRight, beam.WallHeight, hf, beam.Shear1, Rb, Rbt, RsStirrup);
                    beam.StirrupSpacingV2 = CalculateShear(beam.Ltt, beam.B, beam.H, ProtectiveLayer, stirrupArea, beam.SpanLeft, beam.SpanRight, beam.WallHeight, hf, beam.Shear2, Rb, Rbt, RsStirrup);
                    beam.StirrupSpacingV3 = CalculateShear(beam.Ltt, beam.B, beam.H, ProtectiveLayer, stirrupArea, beam.SpanLeft, beam.SpanRight, beam.WallHeight, hf, beam.Shear3, Rb, Rbt, RsStirrup);
                }
            }
            OnPropertyChanged(nameof(BeamList));
        }

        private double GetStirrupAreaByDiameter(string diameter)
        {
            if (string.IsNullOrEmpty(diameter) || !diameter.StartsWith("Ø")) return 0;
            if (int.TryParse(diameter.Substring(1), out int dia))
            {
                switch (dia)
                {
                    case 6: return 28.3;
                    case 8: return 50.3;
                    case 10: return 78.5;
                    case 12: return 113.0;
                    default: return 0;
                }
            }
            return 0;
        }

        #endregion
    }
}
