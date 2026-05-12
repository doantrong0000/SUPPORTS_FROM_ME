using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Calculator_Foundation_Etabs_API.Models;

namespace Calculator_Foundation_Etabs_API.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        // Inputs
        private string _gamma1 = "18";
        private string _gamma = "18";
        private string _phi = "30";
        private string _c = "20";
        private string _df = "1.5";
        private string _fs = "2.5";
        private string _b = "1.2";
        private string _h = "0.5";
        private string _bw = "0.22";
        private string _selectedCombo;

        public string Gamma1 { get => _gamma1; set { _gamma1 = value; OnPropertyChanged(); CalculateAll(); } }
        public string Gamma { get => _gamma; set { _gamma = value; OnPropertyChanged(); CalculateAll(); } }
        public string Phi { get => _phi; set { _phi = value; OnPropertyChanged(); CalculateAll(); } }
        public string C { get => _c; set { _c = value; OnPropertyChanged(); CalculateAll(); } }
        public string Df { get => _df; set { _df = value; OnPropertyChanged(); CalculateAll(); } }
        public string Fs { get => _fs; set { _fs = value; OnPropertyChanged(); CalculateAll(); } }
        public string B { get => _b; set { _b = value; OnPropertyChanged(); CalculateAll(); } }
        public string H { get => _h; set { _h = value; OnPropertyChanged(); CalculateAll(); } }
        public string Bw { get => _bw; set { _bw = value; OnPropertyChanged(); CalculateAll(); } }

        private double ParseFraction(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return 0;
            input = input.Trim().Replace(",", ".");
            if (input.Contains("/"))
            {
                var parts = input.Split('/');
                if (parts.Length == 2 && double.TryParse(parts[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double n) &&
                    double.TryParse(parts[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double d) && d != 0)
                    return n / d;
            }
            double.TryParse(input, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double res);
            return res;
        }

        public string SelectedCombo 
        { 
            get => _selectedCombo; 
            set 
            { 
                if (SetProperty(ref _selectedCombo, value))
                {
                    SyncEtabs(null);
                }
            } 
        }

        // Collections
        public ObservableCollection<FoundationModel> Foundations { get; } = new ObservableCollection<FoundationModel>();
        public ObservableCollection<string> LoadCombos { get; } = new ObservableCollection<string>();

        public IEnumerable<ConcreteGrade> ConcreteGrades => Enum.GetValues(typeof(ConcreteGrade)).Cast<ConcreteGrade>();
        public IEnumerable<RebarGrade> RebarGrades => Enum.GetValues(typeof(RebarGrade)).Cast<RebarGrade>();

        private ConcreteGrade _selectedConcrete = ConcreteGrade.B20;
        public ConcreteGrade SelectedConcrete { get => _selectedConcrete; set { SetProperty(ref _selectedConcrete, value); CalculateAll(); } }

        private RebarGrade _selectedRebar = RebarGrade.CB400V;
        public RebarGrade SelectedRebar { get => _selectedRebar; set { SetProperty(ref _selectedRebar, value); CalculateAll(); } }

        // Commands
        public ICommand SyncEtabsCommand { get; }
        public ICommand RefreshCombosCommand { get; }

        public MainViewModel()
        {
            SyncEtabsCommand = new RelayCommand(SyncEtabs);
            RefreshCombosCommand = new RelayCommand(RefreshCombos);
        }

        private void RefreshCombos(object obj)
        {
            try {
                var combos = EtabsHelper.GetLoadCombinations();
                LoadCombos.Clear();
                foreach (var c in combos) LoadCombos.Add(c);
                if (LoadCombos.Any()) SelectedCombo = LoadCombos.First();
            } catch (Exception ex) {
                MessageBox.Show("Lỗi kết nối ETABS: " + ex.Message);
            }
        }

        private void SyncEtabs(object obj)
        {
            if (string.IsNullOrEmpty(SelectedCombo)) return;

            var rawDataList = EtabsHelper.GetBaseLevelFoundations(SelectedCombo);
            if (rawDataList != null && rawDataList.Any())
            {
                Foundations.Clear();
                foreach (var raw in rawDataList)
                {
                    var (n, m) = FoundationCalculator.MergeForces(raw);
                    var f = new FoundationModel {
                        Name = raw.Name,
                        B = raw.B,
                        H = raw.H,
                        Bw = raw.Bw,
                        Dimensions = $"{raw.B:0.##} x {raw.H:0.##} x {raw.Bw:0.##}",
                        N = n,
                        M = m,
                        Length = raw.MaxCoord - raw.MinCoord,
                        SelectedSteel = SelectedRebar.ToString()
                    };
                    Foundations.Add(f);
                }
                CalculateAll();
                
                if (obj != null)
                    MessageBox.Show($"Đã đồng bộ {rawDataList.Count} dải móng từ ETABS thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CalculateAll()
        {
            if (!Foundations.Any()) return;

            double phiVal = ParseFraction(Phi);
            double cVal = ParseFraction(C);
            double g1Val = ParseFraction(Gamma1);
            double gVal = ParseFraction(Gamma);
            double dfVal = ParseFraction(Df);
            double fsVal = ParseFraction(Fs);
            double bVal = ParseFraction(B);
            double hVal = ParseFraction(H);
            double bwVal = ParseFraction(Bw);

            double rs = SelectedRebar == RebarGrade.CB400V ? 350 : 260;

            foreach (var f in Foundations)
            {
                f.B = bVal; f.H = hVal; f.Bw = bwVal;
                f.Dimensions = $"{bVal:0.##} x {hVal:0.##} x {bwVal:0.##}";

                FoundationCalculator.RunCalculation(f, phiVal, cVal, g1Val, gVal, dfVal, fsVal, rs);
            }
        }
    }
}
