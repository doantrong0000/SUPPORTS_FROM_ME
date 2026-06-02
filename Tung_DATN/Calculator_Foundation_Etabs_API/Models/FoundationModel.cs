using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Calculator_Foundation_Etabs_API.Models
{
    public class FoundationModel : INotifyPropertyChanged
    {
        private string _name;
        private double _length;
        private double _b;
        private double _bd;
        private double _h;
        private double _hd;

        private double _mBot_Start;
        private double _mTop_Mid;
        private double _mBot_End;
        private double _q_Max;

        // Bổ sung biến Strip
        private double _mStrip_Start;
        private double _mStrip_Mid;
        private double _mStrip_End;

        private double _asBot_Start;
        private double _asTop_Mid;
        private double _asBot_End;
        private double _asw;

        private string _selectedSteel;
        private string _status;

        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
        public double Length { get => _length; set { _length = value; OnPropertyChanged(); } }
        public double B { get => _b; set { _b = value; OnPropertyChanged(); } }
        public double Bd { get => _bd; set { _bd = value; OnPropertyChanged(); } }
        public double H { get => _h; set { _h = value; OnPropertyChanged(); } }
        public double Hd { get => _hd; set { _hd = value; OnPropertyChanged(); } }

        // --- NỘI LỰC DẦM (FRAME) ---
        // Khi thay đổi Momen Dầm, gọi OnPropertyChanged cho Momen Tổng để UI tự update
        public double MBot_Start { get => _mBot_Start; set { _mBot_Start = value; OnPropertyChanged(); OnPropertyChanged(nameof(MTotal_Start)); } }
        public double MTop_Mid { get => _mTop_Mid; set { _mTop_Mid = value; OnPropertyChanged(); OnPropertyChanged(nameof(MTotal_Mid)); } }
        public double MBot_End { get => _mBot_End; set { _mBot_End = value; OnPropertyChanged(); OnPropertyChanged(nameof(MTotal_End)); } }
        public double Q_Max { get => _q_Max; set { _q_Max = value; OnPropertyChanged(); } }

        // --- NỘI LỰC DẢI TRUYỀN VÀO (STRIP) ---
        // Khi thay đổi Momen Strip, gọi OnPropertyChanged cho Momen Tổng để UI tự update
        public double MStrip_Start { get => _mStrip_Start; set { _mStrip_Start = value; OnPropertyChanged(); OnPropertyChanged(nameof(MTotal_Start)); } }
        public double MStrip_Mid { get => _mStrip_Mid; set { _mStrip_Mid = value; OnPropertyChanged(); OnPropertyChanged(nameof(MTotal_Mid)); } }
        public double MStrip_End { get => _mStrip_End; set { _mStrip_End = value; OnPropertyChanged(); OnPropertyChanged(nameof(MTotal_End)); } }

        // --- NỘI LỰC TỔNG CỘNG (DÙNG ĐỂ TÍNH THÉP) ---
        public double MTotal_Start => MBot_Start + MStrip_Start;
        public double MTotal_Mid => MTop_Mid + MStrip_Mid;
        public double MTotal_End => MBot_End + MStrip_End;

        // --- CỐT THÉP ---
        public double AsBot_Start { get => _asBot_Start; set { _asBot_Start = value; OnPropertyChanged(); } }
        public double AsTop_Mid { get => _asTop_Mid; set { _asTop_Mid = value; OnPropertyChanged(); } }
        public double AsBot_End { get => _asBot_End; set { _asBot_End = value; OnPropertyChanged(); } }
        public double Asw { get => _asw; set { _asw = value; OnPropertyChanged(); } }

        public string SelectedSteel { get => _selectedSteel; set { _selectedSteel = value; OnPropertyChanged(); } }
        public string Status { get => _status; set { _status = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}