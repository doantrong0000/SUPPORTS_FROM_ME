using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Calculator_Foundation_Etabs_API.Models
{
    public class FoundationModel : INotifyPropertyChanged
    {
        private string _name;
        private string _dimensions;
        private double _b;
        private double _h;
        private double _bw;
        private double _n;
        private double _m;
        private double _asReq;
        private string _selectedSteel;
        private string _status;
        private double _pmax;
        private double _pmin;
        private double _rtc;
        private double _length;
        private double _pPunchMax;
        private double _pCdt;
        private string _statusPunch;

        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
        public string Dimensions { get => _dimensions; set { _dimensions = value; OnPropertyChanged(); } }
        public double B { get => _b; set { _b = value; OnPropertyChanged(); } }
        public double H { get => _h; set { _h = value; OnPropertyChanged(); } }
        public double Bw { get => _bw; set { _bw = value; OnPropertyChanged(); } }
        public double N { get => _n; set { _n = value; OnPropertyChanged(); } }
        public double M { get => _m; set { _m = value; OnPropertyChanged(); } }
        public double AsReq { get => _asReq; set { _asReq = value; OnPropertyChanged(); } }
        public string SelectedSteel { get => _selectedSteel; set { _selectedSteel = value; OnPropertyChanged(); } }
        public string Status { get => _status; set { _status = value; OnPropertyChanged(); } }
        public double Pmax { get => _pmax; set { _pmax = value; OnPropertyChanged(); } }
        public double Pmin { get => _pmin; set { _pmin = value; OnPropertyChanged(); } }
        public double Rtc { get => _rtc; set { _rtc = value; OnPropertyChanged(); } }
        public double Length { get => _length; set { _length = value; OnPropertyChanged(); } }
        public double PPunchMax { get => _pPunchMax; set { _pPunchMax = value; OnPropertyChanged(); } }
        public double PCdt { get => _pCdt; set { _pCdt = value; OnPropertyChanged(); } }
        public string StatusPunch { get => _statusPunch; set { _statusPunch = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
