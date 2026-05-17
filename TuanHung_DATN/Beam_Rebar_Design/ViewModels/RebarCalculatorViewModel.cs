using Beam_Rebar_Design.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Beam_Rebar_Design.ViewModels
{
    public class RebarCalculatorViewModel : INotifyPropertyChanged
    {
        private EtabsConnector _etabs = new EtabsConnector();

        public ObservableCollection<BeamModel> BeamList { get; set; } = new ObservableCollection<BeamModel>();
        public ObservableCollection<string> OutputCases { get; set; } = new ObservableCollection<string>();

        private string _selectedOutputCase;
        public string SelectedOutputCase
        {
            get { return _selectedOutputCase; }
            set { _selectedOutputCase = value; OnPropertyChanged("SelectedOutputCase"); }
        }

        public List<ConcreteModel> ConcreteList { get; set; } = new List<ConcreteModel>();
        public List<RebarModel> RebarList { get; set; } = new List<RebarModel>();
        public List<string> StirrupDiameterList { get; set; } = new List<string> { "Ø6", "Ø8", "Ø10", "Ø12" };

        private string _selectedStirrupType = "Ø8";
        public string SelectedStirrupType
        {
            get { return _selectedStirrupType; }
            set { _selectedStirrupType = value; OnPropertyChanged("SelectedStirrupType"); UpdateAllStirrup(); }
        }

        private RebarModel _selectedStirrupMaterial;
        public RebarModel SelectedStirrupMaterial
        {
            get { return _selectedStirrupMaterial; }
            set { _selectedStirrupMaterial = value; OnPropertyChanged("SelectedStirrupMaterial"); UpdateAllStirrup(); }
        }

        private ConcreteModel _selectedConcrete;
        public ConcreteModel SelectedConcrete
        {
            get { return _selectedConcrete; }
            set { _selectedConcrete = value; OnPropertyChanged("SelectedConcrete"); }
        }

        private RebarModel _selectedRebarLong;
        public RebarModel SelectedRebarLongitudinal
        {
            get { return _selectedRebarLong; }
            set
            {
                _selectedRebarLong = value;
                OnPropertyChanged("SelectedRebarLongitudinal");
                OnPropertyChanged("KsiRDisplay");
                OnPropertyChanged("AlphaRDisplay");
            }
        }

        public double ProtectiveLayer { get; set; } = 25;
        public double DefaultFloorThickness { get; set; } = 120;
        public double DefaultWallHeight { get; set; } = 3000;

        public string KsiRDisplay { get { return SelectedRebarLongitudinal != null ? SelectedRebarLongitudinal.KsiR.ToString("F3") : "0.615"; } }
        public string AlphaRDisplay { get { return SelectedRebarLongitudinal != null ? SelectedRebarLongitudinal.AlphaR.ToString("F3") : "0.426"; } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        public RebarCalculatorViewModel()
        {
            // Bê tông
            ConcreteList.Add(new ConcreteModel { Name = "B20", Rb = 11.5, Rbt = 0.9 });
            ConcreteList.Add(new ConcreteModel { Name = "B25", Rb = 14.5, Rbt = 1.05 });
            ConcreteList.Add(new ConcreteModel { Name = "B30", Rb = 17, Rbt = 1.15 });

            // Thép
            RebarList.Add(new RebarModel { Name = "CB240-T", Rs = 210, Rsw = 170, KsiR = 0.615, AlphaR = 0.426 });
            RebarList.Add(new RebarModel { Name = "CB300-T", Rs = 260, Rsw = 210, KsiR = 0.583, AlphaR = 0.413 });
            RebarList.Add(new RebarModel { Name = "CB400-V", Rs = 350, Rsw = 280, KsiR = 0.533, AlphaR = 0.391 });

            SelectedConcrete = ConcreteList[1];
            SelectedRebarLongitudinal = RebarList[1];
            SelectedStirrupMaterial = RebarList[0];
        }

        public bool ConnectToEtabs()
        {
            return _etabs.Connect();
        }

        public bool LoadBeamsFromEtabs()
        {
            if (!_etabs.IsConnected) return false;
            BeamList.Clear();
            List<BeamModel> beams = _etabs.GetAllBeams();
            foreach (var b in beams)
            {
                b.FloorThicknessLeft = DefaultFloorThickness;
                b.FloorThicknessRight = DefaultFloorThickness;
                b.WallHeight = DefaultWallHeight;
                BeamList.Add(b);
            }
            return BeamList.Count > 0;
        }

        public bool LoadOutputCases()
        {
            if (!_etabs.IsConnected) return false;
            OutputCases.Clear();
            List<string> cases = _etabs.GetAllLoadCasesAndCombos();
            foreach (var c in cases) OutputCases.Add(c);
            return OutputCases.Count > 0;
        }

        public bool LoadForcesFromEtabs()
        {
            if (!_etabs.IsConnected || string.IsNullOrEmpty(SelectedOutputCase)) return false;
            
            List<string> frameNames = new List<string>();
            foreach (var b in BeamList) frameNames.Add(b.Name);

            List<DetailedFrameForceData> forces = _etabs.GetFrameForces(SelectedOutputCase, frameNames);
            if (forces.Count == 0) return false;

            foreach (var beam in BeamList)
            {
                List<DetailedFrameForceData> myForces = new List<DetailedFrameForceData>();
                foreach (var f in forces) if (f.FrameName == beam.Name) myForces.Add(f);

                if (myForces.Count > 0)
                {
                    // Sắp xếp đơn giản theo Station
                    myForces.Sort((x, y) => x.Station.CompareTo(y.Station));

                    var start = myForces[0];
                    var end = myForces[myForces.Count - 1];
                    var mid = myForces[myForces.Count / 2]; // Lấy tạm giữa

                    beam.Momen1 = Math.Abs(start.Moment3) > Math.Abs(start.Moment2) ? start.Moment3 : start.Moment2;
                    beam.Shear1 = Math.Abs(start.Shear2) > Math.Abs(start.Shear3) ? start.Shear2 : start.Shear3;

                    beam.Momen3 = Math.Abs(end.Moment3) > Math.Abs(end.Moment2) ? end.Moment3 : end.Moment2;
                    beam.Shear3 = Math.Abs(end.Shear2) > Math.Abs(end.Shear3) ? end.Shear2 : end.Shear3;

                    beam.Momen2 = Math.Abs(mid.Moment3) > Math.Abs(mid.Moment2) ? mid.Moment3 : mid.Moment2;
                    beam.Shear2 = Math.Abs(mid.Shear2) > Math.Abs(mid.Shear3) ? mid.Shear2 : mid.Shear3;
                }
            }
            return true;
        }

        public void CalculateRebarForAllBeams()
        {
            foreach (var beam in BeamList)
            {
                double Rb = SelectedConcrete.Rb;
                double Rs = SelectedRebarLongitudinal.Rs;
                double h0 = beam.H - ProtectiveLayer - 10;
                double b = beam.B;

                beam.AsRequiredTrai = CalcAs(beam.Momen1, b, h0, Rb, Rs);
                beam.AsRequiredDuoi = CalcAs(beam.Momen2, b, h0, Rb, Rs);
                beam.AsRequiredPhai = CalcAs(beam.Momen3, b, h0, Rb, Rs);
            }
            UpdateAllStirrup();
        }

        private double CalcAs(double M, double b, double h0, double Rb, double Rs)
        {
            M = Math.Abs(M);
            if (M == 0) return 0;
            double M_Nmm = M * 1000000;
            double alpha_m = M_Nmm / (Rb * b * h0 * h0);
            if (alpha_m > SelectedRebarLongitudinal.AlphaR) return -1;

            double ksi = 0.5 * (1 + Math.Sqrt(1 - 2 * alpha_m));
            double As = M_Nmm / (Rs * ksi * h0);
            double As_min = 0.0005 * b * h0;
            return Math.Max(As, As_min);
        }

        private void UpdateAllStirrup()
        {
            foreach (var beam in BeamList)
            {
                double stirrupArea = 0;
                if (SelectedStirrupType == "Ø6") stirrupArea = 28.3;
                else if (SelectedStirrupType == "Ø8") stirrupArea = 50.3;
                else if (SelectedStirrupType == "Ø10") stirrupArea = 78.5;
                else if (SelectedStirrupType == "Ø12") stirrupArea = 113.0;

                double Rsw = SelectedStirrupMaterial != null ? SelectedStirrupMaterial.Rsw : 170;
                double Rbt = SelectedConcrete.Rbt;
                double h0 = beam.H - ProtectiveLayer - 10;

                // Tính toán đai đơn giản nhất (Ví dụ dùng Rsw và stirrupArea)
                double V = Math.Abs(beam.Shear1 * 1000); // Đổi ra N
                if (V > 0)
                {
                    double q_sw = (Rsw * stirrupArea * 2) / 150; // Giả sử bước 150 để test
                    double s_max = (Rbt * beam.B * h0 * h0) / V;
                    double s_cau_tao = Math.Min(h0 / 2, 300);
                    beam.StirrupSpacingV1 = Math.Min(s_max, s_cau_tao);
                    beam.StirrupSpacingV2 = s_cau_tao;
                    beam.StirrupSpacingV3 = Math.Min(s_max, s_cau_tao);
                }
            }
        }
    }
}
